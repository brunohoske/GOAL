import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';

enum TaskLevel { easy, medium, hard }

/// A small difficulty badge (Fácil / Médio / Difícil) with a level-colored dot.
class LevelBadge extends StatelessWidget {
  const LevelBadge(this.level, {super.key});

  final TaskLevel level;

  @override
  Widget build(BuildContext context) {
    final (color, label) = switch (level) {
      TaskLevel.easy => (AppColors.levelEasy, 'Fácil'),
      TaskLevel.medium => (AppColors.levelMedium, 'Médio'),
      TaskLevel.hard => (AppColors.levelHard, 'Difícil'),
    };

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md, vertical: 6),
      decoration: BoxDecoration(
        color: AppColors.surfaceAlt,
        borderRadius: BorderRadius.circular(AppSpacing.radiusSm),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(width: 8, height: 8, decoration: BoxDecoration(color: color, shape: BoxShape.circle)),
          const SizedBox(width: 6),
          Text(label, style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: AppColors.onSurface)),
        ],
      ),
    );
  }
}
