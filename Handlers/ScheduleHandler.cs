using System.Net;
using System.Text;
using Newtonsoft.Json;
using nure_api.Models;
using JsonRepairUtils;

namespace nure_api.Handlers;

public class ScheduleHandler
{
    private static string getType(int id)
    {
        if (id == 10 || id == 12)
        {
            return "Пз";
        }
        else if (id == 20 | id == 21 || id == 22 || id == 23 || id == 24)
        {
            return "Лб";
        }
        else if (id == 30)
        {
            return "Конс";
        }
        else if (id == 40 || id == 41)
        {
            return "Зал";
        }
        else if (id == 50 || id == 51 || id == 52 || id == 53 || id == 54 || id == 55)
        {
            return "Екз";
        }
        else if (id == 60)
        {
            return "КП/КР";
        }

        return "Лк";
    }

    private static Auditory? getAuditory(string Name)
    {
        List<Auditory> auditories = new List<Auditory>();
        using (var context = new Context())
        {
            auditories = context.Auditories.ToList();
        }

        foreach (var auditory in auditories)
        {
            if (auditory.Name.ToUpper() == Name.ToUpper())
            {
                return auditory;
            }
        }

        return null;
    }

    private static Subject? findSubjectById(dynamic subjects, int id)
    {
        foreach (var subject in subjects)
        {
            if (subject.id == id)
            {
                return new Subject()
                {
                    Id = subject.id,
                    Title = subject.title,
                    Brief = subject.brief
                };
            }
        }

        return null; // якщо клас з таким ідентифікатором не знайдено
    }

    private static Teacher? findTeacherById(dynamic teachers, int id)
    {
        foreach (var teacher in teachers)
        {
            if (teacher.id == id)
            {
                return new Teacher()
                {
                    Id = teacher.id,
                    FullName = teacher.full_name,
                    ShortName = teacher.short_name
                };
            }
        }

        return null; // якщо клас з таким ідентифікатором не знайдено
    }

    private static Group? findGroupById(dynamic groups, int id)
    {
        foreach (var group in groups)
        {
            if (group.id == id)
            {
                return new Group()
                {
                    Id = group.id,
                    Name = group.full_name
                };
            }
        }

        return null; // якщо клас з таким ідентифікатором не знайдено
    }

    private static string Download(long Id, int Type)
    {
        using (HttpClient httpClient = new HttpClient())
        {
            var webRequest = WebRequest.Create($"https://cist.nure.ua/ias/app/tt/P_API_EVEN_JSON?" +
                                               $"type_id={Type}" +
                                               $"&timetable_id={Id}" +
                                               "&idClient=KNURESked") as HttpWebRequest;

            webRequest.ContentType = "application/json";

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (var webResponse = webRequest.GetResponse())
            using (var streamReader =
                   new StreamReader(webResponse.GetResponseStream(), Encoding.GetEncoding("windows-1251")))
            using (var memoryStream = new MemoryStream())
            using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8))
            {
                streamWriter.Write(streamReader.ReadToEnd());
                streamWriter.Flush();
                memoryStream.Position = 0;

                var json = Encoding.UTF8.GetString(memoryStream.ToArray());

                // Remove BOM
                json = json.TrimStart('\uFEFF');

                return json;
            }
        }
    }

    public static void Init()
    {
        List<Group> groups = new List<Group>();
        List<Auditory> auditories = new List<Auditory>();
        List<Teacher> teachers = new List<Teacher>();

        var jsonRepair = new JsonRepair();

        using (var context = new Context())
        {
            groups = context.Groups.ToList();
            auditories = context.Auditories.ToList();
            teachers = context.Teachers.ToList();


            foreach (var group in groups)
            {
                Console.WriteLine(group.Name);
                try
                {
                    group.Schedule = Parse(jsonRepair.Repair(Download(group.Id, 1)));
                    group.lastUpdated = DateTime.Now;
                }
                catch (Exception e)
                {
                    group.Schedule = new List<Event>();
                    group.lastUpdated = DateTime.Now;
                }
            }

            foreach (var teacher in teachers)
            {
                Console.WriteLine(teacher.ShortName);
                try
                {
                    teacher.Schedule = Parse(jsonRepair.Repair(Download(teacher.Id, 2)));
                    teacher.lastUpdated = DateTime.Now;
                }
                catch (Exception e)
                {
                    teacher.Schedule = new List<Event>();
                    teacher.lastUpdated = DateTime.Now;
                }
            }

            foreach (var auditory in auditories)
            {
                Console.WriteLine(auditory.Name);
                try
                {
                    auditory.Schedule = Parse(jsonRepair.Repair(Download(auditory.Id, 3)));
                    auditory.lastUpdated = DateTime.Now;
                }
                catch (Exception e)
                {
                    auditory.Schedule = new List<Event>();
                    auditory.lastUpdated = DateTime.Now;
                }
            }
            context.SaveChanges();
        }
    }

    private static List<Event> Parse(string json)
    {
        List<Event> pairs = new List<Event>();
        var events = JsonConvert.DeserializeObject<dynamic>(json);

        foreach (var lesson in events.events)
        {
            Event pair = new Event();
            pair.NumberPair = int.Parse(lesson.number_pair);
            pair.StartTime = long.Parse(lesson.start_time);
            pair.EndTime = long.Parse(lesson.end_time);
            pair.Type = getType(lesson.end_time);

            var auditory = getAuditory(lesson.auditory);
            if (auditory != null)
            {
                pair.Auditory = auditory;
            }
            else
            {
                pair.Auditory = new Auditory()
                {
                    Id = 123456789,
                    Name = "Нема аудиторії"
                };
            }

            pair.Subject = findSubjectById(events.subjects, lesson.subject_id);

            if (lesson.teachers.Length == 0)
            {
                pair.Teachers = new List<Teacher>();
            }
            else
            {
                foreach (var teacher in lesson.teachers)
                {
                    pair.Teachers.Add(findTeacherById(lesson.teachers, teacher.id));
                }
            }

            if (lesson.groups.Length == 0)
            {
                pair.Groups = new List<Group>();
            }
            else
            {
                foreach (var group in lesson.groups)
                {
                    pair.Groups.Add(findGroupById(lesson.groups, group.id));
                }
            }

            pairs.Add(pair);
        }

        return pairs.OrderBy(x => x.StartTime).ToList();
    }

    public static List<Event> GetEvents(long Id, string Type, long StartTime, long EndTime)
    {
        switch (Type)
        {
            case "group":
                using (var context = new Context())
                {
                    Group group = context.Groups.ToList().Find(x => x.Id == Id);

                    return group.Schedule.Where(e => e.StartTime >= StartTime && e.StartTime <= EndTime)
                        .OrderBy(x => x.StartTime)
                        .ToList();
                }

                break;
            case "teacher":
                using (var context = new Context())
                {
                    var teacher = context.Teachers.ToList().Find(x => x.Id == Id);

                    return teacher.Schedule.Where(e => e.StartTime >= StartTime && e.StartTime <= EndTime)
                        .OrderBy(x => x.StartTime)
                        .ToList();
                }

                break;
            case "auditory":
                using (var context = new Context())
                {
                    var auditory = context.Auditories.ToList().Find(x => x.Id == Id);

                    return auditory.Schedule.Where(e => e.StartTime >= StartTime && e.StartTime <= EndTime)
                        .OrderBy(x => x.StartTime)
                        .ToList();
                }

                break;
            default:
                return new List<Event>();
                break;
        }

        return new List<Event>();
    }
}