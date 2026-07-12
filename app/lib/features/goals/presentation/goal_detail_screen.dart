import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../design_system/components/empty_state.dart';
import '../../../design_system/components/goal_progress_ring.dart';
import '../../../design_system/components/status_chip.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../../blocking/presentation/blocking_sync.dart';
import '../../tasks/domain/task_models.dart';
import '../data/goals_repository.dart';

/// Goal hub with tabs: Sprint · Tarefas · Ranking · Membros · Revisão.
class GoalDetailScreen extends ConsumerWidget {
  const GoalDetailScreen({super.key, required this.goalId});
  final String goalId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final detailAsync = ref.watch(goalDetailProvider(goalId));

    // Keep the native block policy in sync while this goal is on screen.
    final title = detailAsync.maybeWhen(data: (g) => g.title, orElse: () => '');
    ref.watch(blockingSyncProvider((goalId: goalId, goalTitle: title)));

    return detailAsync.when(
      loading: () => const Scaffold(body: Center(child: CircularProgressIndicator())),
      error: (e, _) => Scaffold(
        appBar: AppBar(
          leading: IconButton(
            icon: const Icon(Icons.arrow_back),
            onPressed: () => context.canPop() ? context.pop() : context.go('/home'),
          ),
        ),
        body: EmptyState(
          icon: Icons.cloud_off,
          title: 'Não foi possível carregar',
          action: FilledButton(
            onPressed: () => ref.invalidate(goalDetailProvider(goalId)),
            child: const Text('Tentar de novo'),
          ),
        ),
      ),
      data: (goal) {
        final sprintId = goal.currentSprintId;
        final members = ref.watch(membersProvider(goalId)).valueOrNull ?? const <Member>[];
        final isAdmin = members.any((m) => m.isMe && m.isAdmin);

        return DefaultTabController(
          length: 5,
          child: Scaffold(
            appBar: AppBar(
              // Always show a way home: after create/join the stack may have nothing to pop.
              leading: IconButton(
                icon: const Icon(Icons.arrow_back),
                onPressed: () => context.canPop() ? context.pop() : context.go('/home'),
              ),
              title: Text(goal.title, overflow: TextOverflow.ellipsis),
              actions: [
                IconButton(
                  icon: const Icon(Icons.person_add_outlined),
                  tooltip: 'Convidar',
                  onPressed: () => _showInvite(context, goal.joinCode),
                ),
                if (isAdmin)
                  PopupMenuButton<String>(
                    onSelected: (v) {
                      if (v == 'edit') _showEditGoal(context, ref, goal);
                      if (v == 'delete') _confirmDelete(context, ref, goal.title);
                    },
                    itemBuilder: (_) => const [
                      PopupMenuItem(value: 'edit', child: Text('Editar GOAL')),
                      PopupMenuItem(
                        value: 'delete',
                        child: Text('Excluir GOAL', style: TextStyle(color: AppColors.danger)),
                      ),
                    ],
                  ),
              ],
              bottom: const TabBar(
                isScrollable: true,
                tabAlignment: TabAlignment.start,
                labelColor: AppColors.primary,
                indicatorColor: AppColors.primary,
                tabs: [
                  Tab(text: 'Sprint'),
                  Tab(text: 'Tarefas'),
                  Tab(text: 'Equipe'),
                  Tab(text: 'Ranking'),
                  Tab(text: 'Revisão'),
                ],
              ),
            ),
            body: TabBarView(
              children: [
                _SprintTab(goalId: goalId),
                _TasksTab(goalId: goalId, sprintId: sprintId, isAdmin: isAdmin, members: members),
                _TeamTab(goalId: goalId, sprintId: sprintId),
                _RankingTab(goalId: goalId),
                _ReviewTab(goalId: goalId),
              ],
            ),
            floatingActionButton: _Fab(goalId: goalId, sprintId: sprintId),
          ),
        );
      },
    );
  }

  /// Admin edit sheet — only the mutable fields, each labelled with WHEN it applies.
  void _showEditGoal(BuildContext context, WidgetRef ref, dynamic goal) {
    final titleCtrl = TextEditingController(text: goal.title as String);
    int sprintDays = goal.settings.sprintDurationDays as int;
    int xpTarget = goal.settings.baseXpTargetPerSprint as int;
    int tasksPerSprint = (xpTarget / (goal.settings.xpScalableMedium as int)).round().clamp(1, 50);
    bool saving = false;

    int xpMedium() => (xpTarget / tasksPerSprint).round().clamp(1, 100000);
    int xpEasy() => (xpMedium() / 2).round().clamp(1, 100000);
    int xpHard() => xpMedium() * 2;

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(AppSpacing.radiusLg)),
      ),
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setSheet) => Padding(
          padding: EdgeInsets.fromLTRB(AppSpacing.xl, AppSpacing.xl, AppSpacing.xl,
              MediaQuery.of(ctx).viewInsets.bottom + AppSpacing.xl),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text('Editar GOAL', style: Theme.of(ctx).textTheme.headlineSmall),
              const SizedBox(height: AppSpacing.lg),
              TextField(controller: titleCtrl, decoration: const InputDecoration(labelText: 'Nome')),
              const SizedBox(height: AppSpacing.lg),
              _EditStepper(
                label: 'Duração da sprint',
                hint: 'vale a partir da PRÓXIMA sprint',
                value: '$sprintDays dias',
                onMinus: () => setSheet(() => sprintDays = (sprintDays - 1).clamp(1, 90)),
                onPlus: () => setSheet(() => sprintDays = (sprintDays + 1).clamp(1, 90)),
              ),
              _EditStepper(
                label: 'Meta de XP por sprint',
                hint: 'aplica AGORA — XP ganho e dívidas são mantidos',
                value: '$xpTarget XP',
                onMinus: () => setSheet(() => xpTarget = (xpTarget - 10).clamp(10, 1000)),
                onPlus: () => setSheet(() => xpTarget = (xpTarget + 10).clamp(10, 1000)),
              ),
              _EditStepper(
                label: 'Tarefas por sprint (média)',
                hint: 'recalcula: Fácil ${xpEasy()} · Médio ${xpMedium()} · Difícil ${xpHard()} XP',
                value: '$tasksPerSprint',
                onMinus: () => setSheet(() => tasksPerSprint = (tasksPerSprint - 1).clamp(1, 50)),
                onPlus: () => setSheet(() => tasksPerSprint = (tasksPerSprint + 1).clamp(1, 50)),
              ),
              const SizedBox(height: AppSpacing.lg),
              FilledButton(
                onPressed: saving
                    ? null
                    : () async {
                        setSheet(() => saving = true);
                        try {
                          await ref.read(goalsRepositoryProvider).updateGoal(goalId, {
                            'title': titleCtrl.text.trim(),
                            'sprintDurationDays': sprintDays,
                            'baseXpTargetPerSprint': xpTarget,
                            'xpScalableEasy': xpEasy(),
                            'xpScalableMedium': xpMedium(),
                            'xpScalableHard': xpHard(),
                          });
                          ref.invalidate(goalDetailProvider(goalId));
                          ref.invalidate(blockingStateProvider(goalId));
                          ref.invalidate(membersProvider(goalId));
                          ref.invalidate(tasksProvider(goalId));
                          ref.invalidate(goalsListProvider);
                          if (ctx.mounted) Navigator.pop(ctx);
                        } catch (_) {
                          setSheet(() => saving = false);
                          if (ctx.mounted) {
                            ScaffoldMessenger.of(ctx).showSnackBar(
                              const SnackBar(content: Text('Não foi possível salvar.')),
                            );
                          }
                        }
                      },
                child: saving
                    ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                    : const Text('Salvar'),
              ),
            ],
          ),
        ),
      ),
    );
  }

  /// Deleting is the only way out — make sure the admin understands the blast radius.
  Future<void> _confirmDelete(BuildContext context, WidgetRef ref, String title) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('Excluir "$title"?'),
        content: const Text(
            'O GOAL some para TODOS os membros, o código deixa de funcionar e os bloqueios são liberados. '
            'Isso não pode ser desfeito.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancelar')),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: AppColors.danger),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Excluir'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    await ref.read(goalsRepositoryProvider).deleteGoal(goalId);
    ref.invalidate(goalsListProvider);
    if (context.mounted) {
      context.go('/home');
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('GOAL "$title" excluído.')),
      );
    }
  }

  /// Invite = share the goal's join code. Friends type it in "Entrar com código" on Home.
  void _showInvite(BuildContext context, String joinCode) {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(AppSpacing.radiusLg)),
      ),
      builder: (ctx) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.xl),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text('Convidar amigos', style: Theme.of(ctx).textTheme.headlineSmall),
              const SizedBox(height: AppSpacing.sm),
              Text('Compartilhe este código. No app, o amigo toca em "Entrar com código" na tela inicial.',
                  style: Theme.of(ctx).textTheme.bodySmall),
              const SizedBox(height: AppSpacing.xl),
              Container(
                padding: const EdgeInsets.symmetric(vertical: AppSpacing.lg),
                decoration: BoxDecoration(
                  color: AppColors.primaryContainer,
                  borderRadius: BorderRadius.circular(AppSpacing.radiusSm),
                ),
                child: Text(
                  joinCode,
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    fontSize: 32,
                    fontWeight: FontWeight.w800,
                    letterSpacing: 8,
                    color: AppColors.primary,
                  ),
                ),
              ),
              const SizedBox(height: AppSpacing.lg),
              FilledButton.icon(
                onPressed: () async {
                  await Clipboard.setData(ClipboardData(text: joinCode));
                  if (ctx.mounted) {
                    Navigator.pop(ctx);
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Código copiado!')),
                    );
                  }
                },
                icon: const Icon(Icons.copy, size: 18),
                label: const Text('Copiar código'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Stepper row used by the admin edit sheet, with a hint explaining WHEN the change applies.
class _EditStepper extends StatelessWidget {
  const _EditStepper({
    required this.label,
    required this.hint,
    required this.value,
    required this.onMinus,
    required this.onPlus,
  });

  final String label;
  final String hint;
  final String value;
  final VoidCallback onMinus;
  final VoidCallback onPlus;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.md),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label, style: const TextStyle(fontWeight: FontWeight.w600)),
                Text(hint, style: Theme.of(context).textTheme.bodySmall),
              ],
            ),
          ),
          IconButton(onPressed: onMinus, icon: const Icon(Icons.remove_circle_outline)),
          SizedBox(
            width: 72,
            child: Text(value, textAlign: TextAlign.center,
                style: const TextStyle(fontWeight: FontWeight.w700)),
          ),
          IconButton(onPressed: onPlus, icon: const Icon(Icons.add_circle_outline)),
        ],
      ),
    );
  }
}

