namespace Goal.Domain.Common;

public enum GoalStatus { Active = 0, Archived = 1 }

public enum MemberRole { Member = 0, Admin = 1 }

public enum MemberStatus { Active = 0, Left = 1, Removed = 2 }

public enum InviteStatus { Pending = 0, Accepted = 1, Declined = 2, Expired = 3 }

public enum DevicePlatform { Android = 0, Ios = 1, Web = 2 }

/// <summary>How XP is computed for a task definition.</summary>
public enum XpMode
{
    /// <summary>Admin types a fixed XP value.</summary>
    Manual = 0,
    /// <summary>Scalable: base XP per difficulty level + on-time / streak bonuses.</summary>
    Scalable = 1
}

public enum Difficulty { Easy = 0, Medium = 1, Hard = 2 }

/// <summary>
/// Approval lifecycle of a catalog task. Admin-created tasks are born Approved; tasks
/// proposed by regular members stay Pending until the admin approves (optionally after
/// adjusting any field) or rejects them. Approved = 0 so pre-existing rows stay valid.
/// </summary>
public enum TaskApprovalStatus { Approved = 0, Pending = 1, Rejected = 2 }

public enum SprintStatus { Active = 0, Closing = 1, Closed = 2 }

public enum AssignmentType { SelfAssigned = 0, AdminAssigned = 1 }

public enum AssignmentStatus
{
    Open = 0,
    InProgress = 1,
    PendingReview = 2,
    Approved = 3,
    Rejected = 4,
    CarriedToBacklog = 5
}

public enum CompletionStatus
{
    PendingReview = 0,
    Approved = 1,
    Rejected = 2,
    Resubmitted = 3
}

public enum AttachmentType { Image = 0, File = 1, Link = 2 }

public enum VoteDecision { Approve = 0, Reject = 1 }

/// <summary>Source of an XP ledger credit (append-only audit trail).</summary>
public enum XpSourceType
{
    TaskCompletion = 0,
    OnTimeBonus = 1,
    StreakBonus = 2,
    Adjustment = 3
}

public enum NotificationType
{
    InviteReceived = 0,
    ReviewRequested = 1,
    CompletionApproved = 2,
    CompletionRejected = 3,
    SprintEndingSoon = 4,
    BlockedReminder = 5,
    DebtWarning = 6,
    TaskProposed = 7,
    TaskApproved = 8,
    TaskRejected = 9
}

public enum NotificationStatus { Pending = 0, Sent = 1, Read = 2, Failed = 3 }

public enum NotificationScheduleKind { BlockedNudge = 0, DebtNudge = 1, DeadlineNudge = 2 }
