import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../design_system/components/goal_progress_ring.dart';
import '../../../../design_system/components/status_chip.dart';
import '../../../../design_system/theme/app_colors.dart';
import '../../../../design_system/theme/app_spacing.dart';
import '../../data/goals_repository.dart';
import '../../domain/goal_models.dart';

/// A minimalist goal card: title, sprint meta, member count — and the brand ring
/// showing this member's sprint progress at a glance.
class GoalCard extends ConsumerWidget {
  const GoalCard({super.key, required this.goal, required this.onTap});

  final GoalSummary goal;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final daysLeft = goal.sprintEndsAt == null
        ? null
        : goal.sprintEndsAt!.difference(DateTime.now()).inDays;
    final blocking = ref.watch(blockingStateProvider(goal.id)).valueOrNull;

    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(AppSpacing.radiusMd),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.lg),
          child: Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(goal.title,
                              style: Theme.of(context).textTheme.titleMedium,
                              maxLines: 1, overflow: TextOverflow.ellipsis),
                        ),
                        if (goal.isAdmin) StatusChip.neutral('Admin'),
                      ],
                    ),
                    if (goal.description != null) ...[
                      const SizedBox(height: AppSpacing.xs),
                      Text(goal.description!,
                          style: Theme.of(context).textTheme.bodySmall,
                          maxLines: 1, overflow: TextOverflow.ellipsis),
                    ],
                    const SizedBox(height: AppSpacing.md),
                    Row(
                      children: [
                        _Meta(icon: Icons.bolt_outlined, label: goal.currentSprintNumber == null
                            ? 'Sem sprint'
                            : 'Sprint ${goal.currentSprintNumber}'),
                        const SizedBox(width: AppSpacing.lg),
                        if (daysLeft != null)
                          _Meta(icon: Icons.schedule, label: daysLeft <= 0 ? 'Encerrando' : '$daysLeft dias'),
                        const Spacer(),
                        _Meta(icon: Icons.group_outlined, label: '${goal.memberCount}'),
                      ],
                    ),
                  ],
                ),
              ),
              if (blocking != null && blocking.effectiveTargetXp > 0) ...[
                const SizedBox(width: AppSpacing.lg),
                GoalProgressRing(
                  progress: blocking.earnedXp / blocking.effectiveTargetXp,
                  threshold: (blocking.targetXp / blocking.effectiveTargetXp).clamp(0.0, 1.0).toDouble(),
                  size: 52,
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _Meta extends StatelessWidget {
  const _Meta({required this.icon, required this.label});
  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 16, color: AppColors.onSurfaceMuted),
        const SizedBox(width: 4),
        Text(label, style: const TextStyle(fontSize: 13, color: AppColors.onSurfaceMuted)),
      ],
    );
  }
}
