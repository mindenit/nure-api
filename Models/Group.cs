using Newtonsoft.Json;

namespace nure_api.Models;

public class Group
{
    public int Id { get; set; }
    public string Name { get; set; }

    [JsonIgnore] public string? Schedule { get; set; }

    [JsonIgnore]
    public DateTime lastUpdated { get;set; }
    
    public static List<Group> Parse(string json)
    {
        List<Group> groups = new List<Group>();
        var cistGroups = JsonConvert.DeserializeObject<dynamic>(json);

        if (cistGroups is not null && cistGroups.university is not null)
        {
            if (cistGroups.university.faculties is not null)
            {
                foreach (var faculty in cistGroups.university.faculties)
                {
                    if (faculty.directions is not null)
                    {
                        foreach (var direction in faculty.directions)
                        {
                            if (direction.groups is not null)
                            {
                                foreach (var group in direction.groups)
                                {
                                    groups.Add(new Group(){ Id = group.id, Name = group.name, Schedule = ""});
                                }
                            }
                            if (direction.specialities is not null)
                            {
                                foreach (var specialition in direction.specialities)
                                {
                                    if (specialition.groups is not null)
                                    {
                                        foreach (var group in specialition.groups)
                                        {
                                            groups.Add(new Group(){ Id = group.id, Name = group.name, Schedule = ""});
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return groups;
    }
}
