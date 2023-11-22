namespace nure_api.Models;

public class ScheduleGroup
{
    public string? time_zone { get; set; }
    public ParseEvent[]? events { get; set; }
    public ParseTeacher[]? teachers { get; set; }
    public ParseSubject[]? subjects { get; set; }
}

public class ParseEvent
{
    public int? subject_id { get; set; }
    public long? start_time { get; set; }
    public long? end_time { get; set; }
    public int? type { get; set; }
    public int? number_pair { get; set; }
    public string? auditory { get; set; }
    public int[]? teachers { get; set; }
    public int[]? groups { get; set; }
}

public class ParseTeacher
{
    public int? id { get; set; }
    public string? full_name { get; set; }
    public string? short_name { get; set; }
}

public class ParseSubject
{
    public int? id { get; set; }
    public string? title { get; set; }
    public string? brief { get; set; }
}