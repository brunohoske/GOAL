import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../design_system/components/empty_state.dart';
import '../../../design_system/components/goal_wordmark.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/goals_repository.dart';
import 'widgets/goal_card.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final goalsAsync = ref.watch(goalsListProvider);

    return Scaffold(
      appBar: AppBar(
        titleSpacing: AppSpacing.lg,
        title: const GoalWordmark(fontSize: 24),
        actions: [
          IconButton(
            icon: const Icon(Icons.vpn_key_outlined),
            tooltip: 'Entrar com código',
            onPressed: () => _joinWithCode(context, ref),
          ),
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: 'Sair',
            onPressed: () => ref.read(authControllerProvider.notifier).logout(),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push('/goals/create'),
        icon: const Icon(Icons.add),
        label: const GoalBrandedLabel(prefix: 'Novo', fontSize: 15, color: AppColors.primary),
      ),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(goalsListProvider),
        child: goalsAsync.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) => EmptyState(
            icon: Icons.cloud_off,
            title: 'Não foi possível carregar',
            message: 'Verifique sua conexão e tente novamente.',
            action: FilledButton(
              onPressed: () => ref.invalidate(goalsListProvider),
              child: const Text('Tentar de novo'),
            ),
          ),
          data: (goals) {
            if (goals.isEmpty) {
              return EmptyState(
                icon: Icons.flag_outlined,
                title: 'Nenhum GOAL ainda',
                message: 'Crie um GOAL e convide seus amigos — ou entre num GOAL existente com o código.',
                action: OutlinedButton.icon(
                  onPressed: () => _joinWithCode(context, ref),
                  icon: const Icon(Icons.vpn_key_outlined, size: 18),
                  label: const Text('Entrar com código'),
                ),
              );
            }
            return ListView.separated(
              padding: const EdgeInsets.all(AppSpacing.lg),
              itemCount: goals.length,
              separatorBuilder: (_, __) => const SizedBox(height: AppSpacing.md),
              itemBuilder: (_, i) => GoalCard(
                goal: goals[i],
                onTap: () => context.push('/goals/${goals[i].id}'),
              ),
            );
          },
        ),
      ),
    );
  }

  /// Bottom sheet: type a friend's GOAL code to join it.
  void _joinWithCode(BuildContext context, WidgetRef ref) {
    final codeCtrl = TextEditingController();
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(AppSpacing.radiusLg)),
      ),
      builder: (ctx) => Padding(
        padding: EdgeInsets.fromLTRB(AppSpacing.xl, AppSpacing.xl, AppSpacing.xl,
            MediaQuery.of(ctx).viewInsets.bottom + AppSpacing.xl),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Entrar num GOAL', style: Theme.of(ctx).textTheme.headlineSmall),
            const SizedBox(height: AppSpacing.sm),
            Text('Peça o código para quem criou o GOAL — ele aparece na tela do GOAL.',
                style: Theme.of(ctx).textTheme.bodySmall),
            const SizedBox(height: AppSpacing.lg),
            TextField(
              controller: codeCtrl,
              textCapitalization: TextCapitalization.characters,
              autofocus: true,
              style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w700, letterSpacing: 4),
              textAlign: TextAlign.center,
              decoration: const InputDecoration(hintText: 'EX: K7M2XQ'),
            ),
            const SizedBox(height: AppSpacing.lg),
            FilledButton(
              onPressed: () async {
                final code = codeCtrl.text.trim();
                if (code.isEmpty) return;
                try {
                  final goalId = await ref.read(goalsRepositoryProvider).joinByCode(code);
                  ref.invalidate(goalsListProvider);
                  if (ctx.mounted) Navigator.pop(ctx);
                  if (context.mounted) context.push('/goals/$goalId');
                } catch (e) {
                  if (ctx.mounted) {
                    ScaffoldMessenger.of(ctx).showSnackBar(
                      const SnackBar(content: Text('Código inválido ou você já participa desse GOAL.')),
                    );
                  }
                }
              },
              child: const Text('Entrar'),
            ),
          ],
        ),
      ),
    );
  }
}
