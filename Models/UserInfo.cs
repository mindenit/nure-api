namespace nure_api.Models;

public class UserInfo
{
    public object[] schedules { get; set; }
    public string? id { get; set; }
    public string? userName { get; set; }
    public string? email { get; set; }
    public int accessFailedCount { get; set; }
}