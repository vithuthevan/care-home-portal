namespace CareHome.Api.Dtos.Audit
{
    public class AuditLogDto
    {
        public long Id { get; set; }

        public string? UserId { get; set; }

        public string? UserDisplayName { get; set; }

        public DateTimeOffset LoggedAt { get; set; }

        public string EntityType { get; set; } = string.Empty;

        public string? EntityId { get; set; }

        public string Action { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}

