namespace CareHome.Api.Documents;

public static class LogoStorage
{
    public const long MaxBytes = 2 * 1024 * 1024;

    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return "Choose an image to upload.";
        }

        if (file.Length > MaxBytes)
        {
            return "Logo must be smaller than 2 MB.";
        }

        if (Extension(file) is null)
        {
            return "Logo must be a PNG, JPG, WEBP, or GIF image.";
        }

        return null;
    }

    public static string? Extension(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return extension is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif" ? extension : null;
    }

    public static string ContentType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };
    }
}
