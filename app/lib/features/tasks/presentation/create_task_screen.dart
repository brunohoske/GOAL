import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../design_system/components/error_state.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../../goals/data/goals_repository.dart';
import '../domain/task_models.dart';

/// Creates (or reviews) a catalog task. Any member can create: admin tasks go live
/// immediately, member tasks become a proposal the admin approves — optionally after
/// adjusting any field ([review] mode). Text documentation is always required.
class CreateTaskScreen extends ConsumerStatefulWidget {
  const CreateTaskScreen({
    super.key,
    required this.goalId,
    this.isAdmin = true,
    this.review,
  });
  final String goalId;
  final bool isAdmin;

  /// When set, the screen reviews this pending proposal instead of creating.
  final TaskDef? review;
  @override
  ConsumerState<CreateTaskScreen> createState() => _CreateTaskScreenState();
}

enum _XpMode { manual, scalable }

class _ChecklistDraft {
  _ChecklistDraft() : controller = TextEditingController();
  final TextEditingController controller;
  bool isRequired = true;
}

class _CreateTaskScreenState extends ConsumerState<CreateTaskScreen> {
  final _title = TextEditingController();
  final _description = TextEditingController();
  final _manualXp = TextEditingController(text: '20');

  _XpMode _mode = _XpMode.scalable;
  int _difficulty = 1; // 0 easy, 1 medium, 2 hard
  bool _requiresImage = false;
  bool _requiresAttachment = false;
  bool _hasChecklist = false;
  final List<_ChecklistDraft> _checklist = [];
  bool _submitting = false;

  bool get _reviewing => widget.review != null;

  @override
  void initState() {
    super.initState();
    // Review mode: prefill everything from the proposal so the admin can adjust freely.
    final t = widget.review;
    if (t != null) {
      _title.text = t.title;
      _description.text = t.description ?? '';
      _mode = t.xpMode == 0 ? _XpMode.manual : _XpMode.scalable;
      _difficulty = t.difficulty ?? 1;
      if (t.manualXp != null) _manualXp.text = '${t.manualXp}';
      _requiresImage = t.requiresImage;
      _requiresAttachment = t.requiresAttachment;
      _hasChecklist = t.hasChecklist && t.checklistItems.isNotEmpty;
      for (final item in t.checklistItems) {
        final draft = _ChecklistDraft()..isRequired = item.isRequired;
        draft.controller.text = item.label;
        _checklist.add(draft);
      }
    }
  }

  @override
  void dispose() {
    _title.dispose();
    _description.dispose();
    _manualXp.dispose();
    for (final c in _checklist) {
      c.controller.dispose();
    }
    super.dispose();
  }

  Map<String, dynamic> _buildBody() {
    final items = <Map<String, dynamic>>[
      for (var i = 0; i < _checklist.length; i++)
        if (_checklist[i].controller.text.trim().isNotEmpty)
          {
            'label': _checklist[i].controller.text.trim(),
            'orderIndex': i,
            'isRequired': _checklist[i].isRequired,
          },
    ];
    return {
      'title': _title.text.trim(),
      'description': _description.text.trim().isEmpty ? null : _description.text.trim(),
      'xpMode': _mode == _XpMode.manual ? 0 : 1,
      'manualXp': _mode == _XpMode.manual ? int.tryParse(_manualXp.text) ?? 10 : null,
      'difficulty': _mode == _XpMode.scalable ? _difficulty : null,
      'onTimeBonusXp': _mode == _XpMode.scalable ? 5 : null,
      'streakBonusXp': null,
      'requiresImage': _requiresImage,
      'requiresAttachment': _requiresAttachment,
      'hasChecklist': _hasChecklist && items.isNotEmpty,
      'checklistItems': items,
    };
  }

