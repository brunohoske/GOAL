import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../theme/app_colors.dart';

/// The brand's "O" as a living element: a ring that closes as sprint progress grows.
///
/// Reading it is immediate: the colored arc is how much of the XP target you've earned
/// (with the % in the center), and the small tick on the ring marks the unblock point —
/// amber while you're short, green once you've passed it.
class GoalProgressRing extends StatelessWidget {
  const GoalProgressRing({
    super.key,
    required this.progress,
    this.threshold,
    this.size = 104,
    this.showLabel = true,
    this.sublabel,
  });

  /// 0..1 fraction of the effective XP target already earned.
  final double progress;

  /// 0..1 fraction of the target where apps unblock (draws the tick). Omit to hide.
  final double? threshold;

  final double size;
  final bool showLabel;
  final String? sublabel;

  @override
  Widget build(BuildContext context) {
    final clamped = progress.clamp(0.0, 1.0).toDouble();
    final reached = threshold == null ? clamped >= 1.0 : clamped >= threshold!;

    return TweenAnimationBuilder<double>(
      tween: Tween(begin: 0, end: clamped),
      duration: const Duration(milliseconds: 900),
      curve: Curves.easeOutCubic,
      builder: (context, animated, _) {
        return SizedBox(
          width: size,
          height: size,
          child: Stack(
            fit: StackFit.expand,
            children: [
              CustomPaint(
                painter: _RingPainter(
                  progress: animated,
                  threshold: threshold,
                  reached: reached,
                  strokeWidth: size * 0.10,
                ),
              ),
              if (showLabel)
                Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        '${(animated * 100).round()}%',
                        style: TextStyle(
                          fontSize: size * 0.22,
                          fontWeight: FontWeight.w800,
                          color: reached ? AppColors.secondary : AppColors.onSurface,
                          height: 1,
                        ),
                      ),
                      if (sublabel != null) ...[
                        SizedBox(height: size * 0.03),
                        Text(
                          sublabel!,
                          style: TextStyle(fontSize: size * 0.10, color: AppColors.onSurfaceMuted),
                        ),
                      ],
                    ],
                  ),
                ),
            ],
          ),
        );
      },
    );
  }
}

class _RingPainter extends CustomPainter {
  _RingPainter({
    required this.progress,
    required this.threshold,
    required this.reached,
    required this.strokeWidth,
  });

  final double progress;
  final double? threshold;
  final bool reached;
  final double strokeWidth;

  static const _start = -math.pi / 2; // 12 o'clock

  @override
  void paint(Canvas canvas, Size size) {
    final center = size.center(Offset.zero);
    final radius = (size.shortestSide - strokeWidth) / 2;
    final rect = Rect.fromCircle(center: center, radius: radius);

    // Quiet track: the part of the ring still to be earned.
    final track = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = strokeWidth
      ..color = AppColors.surfaceAlt;
    canvas.drawCircle(center, radius, track);

    // Progress arc with the brand gradient (indigo -> teal), starting at the top.
    if (progress > 0) {
      final arc = Paint()
        ..style = PaintingStyle.stroke
        ..strokeWidth = strokeWidth
        ..strokeCap = StrokeCap.round
        ..shader = SweepGradient(
          colors: const [AppColors.primary, AppColors.secondary],
          transform: const GradientRotation(_start),
        ).createShader(rect);
      canvas.drawArc(rect, _start, 2 * math.pi * progress, false, arc);
    }

    // Unblock tick: where the apps free up.
    final t = threshold;
    if (t != null && t > 0 && t < 1) {
      final angle = _start + 2 * math.pi * t;
      final dir = Offset(math.cos(angle), math.sin(angle));
      final tick = Paint()
        ..strokeWidth = strokeWidth * 0.35
        ..strokeCap = StrokeCap.round
        ..color = reached ? AppColors.success : AppColors.warning;
      canvas.drawLine(
        center + dir * (radius - strokeWidth * 0.85),
        center + dir * (radius + strokeWidth * 0.85),
        tick,
      );
    }
  }

  @override
  bool shouldRepaint(_RingPainter old) =>
      old.progress != progress ||
      old.threshold != threshold ||
      old.reached != reached ||
      old.strokeWidth != strokeWidth;
}
