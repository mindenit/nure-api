using nure_api.Models;

namespace nure_api.Services;

public class DbUtil
{
    public static bool CheckExists(Schedule item)
    {
        using var context = new Context();
        if(item.type == "group")
        {
            return context.Groups.Any(x => x.id == item.id);
        }
        if(item.type == "teacher")
        {
            return context.Teachers.Any(x => x.id == item.id);
        }
        else
        {
            return context.Auditories.Any(x => x.id == item.id);
        }
    }
}