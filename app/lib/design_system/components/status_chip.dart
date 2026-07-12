import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';

/// A small, low-saturation status pill. Minimalist: soft tint background, no border.
class StatusChip extends StatelessWidget {
  const StatusChip._(this.label, this.fg, this.bg);

  final String label;
  final Color fg;
  final Color bg;

  factory StatusChip.pending([String label = 'Em revisão']) =>
      StatusChip._(label, AppColors.warning, AppColors.warningTint);
  factory StatusChip.approved([String label = 'Aprovado']) =>
      StatusChip._(label, AppColors.success, AppColors.successTint);
  factory StatusChip.blocked([String label = 'Bloqueado']) =>
      StatusChip._(label, AppColors.danger, AppColors.dangerTint);
  factory StatusChip.rejected([String label = 'Refazer']) =>
      StatusChip._(label, AppColors.danger, AppColors.dangerTint);
  factory StatusChip.debt(String label) =>
      StatusChip._(label, AppColors.debt, AppColors.debtTint);
  factory StatusChip.neutral(String label) =>
      StatusChip._(label, AppColors.onSurfaceMuted, AppColors.surfaceAlt);

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md, vertical: 6),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(AppSpacing.radiusSm),
      ),
      child: Text(
        label,
        style: TextStyle(color: fg, fontSize: 12, fontWeight: FontWeight.w600),
      ),
    );
  }
}