/// Context-aware FAB: only on the Tarefas tab (admin creates tasks).
class _Fab extends StatelessWidget {
  const _Fab({required this.goalId, required this.sprintId});
  final String goalId;
  final String? sprintId;

  @override
  Widget build(BuildContext context) {
    final tab = DefaultTabController.of(context);
    return AnimatedBuilder(
      animation: tab,
      builder: (context, _) {
        if (tab.index != 1) return const SizedBox.shrink();
        return FloatingActionButton.extended(
          onPressed: () => context.push('/goals/$goalId/tasks/create'),
          icon: const Icon(Icons.add_task),
          label: const Text('Nova tarefa'),
        );
      },
    );
  }
}

// ---------------- Sprint tab ----------------
class _SprintTab extends ConsumerWidget {
  const _SprintTab({required this.goalId});
  final String goalId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final blockingAsync = ref.watch(blockingStateProvider(goalId));
    final detail = ref.watch(goalDetailProvider(goalId)).valueOrNull;
    final joinCode = detail?.joinCode ?? '';
    return RefreshIndicator(
      onRefresh: () async => ref.invalidate(blockingStateProvider(goalId)),
      child: ListView(
        padding: const EdgeInsets.all(AppSpacing.lg),
        children: [
          blockingAsync.when(
            loading: () => const _CardSkeleton(),
            error: (_, __) => const SizedBox.shrink(),
            data: (bs) => _BlockingCard(state: bs, sprintEndsAt: detail?.currentSprintEndsAt),
          ),
          if (joinCode.isNotEmpty) ...[
            const SizedBox(height: AppSpacing.md),
            Card(
              child: ListTile(
                contentPadding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
                leading: const Icon(Icons.vpn_key_outlined, color: AppColors.primary),
                title: const Text('Código do GOAL', style: TextStyle(fontSize: 13)),
                subtitle: Text(joinCode,
                    style: const TextStyle(
                        fontSize: 20, fontWeight: FontWeight.w800, letterSpacing: 4, color: AppColors.onSurface)),
                trailing: IconButton(
                  icon: const Icon(Icons.copy, size: 20),
                  tooltip: 'Copiar',
                  onPressed: () async {
                    await Clipboard.setData(ClipboardData(text: joinCode));
                    if (context.mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(content: Text('Código copiado! Compartilhe com seus amigos.')),
                      );
                    }
                  },
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _BlockingCard extends StatelessWidget {
  const _BlockingCard({required this.state, this.sprintEndsAt});
  final dynamic state;
  final DateTime? sprintEndsAt;

  @override
  Widget build(BuildContext context) {
    final target = state.effectiveTargetXp as int;
    final progress = target <= 0 ? 0.0 : state.earnedXp / target;
    final threshold = target <= 0 ? null : (state.targetXp / target).clamp(0.0, 1.0).toDouble();
    final reached = !(state.isBlocked as bool);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Text('Seu progresso', style: Theme.of(context).textTheme.titleMedium),
                const Spacer(),
                state.isBlocked ? StatusChip.blocked() : StatusChip.approved('Livre'),
              ],
            ),
            const SizedBox(height: AppSpacing.lg),

            // The brand "O" closing with the sprint: arc = XP earned, tick = unblock point.
            Row(
              children: [
                GoalProgressRing(
                  progress: progress,
                  threshold: threshold,
                  size: 108,
                  sublabel: 'da meta',
                ),
                const SizedBox(width: AppSpacing.xl),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('${state.earnedXp} / ${state.effectiveTargetXp} XP',
                          style: const TextStyle(fontSize: 17, fontWeight: FontWeight.w700)),
                      const SizedBox(height: AppSpacing.xs),
                      Text('${state.daysRemaining} dias restantes',
                          style: Theme.of(context).textTheme.bodySmall),
                      if (sprintEndsAt != null) ...[
                        const SizedBox(height: 2),
                        Text(
                          'Nova sprint: ${DateFormat("dd/MM 'às' HH:mm").format(sprintEndsAt!)}',
                          style: Theme.of(context).textTheme.bodySmall,
                        ),
                      ],
                      const SizedBox(height: AppSpacing.md),
                      Row(
                        children: [
                          Container(
                            width: 10,
                            height: 10,
                            decoration: BoxDecoration(
                              color: reached ? AppColors.success : AppColors.warning,
                              shape: BoxShape.circle,
                            ),
                          ),
                          const SizedBox(width: AppSpacing.sm),
                          Expanded(
                            child: Text(
                              reached
                                  ? 'Apps liberados!'
                                  : 'A marca no anel é o ponto de desbloqueio (${state.targetXp} XP).',
                              style: TextStyle(
                                fontSize: 12,
                                color: reached ? AppColors.success : AppColors.onSurfaceMuted,
                                fontWeight: reached ? FontWeight.w600 : FontWeight.w400,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ],
            ),

            if (state.isBlocked) ...[
              const SizedBox(height: AppSpacing.md),
              Text(
                state.requiresFullCompletion
                    ? 'Último dia! Conclua 100% (${state.xpRemainingToUnblock} XP) para liberar.'
                    : 'Faltam ${state.xpRemainingToUnblock} XP para liberar seus apps.',
                style: const TextStyle(color: AppColors.danger, fontSize: 13, fontWeight: FontWeight.w500),
              ),
            ],
            if (state.debtXp > 0) ...[
              const SizedBox(height: AppSpacing.md),
              StatusChip.debt('Dívida acumulada: ${state.debtXp} XP'),
            ],
          ],
        ),
      ),
    );
  }
}

// ---------------- Tasks tab ----------------
class _TasksTab extends ConsumerWidget {
  const _TasksTab({required this.goalId, required this.sprintId, required this.isAdmin, required this.members});
  final String goalId;
  final String? sprintId;
  final bool isAdmin;
  final List<Member> members;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (sprintId == null) {
      return const EmptyState(icon: Icons.hourglass_empty, title: 'Nenhuma sprint ativa');
    }
    final tasksAsync = ref.watch(tasksProvider(goalId));
    final assignmentsAsync = ref.watch(assignmentsProvider(sprintId!));

    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(tasksProvider(goalId));
        ref.invalidate(assignmentsProvider(sprintId!));
      },
      child: tasksAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Text('Erro: $e')),
        data: (tasks) {
          final assignments = assignmentsAsync.valueOrNull ?? const [];
          final taskById = {for (final t in tasks) t.id: t};

          if (tasks.isEmpty) {
            return const EmptyState(
              icon: Icons.checklist,
              title: 'Nenhuma tarefa no catálogo',
              message: 'O admin cria as tarefas. Toque em "Nova tarefa".',
            );
          }

          return ListView(
            padding: const EdgeInsets.all(AppSpacing.lg),
            children: [
              if (assignments.isNotEmpty) ...[
                Text('Em andamento nesta sprint', style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: AppSpacing.sm),
                ...assignments.map((a) => _AssignmentTile(
                      goalId: goalId,
                      sprintId: sprintId!,
                      assignment: a,
                      task: taskById[a.taskDefinitionId],
                    )),
                const SizedBox(height: AppSpacing.xl),
              ],
              Text('Catálogo de tarefas', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: AppSpacing.sm),
              ...tasks.map((t) => _CatalogTaskTile(
                    goalId: goalId,
                    sprintId: sprintId!,
                    task: t,
                    isAdmin: isAdmin,
                    members: members,
                  )),
            ],
          );
        },
      ),
    );
  }
}

/// A catalog task with a "Pegar" (self-assign) button; admins can also assign to a member.
class _CatalogTaskTile extends ConsumerWidget {
  const _CatalogTaskTile({
    required this.goalId,
    required this.sprintId,
    required this.task,
    required this.isAdmin,
    required this.members,
  });
  final String goalId;
  final String sprintId;
  final TaskDef task;
  final bool isAdmin;
  final List<Member> members;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Card(
      margin: const EdgeInsets.only(bottom: AppSpacing.md),
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(task.title, style: Theme.of(context).textTheme.titleMedium),
                  const SizedBox(height: 4),
                  Row(
                    children: [
                      Text('${task.estimatedXp} XP', style: const TextStyle(color: AppColors.secondary, fontWeight: FontWeight.w600, fontSize: 13)),
                      if (task.requiresImage) const _ReqDot(label: 'imagem'),
                      if (task.requiresAttachment) const _ReqDot(label: 'anexo'),
                      if (task.hasChecklist) const _ReqDot(label: 'checklist'),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(width: AppSpacing.md),
            if (isAdmin)
              IconButton(
                icon: const Icon(Icons.assignment_ind_outlined, color: AppColors.primary),
                tooltip: 'Atribuir a um membro',
                onPressed: () => _pickMember(context, ref),
              ),
            OutlinedButton(
              onPressed: () => _assign(context, ref, null),
              child: const Text('Pegar'),
            ),
          ],
        ),
      ),
    );
  }

  /// Admin: bottom sheet with the member list; picking one assigns the task to them.
  void _pickMember(BuildContext context, WidgetRef ref) {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(AppSpacing.radiusLg)),
      ),
      builder: (ctx) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Padding(
              padding: const EdgeInsets.all(AppSpacing.xl),
              child: Text('Atribuir "${task.title}" para:',
                  style: Theme.of(ctx).textTheme.titleMedium),
            ),
            ...members.map((m) => ListTile(
                  leading: CircleAvatar(
                    backgroundColor: AppColors.primaryContainer,
                    child: Text(m.displayName.isNotEmpty ? m.displayName[0].toUpperCase() : '?',
                        style: const TextStyle(color: AppColors.primary, fontWeight: FontWeight.w700)),
                  ),
                  title: Text('${m.displayName}${m.isMe ? " (você)" : ""}'),
                  onTap: () {
                    Navigator.pop(ctx);
                    _assign(context, ref, m);
                  },
                )),
            const SizedBox(height: AppSpacing.md),
          ],
        ),
      ),
    );
  }

  Future<void> _assign(BuildContext context, WidgetRef ref, Member? target) async {
    await ref
        .read(goalsRepositoryProvider)
        .assignTask(sprintId, task.id, targetMemberId: target?.memberId);
    ref.invalidate(assignmentsProvider(sprintId));
    if (context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(target == null || target.isMe
            ? 'Tarefa "${task.title}" atribuída a você.'
            : 'Tarefa "${task.title}" atribuída a ${target.displayName}.')),
      );
    }
  }
}

