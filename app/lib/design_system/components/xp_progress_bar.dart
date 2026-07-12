import 'package:flutter/material.dart';

import '../theme/app_colors.dart';

/// A thin, rounded XP progress bar. Shows earned progress toward the effective target,
/// with an optional marker for the unblock threshold. Minimalist: flat track, gradient fill.
class XpProgressBar extends StatelessWidget {
  const XpProgressBar({
    super.key,
    required this.earned,
    required this.target,
    this.thresholdXp,
    this.height = 10,
  });

  final int earned;
  final int target;
  final int? thresholdXp;
  final double height;

  @override
  Widget build(BuildContext context) {
    final pct = target <= 0 ? 1.0 : (earned / target).clamp(0.0, 1.0);
    final thresholdPct = (thresholdXp != null && target > 0)
        ? (thresholdXp! / target).clamp(0.0, 1.0)
        : null;

    return LayoutBuilder(
      builder: (context, constraints) {
        final width = constraints.maxWidth;
        return SizedBox(
          height: height,
          child: Stack(
            children: [
              // Track
              Container(
                decoration: BoxDecoration(
                  color: AppColors.surfaceAlt,
                  borderRadius: BorderRadius.circular(height),
                ),
              ),
              // Fill
              FractionallySizedBox(
                widthFactor: pct,
                child: Container(
                  decoration: BoxDecoration(
                    gradient: AppColors.xpGradient,
                    borderRadius: BorderRadius.circular(height),
                  ),
                ),
              ),
              // Unblock threshold marker
              if (thresholdPct != null)
                Positioned(
                  left: (width * thresholdPct).clamp(0.0, width - 2),
                  top: 0,
                  bottom: 0,
                  child: Container(width: 2, color: AppColors.onSurface.withValues(alpha: 0.35)),
                ),
            ],
          ),
        );
      },
    );
  }
}
