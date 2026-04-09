using Postgrest.Attributes;
using Postgrest.Models;

namespace UserService.Domain.Entities;

[Table("reports")]
public class Report : BaseModel
{
    public static class Statuses
    {
        public const string Pending = "pending";
        public const string Assigned = "assigned";
        public const string Resolved = "resolved";
        public const string Dismissed = "dismissed";
    }

    [PrimaryKey("id", true)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("reporter_id")]
    public string ReporterId { get; set; } = string.Empty;

    [Column("reporter_name_snapshot")]
    public string ReporterNameSnapshot { get; set; } = string.Empty;

    [Column("content_type")]
    public string ContentType { get; set; } = string.Empty;

    [Column("target_id")]
    public string TargetId { get; set; } = string.Empty;

    [Column("target_name_snapshot")]
    public string TargetNameSnapshot { get; set; } = string.Empty;

    [Column("reason_id")]
    public string ReasonId { get; set; } = string.Empty;

    [Column("reason_label")]
    public string ReasonLabel { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = Statuses.Pending;

    [Column("admin_notes")]
    public string? AdminNotes { get; set; }

    [Column("assigned_admin_id")]
    public string? AssignedAdminId { get; set; }

    [Column("resolved_at")]
    public DateTime? ResolvedAt { get; set; }

    [Column("dismissed_at")]
    public DateTime? DismissedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public void ApplyAction(string action, string adminUserId, string? note = null)
    {
        var normalizedAction = action.Trim().ToLowerInvariant();

        switch (normalizedAction)
        {
            case "assign":
                ApplyAssign(adminUserId, note);
                break;
            case "resolve":
                ApplyResolve(adminUserId, note);
                break;
            case "dismiss":
                ApplyDismiss(adminUserId, note);
                break;
            default:
                throw new InvalidOperationException($"不支持的举报动作: {action}");
        }
    }

    private void ApplyAssign(string adminUserId, string? note)
    {
        EnsureCanTransition(Statuses.Pending);

        AssignedAdminId = adminUserId;
        Status = Statuses.Assigned;
        AdminNotes = string.IsNullOrWhiteSpace(note) ? AdminNotes : note.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    private void ApplyResolve(string adminUserId, string? note)
    {
        EnsureCanTransition(Statuses.Pending, Statuses.Assigned);

        AssignedAdminId ??= adminUserId;
        Status = Statuses.Resolved;
        ResolvedAt = DateTime.UtcNow;
        AdminNotes = string.IsNullOrWhiteSpace(note) ? AdminNotes : note.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    private void ApplyDismiss(string adminUserId, string? note)
    {
        EnsureCanTransition(Statuses.Pending, Statuses.Assigned);

        AssignedAdminId ??= adminUserId;
        Status = Statuses.Dismissed;
        DismissedAt = DateTime.UtcNow;
        AdminNotes = string.IsNullOrWhiteSpace(note) ? AdminNotes : note.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureCanTransition(params string[] allowedStatuses)
    {
        if (allowedStatuses.Contains(Status, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException($"举报当前状态为 {Status}，不允许执行该操作");
    }
}