using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace nure_api.Models;

public class Event
{
    [Key, JsonIgnore]
    public int Id { get; set; }
    public int? numberPair { get; set; }
    public Subject? subject { get; set; }
    public long? startTime { get; set; }
    public long? endTime { get; set; }
    public string? auditory { get; set; }
    public string? type { get; set; }
    public List<Teacher>? teachers { get; set; } = new List<Teacher>();
    public List<Group>? groups { get; set; } = new List<Group>();
}

public class Subject
{
    public int? id { get; set; }
    public string? title { get; set; }
    public string? brief { get; set; }
}