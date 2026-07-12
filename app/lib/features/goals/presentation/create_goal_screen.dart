import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../design_system/components/goal_wordmark.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../data/goals_repository.dart';

/// Create-goal wizard. This is where the goal's behaviour is configured (immutable after).
/// Kept simple: good defaults pre-filled, only a few clear choices to make.
class CreateGoalScreen extends ConsumerStatefulWidget {
  const CreateGoalScreen({super.key});
  @override
  ConsumerState<CreateGoalScreen> createState() => _CreateGoalScreenState();
}

/// A known social app the user can toggle for blocking.
class _AppOption {
  const _AppOption(this.pkg, this.name);
  final String pkg;
  final String name;
}

const _knownApps = [
  _AppOption('com.instagram.android', 'Instagram'),
  _AppOption('com.zhiliaoapp.musically', 'TikTok'),
  _AppOption('com.twitter.android', 'X'),
  _AppOption('com.facebook.katana', 'Facebook'),
  _AppOption('com.google.android.youtube', 'YouTube'),
];

class _CreateGoalScreenState extends ConsumerState<CreateGoalScreen> {
  final _title = TextEditingController();
  final _description = TextEditingController();

  int _sprintDays = 14;
  int _xpTarget = 100;
  int _tasksPerSprint = 5;
  double _unblockPct = 0.70;
  int _finalTriggerDays = 1;

  /// XP table derived from "target / expected tasks": a MEDIUM task is worth the
  /// average; EASY is half; HARD is double. Snapshotted into GoalSettings on create.
  int get _xpMedium => (_xpTarget / _tasksPerSprint).round().clamp(1, 100000);
  int get _xpEasy => (_xpMedium / 2).round().clamp(1, 100000);
  int get _xpHard => _xpMedium * 2;
  final Set<String> _blockedPkgs = {'com.instagram.android', 'com.zhiliaoapp.musically', 'com.twitter.android'};
  bool _submitting = false;