class _ReqDot extends StatelessWidget {
  const _ReqDot({required this.label});
  final String label;
  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(left: AppSpacing.sm),
        child: Text('· $label', style: Theme.of(context).textTheme.bodySmall),
      );
}

/// An in-progress assignment tile with a "Concluir" action when it's mine.
class _AssignmentTile extends ConsumerWidget {
  const _AssignmentTile({required this.goalId, required this.sprintId, required this.assignment, this.task});
  final String goalId;
  final String sprintId;
  final Assignment assignment;
  final TaskDef? task;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final a = assignment;
    return Card(
      margin: const EdgeInsets.only(bottom: AppSpacing.md),
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(a.taskTitle, style: Theme.of(context).textTheme.titleMedium),
                  const SizedBox(height: 4),
                  Text(
                    a.assignedToMe ? 'Você · ${a.estimatedXp} XP' : '${a.assignedToName ?? "—"} · ${a.estimatedXp} XP',
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                ],
              ),
            ),
            const SizedBox(width: AppSpacing.md),
            _trailing(context),
          ],
        ),
      ),
    );
  }

  Widget _trailing(BuildContext context) {
    switch (assignment.status) {
      case AssignmentStatus.approved:
        return StatusChip.approved();
      case AssignmentStatus.pendingReview:
        return StatusChip.pending();
      case AssignmentStatus.rejected:
        return assignment.assignedToMe
            ? FilledButton(onPressed: () => _complete(context), child: const Text('Refazer'))
            : StatusChip.rejected();
      default:
        return assignment.assignedToMe
            ? FilledButton(onPressed: () => _complete(context), child: const Text('Concluir'))
            : StatusChip.neutral('Em andamento');
    }
  }

  void _complete(BuildContext context) {
    context.push('/goals/$goalId/complete/${assignment.id}', extra: {
      'taskTitle': assignment.taskTitle,
      'requiresImage': assignment.requiresImage,
      'requiresAttachment': assignment.requiresAttachment,
      'hasChecklist': assignment.hasChecklist,
      'checklistItems': (task?.checklistItems ?? const []).map((c) => c.toJson()).toList(),
      'estimatedXp': assignment.estimatedXp,
      'sprintId': sprintId,
    });
  }
}

