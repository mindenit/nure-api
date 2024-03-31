using Newtonsoft.Json;

namespace nure_api.Models;

public class Teacher
{
    public long id { get; set; }
    public string shortName { get; set; }
    public string fullName { get; set; }
    
    [JsonIgnore]
    public string Schedule { get; set; }

    [JsonIgnore]
    public DateTime lastUpdated { get; set; }
    
    public static List<Teacher> Parse(string json)
    {
        List<Teacher> teachers = new List<Teacher>();
        var cistTeachers = JsonConvert.DeserializeObject<dynamic>(json);

        if (cistTeachers is not null && cistTeachers.university is not null)
        {
            if (cistTeachers.university.faculties is not null)
            {
                foreach (var faculty in cistTeachers.university.faculties)
                {
                    if (faculty.departments is not null)
                    {
                        foreach (var department in faculty.departments)
                        {
                            if (department.teachers is not null)
                            {
                                foreach (var teacher in department.teachers)
                                {
                                    teachers.Add(new Teacher(){id = teacher.id, fullName = teacher.full_name, shortName = teacher.short_name, Schedule = ""});
                                }
                            }
                            if (department.departments is not null)
                            {
                                foreach (var childDepartment in department.departments)
                                {
                                    if (childDepartment.teachers is not null)
                                    {
                                        foreach (var teacher in childDepartment.teachers)
                                        {
                                            teachers.Add(new Teacher(){id = teacher.id, fullName = teacher.full_name, shortName = teacher.short_name, Schedule = ""});
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return teachers;
    }
}