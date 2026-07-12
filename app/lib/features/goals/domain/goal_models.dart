/// Hand-written models (no codegen needed) mirroring the backend DTOs.

class BlockedApp {
  const BlockedApp({required this.packageName, required this.displayName});
  final String packageName;
  final String displayName;

  factory BlockedApp.fromJson(Map<String, dynamic> j) =>
      BlockedApp(packageName: j['packageName'], displayName: j['displayName']);
  Map<String, dynamic> toJson() => {'packageName': packageName, 'displayName': displayName};
}

class GoalSettings {
  const GoalSettings({
    required this.sprintDurationDays,
    required this.baseXpTargetPerSprint,
    required this.unblockThresholdPct,
    required this.finalTriggerDaysBefore,
    required this.finalTriggerTargetPct,
    required this.voteApprovalThreshold,
    required this.debtCarryEnabled,
    required this.xpScalableEasy,
    required this.xpScalableMedium,
    required this.xpScalableHard,
    required this.blockedApps,
  });

  final int sprintDurationDays;
  final int baseXpTargetPerSprint;
  final double unblockThresholdPct;
  final int finalTriggerDaysBefore;
  final double finalTriggerTargetPct;
  final double voteApprovalThreshold;
  final bool debtCarryEnabled;
  final int xpScalableEasy;
  final int xpScalableMedium;
  final int xpScalableHard;
  final List<BlockedApp> blockedApps;

  factory GoalSettings.fromJson(Map<String, dynamic> j) => GoalSettings(
        sprintDurationDays: j['sprintDurationDays'],
        baseXpTargetPerSprint: j['baseXpTargetPerSprint'],
        unblockThresholdPct: (j['unblockThresholdPct'] as num).toDouble(),
        finalTriggerDaysBefore: j['finalTriggerDaysBefore'],
        finalTriggerTargetPct: (j['finalTriggerTargetPct'] as num).toDouble(),
        voteApprovalThreshold: (j['voteApprovalThreshold'] as num).toDouble(),
        debtCarryEnabled: j['debtCarryEnabled'],
        xpScalableEasy: j['xpScalableEasy'],
        xpScalableMedium: j['xpScalableMedium'],
        xpScalableHard: j['xpScalableHard'],
        blockedApps: (j['blockedApps'] as List).map((e) => BlockedApp.fromJson(e)).toList(),
      );

  Map<String, dynamic> toJson() => {
        'sprintDurationDays': sprintDurationDays,
        'baseXpTargetPerSprint': baseXpTargetPerSprint,
        'unblockThresholdPct': unblockThresholdPct,
        'finalTriggerDaysBefore': finalTriggerDaysBefore,
        'finalTriggerTargetPct': finalTriggerTargetPct,
        'voteApprovalThreshold': voteApprovalThreshold,
        'debtCarryEnabled': debtCarryEnabled,
        'xpScalableEasy': xpScalableEasy,
        'xpScalableMedium': xpScalableMedium,
        'xpScalableHard': xpScalableHard,
        'blockedApps': blockedApps.map((e) => e.toJson()).toList(),
      };
}

class GoalSummary {
  const GoalSummary({
    required this.id,
    required this.title,
    this.description,
    required this.isAdmin,
    this.currentSprintNumber,
    this.sprintEndsAt,
    required this.memberCount,
  });

  final String id;
  final String title;
  final String? description;
  final bool isAdmin;
  final int? currentSprintNumber;
  final DateTime? sprintEndsAt;
  final int memberCount;

  factory GoalSummary.fromJson(Map<String, dynamic> j) => GoalSummary(
        id: j['id'],
        title: j['title'],
        description: j['description'],
        isAdmin: j['isAdmin'],
        currentSprintNumber: j['currentSprintNumber'],
        sprintEndsAt: j['sprintEndsAt'] == null ? null : DateTime.parse(j['sprintEndsAt']),
        memberCount: j['memberCount'],
      );
}

class GoalDetail {
  const GoalDetail({
    required this.id,
    required this.title,
    this.description,
    required this.joinCode,
    required this.adminUserId,
    required this.timeZone,
    required this.settings,
    this.currentSprintId,
    this.currentSprintNumber,
    this.currentSprintEndsAt,
  });

  final String id;
  final String title;
  final String? description;
  final String joinCode;
  final String adminUserId;
  final String timeZone;
  final GoalSettings settings;
  final String? currentSprintId;
  final int? currentSprintNumber;
  final DateTime? currentSprintEndsAt;

  factory GoalDetail.fromJson(Map<String, dynamic> j) => GoalDetail(
        id: j['id'],
        title: j['title'],
        description: j['description'],
        joinCode: (j['joinCode'] as String?) ?? '',
        adminUserId: j['adminUserId'],
        timeZone: j['timeZone'],
        settings: GoalSettings.fromJson(j['settings']),
        currentSprintId: j['currentSprintId'],
        currentSprintNumber: j['currentSprintNumber'],
        currentSprintEndsAt: j['currentSprintEndsAt'] == null
            ? null
            : DateTime.parse(j['currentSprintEndsAt']).toLocal(),
      );
}