// ---------------- Ranking tab ----------------
class _RankingTab extends ConsumerWidget {
  const _RankingTab({required this.goalId});
  final String goalId;

  static const _medals = ['🥇', '🥈', '🥉'];

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final membersAsync = ref.watch(membersProvider(goalId));
    return RefreshIndicator(
      onRefresh: () async => ref.invalidate(membersProvider(goalId)),
      child: membersAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Text('Erro: $e')),
        data: (members) {
          final ranked = [...members]..sort((a, b) => b.earnedXp.compareTo(a.earnedXp));
          return ListView.separated(
            padding: const EdgeInsets.all(AppSpacing.lg),
            itemCount: ranked.length,
            separatorBuilder: (_, __) => const SizedBox(height: AppSpacing.sm),
            itemBuilder: (_, i) {
              final m = ranked[i];
              final pct = m.effectiveTargetXp <= 0
                  ? 0.0
                  : (m.earnedXp / m.effectiveTargetXp).clamp(0.0, 1.0);
              return Card(
                child: Padding(
                  padding: const EdgeInsets.all(AppSpacing.lg),
                  child: Row(
                    children: [
                      SizedBox(
                        width: 36,
                        child: Text(
                          i < _medals.length ? _medals[i] : '${i + 1}º',
                          style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
                          textAlign: TextAlign.center,
                        ),
                      ),
                      const SizedBox(width: AppSpacing.md),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text('${m.displayName}${m.isMe ? " (você)" : ""}',
                                style: const TextStyle(fontWeight: FontWeight.w600)),
                            const SizedBox(height: AppSpacing.sm),
                            ClipRRect(
                              borderRadius: BorderRadius.circular(4),
                              child: LinearProgressIndicator(
                                value: pct,
                                minHeight: 6,
                                backgroundColor: AppColors.surfaceAlt,
                                valueColor: const AlwaysStoppedAnimation(AppColors.primary),
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(width: AppSpacing.md),
                      Text('${m.earnedXp} XP',
                          style: const TextStyle(color: AppColors.secondary, fontWeight: FontWeight.w700)),
                    ],
                  ),
                ),
              );
            },
          );
        },
      ),
    );
  }
}

