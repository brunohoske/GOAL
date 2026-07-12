import 'package:flutter/material.dart';

/// The Goal palette — minimalist, elegant, light & warm. Hex values are fixed product tokens.
/// Direction: solid soft fills over gradients; the XP gradient is a restrained accent only.
abstract final class AppColors {
  static const primary = Color(0xFF5B5BD6); // indigo — brand wordmark, primary actions
  static const primaryContainer = Color(0xFFECECFB);
  static const secondary = Color(0xFF0FB5A6); // teal — positive progress / XP
  static const secondaryContainer = Color(0xFFE0F6F3);

  // Warm off-white surfaces (a touch of warmth, kept very subtle).
  static const background = Color(0xFFFCFBFA);
  static const surface = Color(0xFFFFFFFF);
  static const surfaceAlt = Color(0xFFF4F3F1);

  static const onSurface = Color(0xFF1A1B25);
  static const onSurfaceMuted = Color(0xFF6B6E80);
  static const outline = Color(0xFFEAE8E4);

  // Semantic / domain colors
  static const success = Color(0xFF22B07D); // approved
  static const warning = Color(0xFFF2A93B); // pending review
  static const danger = Color(0xFFE5484D); // blocked / rejected
  static const debt = Color(0xFF7A3FF2); // accumulated debt

  // Soft tints for status chip backgrounds (minimalist, low-saturation).
  static const successTint = Color(0xFFE4F6EE);
  static const warningTint = Color(0xFFFDF1DE);
  static const dangerTint = Color(0xFFFCE7E8);
  static const debtTint = Color(0xFFEFE7FD);

  // Difficulty levels
  static const levelEasy = Color(0xFF22B07D);
  static const levelMedium = Color(0xFFF2A93B);
  static const levelHard = Color(0xFFE5484D);

  // XP gradient — used sparingly as an accent (e.g. progress fill).
  static const xpGradient = LinearGradient(
    colors: [primary, secondary],
    begin: Alignment.centerLeft,
    end: Alignment.centerRight,
  );
}
