using CareHome.Api.Common;

namespace CareHome.Api.Dtos.Email;

public class EmailSendLogDto
{
    public int Id { get; set; }

    public DateTimeOffset AttemptedAt { get; set; }

    public string DocumentType { get; set; } = string.Empty;

    public int DocumentId { get; set; }

    public string? Recipient { get; set; }

    public bool Success { get; set; }

    public bool Simulated { get; set; }

    public string? ErrorMessage { get; set; }
}

public class UpdateRecipientEmailRequest
{
    [OptionalEmailAddress]
    public string? RecipientEmail { get; set; }
}

public class TestEmailRequest
{
    [OptionalEmailAddress]
    public string? To { get; set; }
}
