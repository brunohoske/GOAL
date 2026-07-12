import 'dart:io';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';

import '../../../core/network/dio_client.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../../goals/data/goals_repository.dart';
import '../../tasks/domain/task_models.dart';

/// Dynamic completion form: text is always required; image/attachment fields appear per flags.
/// On submit it uploads any image, posts the completion, and returns.
class CompleteTaskScreen extends ConsumerStatefulWidget {
  const CompleteTaskScreen({
    super.key,
    required this.goalId,
    required this.assignmentId,
    required this.taskTitle,
    required this.requiresImage,
    required this.requiresAttachment,
    this.hasChecklist = false,
    this.checklistItems = const [],
    required this.estimatedXp,
    required this.sprintId,
  });

  final String goalId;
  final String assignmentId;
  final String taskTitle;
  final bool requiresImage;
  final bool requiresAttachment;
  final bool hasChecklist;
  final List<ChecklistItem> checklistItems;
  final int estimatedXp;
  final String sprintId;

  @override
  ConsumerState<CompleteTaskScreen> createState() => _State();
}

class _State extends ConsumerState<CompleteTaskScreen> {
  final _text = TextEditingController();
  final _link = TextEditingController();
  XFile? _image;
  bool _submitting = false;
  String? _error;
  late final Map<String, bool> _checked = {
    for (final c in widget.checklistItems) c.id: false,
  };

  @override
  void dispose() {
    _text.dispose();
    _link.dispose();
    super.dispose();
  }

  Future<void> _pickImage() async {
    final picked = await ImagePicker().pickImage(source: ImageSource.gallery, imageQuality: 80);
    if (picked != null) setState(() => _image = picked);
  }

  Future<void> _submit() async {
    setState(() => _error = null);
    if (_text.text.trim().isEmpty) {
      setState(() => _error = 'Descreva o que foi feito.');
      return;
    }
    if (widget.requiresImage && _image == null) {
      setState(() => _error = 'Esta tarefa exige uma imagem.');
      return;
    }
    if (widget.requiresAttachment && _link.text.trim().isEmpty) {
      setState(() => _error = 'Esta tarefa exige um link/anexo.');
      return;
    }
    final missingRequired = widget.checklistItems
        .where((c) => c.isRequired && !(_checked[c.id] ?? false))
        .length;
    if (widget.hasChecklist && missingRequired > 0) {
      setState(() => _error = 'Complete os itens obrigatórios do checklist ($missingRequired faltando).');
      return;
    }

    setState(() => _submitting = true);
    try {
      final repo = ref.read(goalsRepositoryProvider);
      final attachments = <Map<String, dynamic>>[];

      if (_image != null) {
        final url = await _uploadImage(_image!);
        attachments.add({'type': 0, 'url': url, 'fileName': _image!.name, 'contentType': 'image/jpeg', 'sizeBytes': null});
      }
      if (_link.text.trim().isNotEmpty) {
        attachments.add({'type': 2, 'url': _link.text.trim(), 'fileName': null, 'contentType': null, 'sizeBytes': null});
      }

      final checklist = widget.checklistItems
          .map((c) => {'checklistItemTemplateId': c.id, 'isChecked': _checked[c.id] ?? false})
          .toList();

      await repo.submitCompletion(widget.assignmentId, _text.text.trim(),
          attachments: attachments, checklist: checklist);
      ref.invalidate(assignmentsProvider(widget.sprintId));
      if (mounted) {
        context.pop();
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Enviado! Aguardando aprovação dos membros.')),
        );
      }
    } catch (e) {
      setState(() => _error = 'Falha ao enviar. Tente novamente.');
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  /// Uploads the image to the backend upload endpoint, returns the stored URL.
  Future<String> _uploadImage(XFile file) async {
    final dio = ref.read(dioProvider);
    final form = FormData.fromMap({
      'file': await MultipartFile.fromFile(file.path, filename: file.name),
    });
    final r = await dio.post('/uploads', data: form);
    return (r.data as Map<String, dynamic>)['url'] as String;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Concluir tarefa')),
      body: ListView(
        padding: const EdgeInsets.all(AppSpacing.lg),
        children: [
          Text(widget.taskTitle, style: Theme.of(context).textTheme.headlineSmall),
          const SizedBox(height: 4),
          Text('Vale ${widget.estimatedXp} XP ao ser aprovada',
              style: const TextStyle(color: AppColors.secondary, fontWeight: FontWeight.w600)),
          const SizedBox(height: AppSpacing.xl),

          Text('O que você fez?', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: AppSpacing.sm),
          TextField(
            controller: _text,
            maxLines: 5,
            decoration: const InputDecoration(hintText: 'Descreva o que foi feito para os outros aprovarem...'),
          ),

          if (widget.requiresImage) ...[
            const SizedBox(height: AppSpacing.xl),
            Text('Imagem de evidência', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: AppSpacing.sm),
            _ImagePickerBox(image: _image, onPick: _pickImage),
          ],

          if (widget.requiresAttachment) ...[
            const SizedBox(height: AppSpacing.xl),
            Text('Link / anexo', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: AppSpacing.sm),
            TextField(controller: _link, decoration: const InputDecoration(hintText: 'https://...')),
          ],

          if (widget.hasChecklist && widget.checklistItems.isNotEmpty) ...[
            const SizedBox(height: AppSpacing.xl),
            Text('Checklist', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: AppSpacing.sm),
            Card(
              margin: EdgeInsets.zero,
              child: Column(
                children: widget.checklistItems
                    .map((c) => CheckboxListTile(
                          value: _checked[c.id] ?? false,
                          onChanged: (v) => setState(() => _checked[c.id] = v ?? false),
                          title: Text(c.label),
                          subtitle: c.isRequired ? null : const Text('opcional', style: TextStyle(fontSize: 12)),
                          controlAffinity: ListTileControlAffinity.leading,
                          dense: true,
                        ))
                    .toList(),
              ),
            ),
          ],

          if (_error != null) ...[
            const SizedBox(height: AppSpacing.md),
            Text(_error!, style: const TextStyle(color: AppColors.danger, fontSize: 13)),
          ],

          const SizedBox(height: AppSpacing.xl),
          FilledButton(
            onPressed: _submitting ? null : _submit,
            child: _submitting
                ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                : const Text('Enviar para aprovação'),
          ),
        ],
      ),
    );
  }
}

class _ImagePickerBox extends StatelessWidget {
  const _ImagePickerBox({required this.image, required this.onPick});
  final XFile? image;
  final VoidCallback onPick;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onPick,
      borderRadius: BorderRadius.circular(AppSpacing.radiusSm),
      child: Container(
        height: 160,
        decoration: BoxDecoration(
          color: AppColors.surfaceAlt,
          borderRadius: BorderRadius.circular(AppSpacing.radiusSm),
          border: Border.all(color: AppColors.outline),
        ),
        clipBehavior: Clip.antiAlias,
        child: image == null
            ? const Center(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(Icons.add_a_photo_outlined, color: AppColors.onSurfaceMuted),
                    SizedBox(height: AppSpacing.sm),
                    Text('Toque para adicionar', style: TextStyle(color: AppColors.onSurfaceMuted)),
                  ],
                ),
              )
            : Image.file(File(image!.path), fit: BoxFit.cover, width: double.infinity),
      ),
    );
  }
}
