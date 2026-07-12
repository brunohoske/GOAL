import '../../goals/domain/goal_models.dart';

/// Mirrors the backend BlockingStateDto. The app reflects this; it never decides blocking itself.
class BlockingState {
  const BlockingState({
    required this.isBlocked,
    required this.currentPct,
    required this.targetPct,
    required this.earnedXp,
    required this.effectiveTargetXp,
    required this.targetXp,
    required this.unblockThresholdXp,
    required this.debtXp,
    required this.daysRemaining,
    required this.requiresFullCompletion,
    required this.xpRemainingToUnblock,
    required this.blockedApps,
    this.randomOverlayActive = false,
    this.typingSabotageActive = false,
    this.typingSabotageText,
  });

  final bool isBlocked;
  final double currentPct;
  final double targetPct;
  final int earnedXp;
  final int effectiveTargetXp;
  final int targetXp;
  final int unblockThresholdXp;
  final int debtXp;
  final int daysRemaining;
  final bool requiresFullCompletion;
  final int xpRemainingToUnblock;
  final List<BlockedApp> blockedApps;
  final bool randomOverlayActive;
  final bool typingSabotageActive;
  final String? typingSabotageText;

  factory BlockingState.fromJson(Map<String, dynamic> j) => BlockingState(
        isBlocked: j['isBlocked'],
        currentPct: (j['currentPct'] as num).toDouble(),
        targetPct: (j['targetPct'] as num).toDouble(),
        earnedXp: j['earnedXp'],
        effectiveTargetXp: j['effectiveTargetXp'],
        targetXp: j['targetXp'],
        unblockThresholdXp: j['unblockThresholdXp'],
        debtXp: j['debtXp'],
        daysRemaining: j['daysRemaining'],
        requiresFullCompletion: j['requiresFullCompletion'],
        xpRemainingToUnblock: j['xpRemainingToUnblock'],
        blockedApps: (j['blockedApps'] as List).map((e) => BlockedApp.fromJson(e)).toList(),
        randomOverlayActive: (j['randomOverlayActive'] as bool?) ?? false,
        typingSabotageActive: (j['typingSabotageActive'] as bool?) ?? false,
        typingSabotageText: j['typingSabotageText'] as String?,
      );
}
