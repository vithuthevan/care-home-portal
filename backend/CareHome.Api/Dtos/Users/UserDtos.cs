namespace CareHome.Api.Dtos.Users;

public class UserDto
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public List<string> Roles { get; set; } = [];

    public List<int> CareHomeIds { get; set; } = [];
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Password { get; set; }

    public string? PasswordCipher { get; set; }

    public string Role { get; set; } = string.Empty;

    public List<int> CareHomeIds { get; set; } = [];
}

public class UpdateUserRequest
{
    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public string Role { get; set; } = string.Empty;

    public List<int> CareHomeIds { get; set; } = [];
}

public class ResetPasswordRequest
{
    public string? NewPassword { get; set; }

    public string? NewPasswordCipher { get; set; }
}

