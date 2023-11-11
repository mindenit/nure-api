using Newtonsoft.Json;

namespace nure_api.Models;

public class Teacher
{
    public int Id { get; set; }
    public string ShortName { get; set; }
    public string FullName { get; set; }
    
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
                                    teachers.Add(new Teacher(){Id = teacher.id, FullName = teacher.full_name, ShortName = teacher.short_name});
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
                                            teachers.Add(new Teacher(){Id = teacher.id, FullName = teacher.full_name, ShortName = teacher.short_name});
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