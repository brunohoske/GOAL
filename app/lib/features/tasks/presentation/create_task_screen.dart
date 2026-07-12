import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../../goals/data/goals_repository.dart';

/// Admin creates a catalog task. XP mode (Manual / Scalable) + documentation flags.
/// Text documentation is always required (locked on).
class CreateTaskScreen extends ConsumerStatefulWidget {
  const CreateTaskScreen({super.key, required this.goalId});
  final String goalId;
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

  Future<void> _submit() async {
    if (_title.text.trim().isEmpty) return;
    setState(() => _submitting = true);
    try {
      final items = <Map<String, dynamic>>[
        for (var i = 0; i < _checklist.length; i++)
          if (_checklist[i].controller.text.trim().isNotEmpty)
            {
              'label': _checklist[i].controller.text.trim(),
              'orderIndex': i,
              'isRequired': _checklist[i].isRequired,
            },
      ];
      final body = {
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
      await ref.read(goalsRepositoryProvider).createTask(widget.goalId, body);
      ref.invalidate(tasksProvider(widget.goalId)); // so the Tarefas tab shows it immediately
      if (mounted) context.pop();
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Nova tarefa')),
      body: ListView(
        padding: const EdgeInsets.all(AppSpacing.lg),
        children: [
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
          FilledButton(
            onPressed: _submitting ? null : _submit,
            child: _submitting
                ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                : const Text('Criar tarefa'),
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
