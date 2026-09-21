namespace CareHome.Api.Common;

public static class EntityRouteKey
{
    public static bool TryParse(string key, out Guid publicId, out int id)
    {
        publicId = default;
        id = default;

        if (Guid.TryParse(key, out publicId))
        {
            return true;
        }

        return int.TryParse(key, out id);
    }
}