// ---------------- Team tab (sprint board per member) ----------------
class _TeamTab extends ConsumerWidget {
  const _TeamTab({required this.goalId, required this.sprintId});
  final String goalId;
  final String? sprintId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (sprintId == null) {
      return const EmptyState(icon: Icons.hourglass_empty, title: 'Nenhuma sprint ativa');
    }
    final membersAsync = ref.watch(membersProvider(goalId));
    final assignments = ref.watch(assignmentsProvider(sprintId!)).valueOrNull ?? const <Assignment>[];

    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(membersProvider(goalId));
        ref.invalidate(assignmentsProvider(sprintId!));
      },
      child: membersAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Text('Erro: $e')),
        data: (members) {
          final unassigned = assignments.where((a) => a.assignedToMemberId == null).toList();
          return ListView(
            padding: const EdgeInsets.all(AppSpacing.lg),
            children: [
              ...members.map((m) => _MemberBoardCard(
                    goalId: goalId,
                    member: m,
                    assignments: assignments.where((a) => a.assignedToMemberId == m.memberId).toList(),
                  )),
              if (unassigned.isNotEmpty) ...[
                const SizedBox(height: AppSpacing.sm),
                Text('Sem responsável', style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: AppSpacing.sm),
                Card(
                  child: Column(
                    children: unassigned
                        .map((a) => _BoardTaskTile(goalId: goalId, assignment: a))
                        .toList(),
                  ),
                ),
              ],
            ],
          );
        },
      ),
    );
  }
}

