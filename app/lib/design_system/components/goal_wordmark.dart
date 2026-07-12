import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import '../theme/app_colors.dart';

/// The GOAL brand wordmark — a stylized typographic treatment (no icon/mascot).
/// "G O A L" in bold Plus Jakarta Sans with tight tracking; the second "O" is rendered
/// as a small ring accent in indigo, a quiet nod to "hitting a target" without an icon.
class GoalWordmark extends StatelessWidget {
  const GoalWordmark({super.key, this.fontSize = 28, this.color, this.accent = true});

  final double fontSize;
  final Color? color;
  final bool accent;

  @override
  Widget build(BuildContext context) {
    final baseColor = color ?? AppColors.onSurface;
    final style = GoogleFonts.plusJakartaSans(
      fontSize: fontSize,
      fontWeight: FontWeight.w800,
      letterSpacing: fontSize * 0.06,
      color: baseColor,
      height: 1,
    );

    return Row(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.center,
      children: [
        Text('G', style: style),
        // Accent "O" rendered as a ring to evoke a target/goal, subtly.
        Padding(
          padding: EdgeInsets.symmetric(horizontal: fontSize * 0.04),
          child: _RingO(size: fontSize * 0.78, color: accent ? AppColors.primary : baseColor),
        ),
        Text('AL', style: style),
      ],
    );
  }
}

/// Inline brand label: "<prefix> GOAL" (e.g. "Novo GOAL") with the wordmark treatment,
/// for buttons, app bars and titles where the brand should carry the copy.
class GoalBrandedLabel extends StatelessWidget {
  const GoalBrandedLabel({super.key, required this.prefix, this.fontSize = 16, this.color, this.accent = true});

  final String prefix;
  final double fontSize;
  final Color? color;
  final bool accent;

  @override
  Widget build(BuildContext context) {
    final baseColor = color ?? AppColors.onSurface;
    return Row(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.center,
      children: [
        Text(
          '$prefix ',
          style: GoogleFonts.plusJakartaSans(
            fontSize: fontSize,
            fontWeight: FontWeight.w700,
            color: baseColor,
            height: 1,
          ),
        ),
        GoalWordmark(fontSize: fontSize, color: baseColor, accent: accent),
      ],
    );
  }
}

class _RingO extends StatelessWidget {
  const _RingO({required this.size, required this.color});

  final double size;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        border: Border.all(color: color, width: size * 0.16),
      ),
    );
  }
}