  Future<void> _submit() async {
    if (_title.text.trim().isEmpty) return;
    setState(() => _submitting = true);
    try {
      final repo = ref.read(goalsRepositoryProvider);
      if (_reviewing) {
        // Approve with whatever adjustments the admin made on screen.
        await repo.reviewTask(widget.goalId, widget.review!.id, {..._buildBody(), 'approve': true});
      } else {
        await repo.createTask(widget.goalId, _buildBody());
      }
      ref.invalidate(tasksProvider(widget.goalId)); // so the Tarefas tab shows it immediately
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
            content: Text(_reviewing
                ? 'Tarefa aprovada e publicada no catálogo.'
                : widget.isAdmin
                    ? 'Tarefa criada.'
                    : 'Tarefa enviada para aprovação do admin.')));
        context.pop();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Não deu para salvar: ${friendlyErrorMessage(e)}')),
        );
      }
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  Future<void> _reject() async {
    setState(() => _submitting = true);
    try {
      await ref.read(goalsRepositoryProvider).reviewTask(
          widget.goalId, widget.review!.id, {'approve': false});
      ref.invalidate(tasksProvider(widget.goalId));
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Tarefa recusada.')));
        context.pop();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Não deu para recusar: ${friendlyErrorMessage(e)}')),
        );
      }
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(_reviewing ? 'Revisar tarefa' : 'Nova tarefa')),
      // SafeArea: explicit list padding disables automatic system-inset handling,
      // so without this the last fields hide behind the Android nav bar.
      body: SafeArea(
        top: false,
        child: ListView(
        padding: const EdgeInsets.all(AppSpacing.lg),
        children: [
          if (_reviewing)
            _InfoBanner(
              icon: Icons.rate_review_outlined,
              text: 'Sugerida por ${widget.review!.proposedByName ?? "um membro"}. '
                  'Ajuste o que quiser e aprove — ou recuse.',
            )
          else if (!widget.isAdmin)
            const _InfoBanner(
              icon: Icons.hourglass_top,
              text: 'Sua tarefa será enviada para o admin aprovar. '
                  'Ela entra no catálogo quando ele aceitar.',
            ),
          if (_reviewing || !widget.isAdmin) const SizedBox(height: AppSpacing.lg),
          TextField(controller: _title, decoration: const InputDecoration(labelText: 'Título')),
          const SizedBox(height: AppSpacing.md),
          TextField(controller: _description, decoration: const InputDecoration(labelText: 'Descrição (opcional)'), maxLines: 2),
          const SizedBox(height: AppSpacing.xl),

          Text('Pontuação (XP)', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: AppSpacing.sm),
          SegmentedButton<_XpMode>(
            segments: const [
              ButtonSegment(value: _XpMode.scalable, label: Text('Por nível')),
              ButtonSegment(value: _XpMode.manual, label: Text('Manual')),
            ],
            selected: {_mode},
            onSelectionChanged: (s) => setState(() => _mode = s.first),
          ),
          const SizedBox(height: AppSpacing.md),
          if (_mode == _XpMode.scalable)
            SegmentedButton<int>(
              segments: const [
                ButtonSegment(value: 0, label: Text('Fácil')),
                ButtonSegment(value: 1, label: Text('Médio')),
                ButtonSegment(value: 2, label: Text('Difícil')),
              ],
              selected: {_difficulty},
              onSelectionChanged: (s) => setState(() => _difficulty = s.first),
            )
          else
            TextField(
              controller: _manualXp,
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(labelText: 'XP da tarefa'),
            ),

          const SizedBox(height: AppSpacing.xl),
          Text('Para concluir, exigir', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: AppSpacing.sm),
          _LockedSwitch(label: 'Texto (descrever o que foi feito)'),
          SwitchListTile(
            value: _requiresImage,
            onChanged: (v) => setState(() => _requiresImage = v),
            title: const Text('Imagem / print de evidência'),
            contentPadding: EdgeInsets.zero,
            activeColor: AppColors.primary,
          ),
          SwitchListTile(
            value: _requiresAttachment,
            onChanged: (v) => setState(() => _requiresAttachment = v),
            title: const Text('Anexo ou link'),
            contentPadding: EdgeInsets.zero,
            activeColor: AppColors.primary,
          ),
          SwitchListTile(
            value: _hasChecklist,
            onChanged: (v) => setState(() {
              _hasChecklist = v;
              if (v && _checklist.isEmpty) _checklist.add(_ChecklistDraft());
            }),
            title: const Text('Checklist de subtarefas'),
            contentPadding: EdgeInsets.zero,
            activeColor: AppColors.primary,
          ),

          if (_hasChecklist) ...[
            const SizedBox(height: AppSpacing.sm),
            ...List.generate(_checklist.length, (i) {
              final item = _checklist[i];
              return Padding(
                padding: const EdgeInsets.only(bottom: AppSpacing.sm),
                child: Row(
                  children: [
                    Expanded(
                      child: TextField(
                        controller: item.controller,
                        decoration: InputDecoration(labelText: 'Item ${i + 1}'),
                      ),
                    ),
                    const SizedBox(width: AppSpacing.sm),
                    Tooltip(
                      message: item.isRequired ? 'Obrigatório' : 'Opcional',
                      child: IconButton(
                        icon: Icon(
                          item.isRequired ? Icons.star : Icons.star_border,
                          color: item.isRequired ? AppColors.warning : AppColors.onSurfaceMuted,
                        ),
                        onPressed: () => setState(() => item.isRequired = !item.isRequired),
                      ),
                    ),
                    IconButton(
                      icon: const Icon(Icons.close, color: AppColors.onSurfaceMuted),
                      onPressed: () => setState(() {
                        item.controller.dispose();
                        _checklist.removeAt(i);
                      }),
                    ),
                  ],
                ),
              );
            }),
            TextButton.icon(
              onPressed: () => setState(() => _checklist.add(_ChecklistDraft())),
              icon: const Icon(Icons.add),
              label: const Text('Adicionar item'),
            ),
          ],

          const SizedBox(height: AppSpacing.xl),
          if (_reviewing) ...[
            FilledButton(
              onPressed: _submitting ? null : _submit,
              child: _submitting
                  ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                  : const Text('Aprovar tarefa'),
            ),
            const SizedBox(height: AppSpacing.sm),
            OutlinedButton(
              onPressed: _submitting ? null : _reject,
              style: OutlinedButton.styleFrom(foregroundColor: AppColors.danger),
              child: const Text('Recusar'),
            ),
          ] else
            FilledButton(
              onPressed: _submitting ? null : _submit,
              child: _submitting
                  ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                  : Text(widget.isAdmin ? 'Criar tarefa' : 'Solicitar tarefa'),
            ),
        ],
        ),
      ),
    );
  }
}

/// Soft callout used for the approval-flow notices.
class _InfoBanner extends StatelessWidget {
  const _InfoBanner({required this.icon, required this.text});
  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(AppSpacing.md),
      decoration: BoxDecoration(
        color: AppColors.primaryContainer,
        borderRadius: BorderRadius.circular(AppSpacing.radiusMd),
      ),
      child: Row(
        children: [
          Icon(icon, color: AppColors.primary, size: 20),
          const SizedBox(width: AppSpacing.md),
          Expanded(
            child: Text(text, style: const TextStyle(fontSize: 13, color: AppColors.primary)),
          ),
        ],
      ),
    );
  }
}

/// The text requirement is always on — shown as a disabled switch for clarity.
class _LockedSwitch extends StatelessWidget {
  const _LockedSwitch({required this.label});
  final String label;
  @override
  Widget build(BuildContext context) {
    return SwitchListTile(
      value: true,
      onChanged: null,
      title: Text(label),
      subtitle: const Text('Sempre obrigatório'),
      contentPadding: EdgeInsets.zero,
      activeColor: AppColors.primary,
    );
  }
}