/// One member's sprint: header (avatar, name, XP) + their tasks with tappable status.
class _MemberBoardCard extends StatelessWidget {
  const _MemberBoardCard({required this.goalId, required this.member, required this.assignments});
  final String goalId;
  final Member member;
  final List<Assignment> assignments;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: AppSpacing.md),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
        child: Column(
          children: [
            ListTile(
              contentPadding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
              leading: CircleAvatar(
                backgroundColor: AppColors.primaryContainer,
                child: Text(member.displayName.isNotEmpty ? member.displayName[0].toUpperCase() : '?',
                    style: const TextStyle(color: AppColors.primary, fontWeight: FontWeight.w700)),
              ),
              title: Text('${member.displayName}${member.isMe ? " (você)" : ""}',
                  style: const TextStyle(fontWeight: FontWeight.w600)),
              subtitle: Text('${member.earnedXp} / ${member.effectiveTargetXp} XP'),
              trailing: member.isAdmin ? StatusChip.neutral('Admin') : null,
            ),
            if (assignments.isEmpty)
              Padding(
                padding: const EdgeInsets.fromLTRB(AppSpacing.lg, 0, AppSpacing.lg, AppSpacing.sm),
                child: Align(
                  alignment: Alignment.centerLeft,
                  child: Text('Nenhuma tarefa nesta sprint',
                      style: Theme.of(context).textTheme.bodySmall),
                ),
              )
            else
              ...assignments.map((a) => _BoardTaskTile(goalId: goalId, assignment: a)),
          ],
        ),
      ),
    );
  }
}

