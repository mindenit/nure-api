using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace nure_api.Models;

public class Event
{
    [Key]
    public int Id { get; set; }
    public int? NumberPair { get; set; }
    public Subject? Subject { get; set; }
    public long? StartTime { get; set; }
    public long? EndTime { get; set; }
    public string? Auditory { get; set; }
    public string? Type { get; set; }
    public List<Teacher>? Teachers { get; set; } = new List<Teacher>();
    public List<Group>? Groups { get; set; } = new List<Group>();
}

public class Subject
{
    public int? Id { get; set; }
    public string? Title { get; set; }
    public string? Brief { get; set; }
}