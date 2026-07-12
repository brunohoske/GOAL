using Goal.Domain.Common;

namespace Goal.Domain.Completions;

public class CompletionAttachment : Entity
{
    public Guid TaskCompletionId { get; set; }
    public TaskCompletion? TaskCompletion { get; set; }

    public AttachmentType Type { get; set; }
    public string Url { get; set; } = default!;
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
}
