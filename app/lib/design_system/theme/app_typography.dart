import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import 'app_colors.dart';

/// Typography: Plus Jakarta Sans for display/titles (personality), Inter for body (legibility).
abstract final class AppTypography {
  static TextTheme textTheme(TextTheme base) {
    final display = GoogleFonts.plusJakartaSansTextTheme(base);
    final body = GoogleFonts.interTextTheme(base);

    return base.copyWith(
      displayLarge: display.displayLarge?.copyWith(fontWeight: FontWeight.w700, color: AppColors.onSurface),
      headlineMedium: display.headlineMedium?.copyWith(fontSize: 24, fontWeight: FontWeight.w700, color: AppColors.onSurface),
      headlineSmall: display.headlineSmall?.copyWith(fontSize: 20, fontWeight: FontWeight.w600, color: AppColors.onSurface),
      titleMedium: display.titleMedium?.copyWith(fontSize: 16, fontWeight: FontWeight.w600, color: AppColors.onSurface),
      bodyLarge: body.bodyLarge?.copyWith(fontSize: 15, color: AppColors.onSurface),
      bodyMedium: body.bodyMedium?.copyWith(fontSize: 14, color: AppColors.onSurface),
      labelLarge: body.labelLarge?.copyWith(fontSize: 14, fontWeight: FontWeight.w600),
      bodySmall: body.bodySmall?.copyWith(fontSize: 12, color: AppColors.onSurfaceMuted),
    );
  }

  /// Tabular figures for XP numbers (so columns of numbers align).
  static TextStyle xpNumber(BuildContext context) => GoogleFonts.plusJakartaSans(
        fontWeight: FontWeight.w700,
        fontFeatures: const [FontFeature.tabularFigures()],
        color: AppColors.onSurface,
      );
}
