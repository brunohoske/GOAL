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
              return const EmptyState(
                icon: Icons.flag_outlined,
                title: 'Nenhum GOAL ainda',
                message: 'Crie um GOAL e convide seus amigos para começar.',
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
}