  @override
  void dispose() {
    _title.dispose();
    _description.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_title.text.trim().isEmpty) return;
    setState(() => _submitting = true);
    try {
      final body = {
        'title': _title.text.trim(),
        'description': _description.text.trim().isEmpty ? null : _description.text.trim(),
        'timeZone': 'America/Sao_Paulo',
        'startAt': null,
        'settings': {
          'sprintDurationDays': _sprintDays,
          'baseXpTargetPerSprint': _xpTarget,
          'unblockThresholdPct': _unblockPct,
          'finalTriggerDaysBefore': _finalTriggerDays,
          'finalTriggerTargetPct': 1.00,
          'voteApprovalThreshold': 0.60,
          'debtCarryEnabled': true,
          'xpScalableEasy': _xpEasy,
          'xpScalableMedium': _xpMedium,
          'xpScalableHard': _xpHard,
          'blockedApps': _knownApps
              .where((a) => _blockedPkgs.contains(a.pkg))
              .map((a) => {'packageName': a.pkg, 'displayName': a.name})
              .toList(),
        },
      };
      final goalId = await ref.read(goalsRepositoryProvider).createGoal(body);
      ref.invalidate(goalsListProvider);
      if (mounted) context.go('/goals/$goalId');
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const GoalBrandedLabel(prefix: 'Novo', fontSize: 20)),
      body: ListView(
        padding: const EdgeInsets.all(AppSpacing.lg),
        children: [
          _Section(
            title: 'O GOAL',
            child: Column(
              children: [
                TextField(controller: _title, decoration: const InputDecoration(labelText: 'Título (ex: Aprender a fazer um jogo)')),
                const SizedBox(height: AppSpacing.md),
                TextField(controller: _description, decoration: const InputDecoration(labelText: 'Descrição (opcional)'), maxLines: 2),
              ],
            ),
          ),
          _Section(
            title: 'Ritmo',
            subtitle: 'Definido agora e fixo durante todo o GOAL.',
            child: Column(
              children: [
                _StepperRow(
                  label: 'Duração da sprint',
                  value: '$_sprintDays dias',
                  onMinus: () => setState(() => _sprintDays = (_sprintDays - 1).clamp(1, 90)),
                  onPlus: () => setState(() => _sprintDays = (_sprintDays + 1).clamp(1, 90)),
                ),
                const Divider(height: AppSpacing.xl),
                _StepperRow(
                  label: 'Meta de XP por sprint',
                  value: '$_xpTarget XP',
                  onMinus: () => setState(() => _xpTarget = (_xpTarget - 10).clamp(10, 1000)),
                  onPlus: () => setState(() => _xpTarget = (_xpTarget + 10).clamp(10, 1000)),
                ),
                const Divider(height: AppSpacing.xl),
                _StepperRow(
                  label: 'Tarefas por sprint (média por pessoa)',
                  value: '$_tasksPerSprint',
                  onMinus: () => setState(() => _tasksPerSprint = (_tasksPerSprint - 1).clamp(1, 50)),
                  onPlus: () => setState(() => _tasksPerSprint = (_tasksPerSprint + 1).clamp(1, 50)),
                ),
                const SizedBox(height: AppSpacing.lg),

                // Live preview of the derived XP table, so the rule is transparent.
                Container(
                  width: double.infinity,
                  padding: const EdgeInsets.all(AppSpacing.md),
                  decoration: BoxDecoration(
                    color: AppColors.primaryContainer.withOpacity(0.45),
                    borderRadius: BorderRadius.circular(AppSpacing.radiusSm),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Valor calculado das tarefas',
                          style: Theme.of(context)
                              .textTheme
                              .bodySmall
                              ?.copyWith(fontWeight: FontWeight.w700, color: AppColors.primary)),
                      const SizedBox(height: AppSpacing.xs),
                      Text(
                        'Fácil $_xpEasy XP  ·  Médio $_xpMedium XP  ·  Difícil $_xpHard XP',
                        style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
                      ),
                      const SizedBox(height: AppSpacing.xs),
                      Text(
                        '$_xpTarget XP ÷ $_tasksPerSprint tarefas = $_xpMedium XP por tarefa média. '
                        'Fácil vale metade, difícil vale o dobro.',
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          _Section(
            title: 'Bloqueio',
            subtitle: 'Apps ficam bloqueados até você atingir a meta da sprint.',
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Liberar ao atingir ${(_unblockPct * 100).round()}% da meta',
                    style: Theme.of(context).textTheme.bodyMedium),
                Slider(
                  value: _unblockPct,
                  min: 0.30,
                  max: 1.0,
                  divisions: 14,
                  activeColor: AppColors.primary,
                  label: '${(_unblockPct * 100).round()}%',
                  onChanged: (v) => setState(() => _unblockPct = v),
                ),
                const SizedBox(height: AppSpacing.sm),
                Text('No último dia, sobe para 100% para liberar.',
                    style: Theme.of(context).textTheme.bodySmall),
                const SizedBox(height: AppSpacing.lg),
                Text('Apps bloqueados', style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: AppSpacing.sm),
                Wrap(
                  spacing: AppSpacing.sm,
                  runSpacing: AppSpacing.sm,
                  children: _knownApps.map((a) {
                    final on = _blockedPkgs.contains(a.pkg);
                    return FilterChip(
                      label: Text(a.name),
                      selected: on,
                      onSelected: (s) => setState(() => s ? _blockedPkgs.add(a.pkg) : _blockedPkgs.remove(a.pkg)),
                      selectedColor: AppColors.primaryContainer,
                      checkmarkColor: AppColors.primary,
                    );
                  }).toList(),
                ),
              ],
            ),
          ),
          const SizedBox(height: AppSpacing.lg),
          FilledButton(
            onPressed: _submitting ? null : _submit,
            child: _submitting
                ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                : const GoalBrandedLabel(prefix: 'Criar', fontSize: 15, color: Colors.white, accent: false),
          ),
          const SizedBox(height: AppSpacing.xl),
        ],
      ),
    );
  }
}

class _Section extends StatelessWidget {
  const _Section({required this.title, this.subtitle, required this.child});
  final String title;
  final String? subtitle;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.xl),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title, style: Theme.of(context).textTheme.titleMedium),
          if (subtitle != null) ...[
            const SizedBox(height: 2),
            Text(subtitle!, style: Theme.of(context).textTheme.bodySmall),
          ],
          const SizedBox(height: AppSpacing.md),
          child,
        ],
      ),
    );
  }
}

class _StepperRow extends StatelessWidget {
  const _StepperRow({required this.label, required this.value, required this.onMinus, required this.onPlus});
  final String label;
  final String value;
  final VoidCallback onMinus;
  final VoidCallback onPlus;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(child: Text(label, style: Theme.of(context).textTheme.bodyLarge)),
        IconButton.outlined(onPressed: onMinus, icon: const Icon(Icons.remove)),
        SizedBox(width: 80, child: Center(child: Text(value, style: const TextStyle(fontWeight: FontWeight.w600)))),
        IconButton.outlined(onPressed: onPlus, icon: const Icon(Icons.add)),
      ],
    );
  }
}