/// A task row in the team board. Tapping opens the status sheet (votes, my vote, go review).
class _BoardTaskTile extends ConsumerWidget {
  const _BoardTaskTile({required this.goalId, required this.assignment});
  final String goalId;
  final Assignment assignment;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final a = assignment;
    return ListTile(
      dense: true,
      contentPadding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
      leading: Icon(_icon(a.status), size: 20, color: _iconColor(a.status)),
      title: Text(a.taskTitle, maxLines: 1, overflow: TextOverflow.ellipsis),
      subtitle: a.status == AssignmentStatus.pendingReview
          ? Text('${a.approvals}/${a.approvalsNeeded} aprovações'
              '${a.myVote == null ? "" : a.myVote == 0 ? " · você aprovou" : " · você reprovou"}')
          : null,
      trailing: _chip(a),
      onTap: () => _showStatusSheet(context),
    );
  }

  static IconData _icon(AssignmentStatus s) => switch (s) {
        AssignmentStatus.approved => Icons.check_circle,
        AssignmentStatus.pendingReview => Icons.how_to_vote_outlined,
        AssignmentStatus.rejected => Icons.cancel_outlined,
        _ => Icons.radio_button_unchecked,
      };

  static Color _iconColor(AssignmentStatus s) => switch (s) {
        AssignmentStatus.approved => AppColors.success,
        AssignmentStatus.pendingReview => AppColors.warning,
        AssignmentStatus.rejected => AppColors.danger,
        _ => AppColors.onSurfaceMuted,
      };

  Widget _chip(Assignment a) => switch (a.status) {
        AssignmentStatus.approved => StatusChip.approved('+${a.awardedXp ?? a.estimatedXp} XP'),
        AssignmentStatus.pendingReview => StatusChip.pending('Revisão'),
        AssignmentStatus.rejected => StatusChip.rejected(),
        _ => StatusChip.neutral('${a.estimatedXp} XP'),
      };

  void _showStatusSheet(BuildContext tabContext) {
    final a = assignment;
    // Can I still vote on this? (pending, someone else's work, no vote cast yet)
    final canVote = a.status == AssignmentStatus.pendingReview && !a.assignedToMe && a.myVote == null;

    showModalBottomSheet(
      context: tabContext,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(AppSpacing.radiusLg)),
      ),
      builder: (ctx) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.xl),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  Expanded(child: Text(a.taskTitle, style: Theme.of(ctx).textTheme.headlineSmall)),
                  _chip(a),
                ],
              ),
              const SizedBox(height: AppSpacing.xs),
              Text(a.assignedToName == null
                  ? 'Sem responsável'
                  : 'Responsável: ${a.assignedToName}${a.assignedToMe ? " (você)" : ""}'),
              const SizedBox(height: AppSpacing.lg),

              if (a.completionId != null) ...[
                _SheetLine(
                  icon: Icons.how_to_vote_outlined,
                  text: '${a.approvals} de ${a.approvalsNeeded} aprovações necessárias'
                      '${a.rejections > 0 ? " · ${a.rejections} reprovação(ões)" : ""}',
                ),
                _SheetLine(
                  icon: a.myVote == null
                      ? Icons.hourglass_empty
                      : a.myVote == 0
                          ? Icons.thumb_up_outlined
                          : Icons.thumb_down_outlined,
                  text: a.assignedToMe
                      ? 'Você é o autor — não vota nesta.'
                      : a.myVote == null
                          ? 'Você ainda não votou.'
                          : a.myVote == 0
                              ? 'Você aprovou esta entrega.'
                              : 'Você reprovou esta entrega.',
                  color: a.myVote == null ? null : (a.myVote == 0 ? AppColors.success : AppColors.danger),
                ),
                if (a.status == AssignmentStatus.approved && a.awardedXp != null)
                  _SheetLine(icon: Icons.bolt, text: 'XP creditado: ${a.awardedXp}', color: AppColors.secondary),
              ] else
                const _SheetLine(icon: Icons.pending_outlined, text: 'Nada foi enviado para revisão ainda.'),

              if (canVote) ...[
                const SizedBox(height: AppSpacing.lg),
                FilledButton.icon(
                  onPressed: () {
                    Navigator.pop(ctx);
                    // Jump to the Revisão tab where the documentation + vote buttons live.
                    DefaultTabController.of(tabContext).animateTo(4);
                  },
                  icon: const Icon(Icons.how_to_vote, size: 18),
                  label: const Text('Ir para a revisão e votar'),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _SheetLine extends StatelessWidget {
  const _SheetLine({required this.icon, required this.text, this.color});
  final IconData icon;
  final String text;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: Row(
        children: [
          Icon(icon, size: 18, color: color ?? AppColors.onSurfaceMuted),
          const SizedBox(width: AppSpacing.sm),
          Expanded(child: Text(text, style: TextStyle(fontSize: 14, color: color ?? AppColors.onSurface))),
        ],
      ),
    );
  }
}

