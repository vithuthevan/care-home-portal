namespace CareHome.Api.Dtos.Auth;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;

    public string? Password { get; set; }

    public string? PasswordCipher { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = [];

    public List<int> CareHomeIds { get; set; } = [];

    public string? TenantName { get; set; }

    public Guid? TenantPublicId { get; set; }

    public bool MustChangePassword { get; set; }

    public bool ShowGuardian { get; set; }

    public bool FinanceModuleEnabled { get; set; }
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public string? NewPassword { get; set; }

    public string? NewPasswordCipher { get; set; }
}

public class ChangePasswordRequest
{
    public string? CurrentPassword { get; set; }

    public string? NewPassword { get; set; }

    public string? CurrentPasswordCipher { get; set; }

    public string? NewPasswordCipher { get; set; }
}

