/// Models for tasks, assignments, members and the review queue (mirror backend DTOs).

class ChecklistItem {
  const ChecklistItem({
    required this.id,
    required this.label,
    required this.isRequired,
    required this.orderIndex,
  });

  final String id;
  final String label;
  final bool isRequired;
  final int orderIndex;

  factory ChecklistItem.fromJson(Map<String, dynamic> j) => ChecklistItem(
        id: j['id'],
        label: j['label'],
        isRequired: j['isRequired'],
        orderIndex: j['orderIndex'],
      );

  Map<String, dynamic> toJson() =>
      {'id': id, 'label': label, 'isRequired': isRequired, 'orderIndex': orderIndex};
}

class TaskDef {
  const TaskDef({
    required this.id,
    required this.title,
    this.description,
    required this.estimatedXp,
    required this.requiresText,
    required this.requiresImage,
    required this.requiresAttachment,
    required this.hasChecklist,
    this.checklistItems = const [],
  });

  final String id;
  final String title;
  final String? description;
  final int estimatedXp;
  final bool requiresText;
  final bool requiresImage;
  final bool requiresAttachment;
  final bool hasChecklist;
  final List<ChecklistItem> checklistItems;

  factory TaskDef.fromJson(Map<String, dynamic> j) => TaskDef(
        id: j['id'],
        title: j['title'],
        description: j['description'],
        estimatedXp: j['estimatedXp'],
        requiresText: j['requiresText'],
        requiresImage: j['requiresImage'],
        requiresAttachment: j['requiresAttachment'],
        hasChecklist: j['hasChecklist'],
        checklistItems: ((j['checklistItems'] as List?) ?? const [])
            .map((e) => ChecklistItem.fromJson(e))
            .toList(),
      );
}

/// Assignment status mirrors the backend enum order.
enum AssignmentStatus { open, inProgress, pendingReview, approved, rejected, carriedToBacklog }

class Assignment {
  const Assignment({
    required this.id,
    required this.taskDefinitionId,
    required this.taskTitle,
    required this.status,
    this.assignedToMemberId,
    this.assignedToName,
    required this.assignedToMe,
    required this.isBacklog,
    required this.estimatedXp,
    required this.requiresImage,
    required this.requiresAttachment,
    required this.hasChecklist,
    this.completionId,
    this.approvals = 0,
    this.rejections = 0,
    this.approvalsNeeded = 0,
    this.myVote,
    this.awardedXp,
  });

  final String id;
  final String taskDefinitionId;
  final String taskTitle;
  final AssignmentStatus status;
  final String? assignedToMemberId;
  final String? assignedToName;
  final bool assignedToMe;
  final bool isBacklog;
  final int estimatedXp;
  final bool requiresImage;
  final bool requiresAttachment;
  final bool hasChecklist;

  /// Latest completion snapshot (null when nothing was submitted yet).
  final String? completionId;
  final int approvals;
  final int rejections;
  final int approvalsNeeded;

  /// 0 = aprovei, 1 = reprovei, null = não votei.
  final int? myVote;
  final int? awardedXp;

  factory Assignment.fromJson(Map<String, dynamic> j) => Assignment(
        id: j['id'],
        taskDefinitionId: j['taskDefinitionId'],
        taskTitle: j['taskTitle'],
        status: AssignmentStatus.values[j['status'] as int],
        assignedToMemberId: j['assignedToMemberId'],
        assignedToName: j['assignedToName'],
        assignedToMe: j['assignedToMe'],
        isBacklog: j['isBacklog'],
        estimatedXp: j['estimatedXp'],
        requiresImage: j['requiresImage'],
        requiresAttachment: j['requiresAttachment'],
        hasChecklist: j['hasChecklist'],
        completionId: j['completionId'],
        approvals: (j['approvals'] as int?) ?? 0,
        rejections: (j['rejections'] as int?) ?? 0,
        approvalsNeeded: (j['approvalsNeeded'] as int?) ?? 0,
        myVote: j['myVote'],
        awardedXp: j['awardedXp'],
      );
}

class Member {
  const Member({
    required this.memberId,
    required this.displayName,
    required this.isAdmin,
    required this.earnedXp,
    required this.effectiveTargetXp,
    required this.isMe,
  });

  final String memberId;
  final String displayName;
  final bool isAdmin;
  final int earnedXp;
  final int effectiveTargetXp;
  final bool isMe;

  factory Member.fromJson(Map<String, dynamic> j) => Member(
        memberId: j['memberId'],
        displayName: j['displayName'],
        isAdmin: j['isAdmin'],
        earnedXp: j['earnedXp'],
        effectiveTargetXp: j['effectiveTargetXp'],
        isMe: j['isMe'],
      );
}

class ReviewItem {
  const ReviewItem({
    required this.completionId,
    required this.taskTitle,
    required this.authorName,
    required this.textContent,
    required this.imageUrls,
    required this.approvals,
    required this.rejections,
    required this.eligibleVoters,
    required this.approvalsNeeded,
  });

  final String completionId;
  final String taskTitle;
  final String authorName;
  final String textContent;
  final List<String> imageUrls;
  final int approvals;
  final int rejections;
  final int eligibleVoters;
  final int approvalsNeeded;

  factory ReviewItem.fromJson(Map<String, dynamic> j) => ReviewItem(
        completionId: j['completionId'],
        taskTitle: j['taskTitle'],
        authorName: j['authorName'],
        textContent: j['textContent'],
        imageUrls: (j['attachments'] as List)
            .where((a) => a['type'] == 0) // 0 == Image
            .map((a) => a['url'] as String)
            .toList(),
        approvals: j['approvals'],
        rejections: j['rejections'],
        eligibleVoters: j['eligibleVoters'],
        approvalsNeeded: j['approvalsNeeded'],
      );
}