// ---------------- Review tab ----------------
class _ReviewTab extends ConsumerWidget {
  const _ReviewTab({required this.goalId});
  final String goalId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final queueAsync = ref.watch(reviewQueueProvider(goalId));
    return RefreshIndicator(
      onRefresh: () async => ref.invalidate(reviewQueueProvider(goalId)),
      child: queueAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Text('Erro: $e')),
        data: (items) {
          if (items.isEmpty) {
            return const EmptyState(
              icon: Icons.how_to_vote_outlined,
              title: 'Nada para revisar',
              message: 'Quando seus amigos concluírem tarefas, elas aparecem aqui para você aprovar.',
            );
          }
          return ListView.separated(
            padding: const EdgeInsets.all(AppSpacing.lg),
            itemCount: items.length,
            separatorBuilder: (_, __) => const SizedBox(height: AppSpacing.md),
            itemBuilder: (_, i) => _ReviewCard(goalId: goalId, item: items[i]),
          );
        },
      ),
    );
  }
}

class _ReviewCard extends ConsumerWidget {
  const _ReviewCard({required this.goalId, required this.item});
  final String goalId;
  final ReviewItem item;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(child: Text(item.taskTitle, style: Theme.of(context).textTheme.titleMedium)),
                StatusChip.pending('${item.approvals}/${item.approvalsNeeded}'),
              ],
            ),
            const SizedBox(height: 4),
            Text('por ${item.authorName}', style: Theme.of(context).textTheme.bodySmall),
            const SizedBox(height: AppSpacing.md),
            Text(item.textContent, style: Theme.of(context).textTheme.bodyMedium),
            const SizedBox(height: AppSpacing.lg),
            Row(
              children: [
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: () => _vote(context, ref, 1),
                    icon: const Icon(Icons.close, size: 18),
                    label: const Text('Reprovar'),
                    style: OutlinedButton.styleFrom(foregroundColor: AppColors.danger),
                  ),
                ),
                const SizedBox(width: AppSpacing.md),
                Expanded(
                  child: FilledButton.icon(
                    onPressed: () => _vote(context, ref, 0),
                    icon: const Icon(Icons.check, size: 18),
                    label: const Text('Aprovar'),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _vote(BuildContext context, WidgetRef ref, int decision) async {
    final result = await ref.read(goalsRepositoryProvider).vote(item.completionId, decision);
    ref.invalidate(reviewQueueProvider(goalId));
    ref.invalidate(blockingStateProvider(goalId));
    ref.invalidate(assignmentsProvider); // refresh the team board (votes/status)
    ref.invalidate(membersProvider(goalId)); // XP may have been credited
    if (context.mounted) {
      final outcome = result['outcome'] as String?;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(outcome == 'Approved'
            ? 'Aprovado! XP creditado ao autor.'
            : 'Voto registrado.')),
      );
    }
  }
}

class _CardSkeleton extends StatelessWidget {
  const _CardSkeleton();
  @override
  Widget build(BuildContext context) => const Card(
        child: SizedBox(height: 160, child: Center(child: CircularProgressIndicator())),
      );
}
