import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../design_system/components/empty_state.dart';
import '../../../design_system/components/status_chip.dart';
import '../../../design_system/components/xp_progress_bar.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../../goals/data/goals_repository.dart';

/// Blocking hub: shows, per goal, whether the user is blocked and what's left to unblock.
/// Reflects backend-computed state; the native module enforces it.
class BlockingHubScreen extends ConsumerWidget {
  const BlockingHubScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final goalsAsync = ref.watch(goalsListProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Bloqueio')),
      body: goalsAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, __) => const EmptyState(icon: Icons.cloud_off, title: 'Não foi possível carregar'),
        data: (goals) {
          if (goals.isEmpty) {
            return const EmptyState(
              icon: Icons.lock_open,
              title: 'Nada bloqueado',
              message: 'Você ainda não participa de nenhum GOAL.',
            );
          }
          return ListView.separated(
            padding: const EdgeInsets.all(AppSpacing.lg),
            itemCount: goals.length,
            separatorBuilder: (_, __) => const SizedBox(height: AppSpacing.md),
            itemBuilder: (_, i) => _GoalBlockingTile(goalId: goals[i].id, title: goals[i].title),
          );
        },
      ),
    );
  }
}

class _GoalBlockingTile extends ConsumerWidget {
  const _GoalBlockingTile({required this.goalId, required this.title});
  final String goalId;
  final String title;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final bsAsync = ref.watch(blockingStateProvider(goalId));
    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(AppSpacing.radiusMd),
        onTap: () => context.push('/goals/$goalId'),
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.lg),
          child: bsAsync.when(
            loading: () => const SizedBox(height: 48, child: Center(child: CircularProgressIndicator())),
            error: (_, __) => Text(title),
            data: (bs) => Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(child: Text(title, style: Theme.of(context).textTheme.titleMedium)),
                    bs.isBlocked ? StatusChip.blocked() : StatusChip.approved('Livre'),
                  ],
                ),
                const SizedBox(height: AppSpacing.md),
                XpProgressBar(earned: bs.earnedXp, target: bs.effectiveTargetXp, thresholdXp: bs.targetXp),
                const SizedBox(height: AppSpacing.sm),
                Text(
                  bs.isBlocked
                      ? 'Faltam ${bs.xpRemainingToUnblock} XP — ${bs.blockedApps.map((a) => a.displayName).join(', ')}'
                      : 'Apps liberados',
                  style: TextStyle(
                    fontSize: 13,
                    color: bs.isBlocked ? AppColors.danger : AppColors.onSurfaceMuted,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
