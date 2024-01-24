using nure_api.Models;

namespace nure_api.Services;

public class DbUtil
{
    public static bool CheckGroupExists(string group)
    {
        using var context = new Context();
        return context.Groups.Any(g => g.name == group);
    }
}