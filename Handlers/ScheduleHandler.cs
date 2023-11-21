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
        string FullName = "";
        string ShortName = "";
        string Schedule = "";
        using (var context = new Context())
        {
            FullName = context.Teachers.Find(id).FullName;
            ShortName = context.Teachers.Find(id).ShortName;
            Schedule = context.Teachers.Find(id).Schedule;
        }
        foreach (var teacher in teachers)
        {
            if (teacher == id)
            {
                return new Teacher()
                {
                    Id = teacher,
                    FullName = FullName,
                    ShortName = ShortName
                };
            }
        }

        return null; // якщо клас з таким ідентифікатором не знайдено
    }

    private static Group? findGroupById(dynamic groups, int id)
    {
        foreach (var group in groups)
        {
            string Name = "";
            string Schedule = "";
            using (var context = new Context())
            {
                Name = context.Groups.Find(id).Name;
                Schedule = context.Groups.Find(id).Schedule;
            }
            if (group.ToObject<int>() == id)
            {
                return new Group()
                {
                    Id = group.ToObject<int>(),
                    Name = Name,
                    Schedule = Schedule
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

                var timeFromUpdate = (DateTime.Now - group.lastUpdated).TotalHours;

                if (group.Schedule == "[]" || group.Schedule == "" || timeFromUpdate >= 3)
                {
                    try
                    {
                        var json = jsonRepair.Repair(Download(group.Id, 1));
                        var parsed = Parse(json);
                        group.Schedule = JsonConvert.SerializeObject(parsed);
                        group.lastUpdated = DateTime.Now;
                    }
                    catch (Exception e)
                    {
                        group.Schedule = "[]";
                        group.lastUpdated = DateTime.Now;
                    }
                }
            }
            

            foreach (var teacher in teachers)
            {
                Console.WriteLine(teacher.ShortName);
                
                var timeFromUpdate = DateTime.Now - teacher.lastUpdated;

                if (teacher.Schedule == "[]" || teacher.Schedule == "" || timeFromUpdate.Hours <= 3)
                {
                    try
                    {
                        var json = jsonRepair.Repair(Download(teacher.Id, 2));
                        var parsed = Parse(json);
                        teacher.Schedule = JsonConvert.SerializeObject(parsed);
                        teacher.lastUpdated = DateTime.Now;
                    }
                    catch (Exception e)
                    {
                        teacher.Schedule = "[]";
                        teacher.lastUpdated = DateTime.Now;
                    }
                }
            }

            foreach (var auditory in auditories)
            {
                Console.WriteLine(auditory.Name);
                var timeFromUpdate = DateTime.Now - auditory.lastUpdated;

                if (auditory.Schedule == "[]" || auditory.Schedule == "" || timeFromUpdate.Hours <= 3)
                {
                    try
                    {
                        var json = jsonRepair.Repair(Download(auditory.Id, 2));
                        var parsed = Parse(json);
                        auditory.Schedule = JsonConvert.SerializeObject(parsed);
                        auditory.lastUpdated = DateTime.Now;
                    }
                    catch (Exception e)
                    {
                        auditory.Schedule = "[]";
                        auditory.lastUpdated = DateTime.Now;
                    }
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
            pair.NumberPair = lesson.number_pair.ToObject<int>();
            pair.StartTime = lesson.start_time.ToObject<long>();
            pair.EndTime = lesson.end_time.ToObject<long>();
            pair.Type = getType(lesson.type.ToObject<int>());

            var auditory = getAuditory(lesson.auditory.ToString());
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

            pair.Subject = findSubjectById(events.subjects, lesson.subject_id.ToObject<int>());

            if (lesson.teachers.Count == 0)
            {
                pair.Teachers = new List<Teacher>();
            }
            else
            {
                foreach (var teacher in lesson.teachers)
                {
                    pair.Teachers.Add(findTeacherById(lesson.teachers, teacher.ToObject<int>()));
                }
            }

            if (lesson.groups.Count == 0)
            { }
            else
            {
                foreach (var group in lesson.groups)
                {
                    var findedGroup = findGroupById(lesson.groups, group.ToObject<int>());
                    pair.Groups.Add(findedGroup);
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

                    var schedule = JsonConvert.DeserializeObject <List<Event>>(group.Schedule);

                    return schedule.Where(e => e.StartTime >= StartTime && e.StartTime <= EndTime)
                        .OrderBy(x => x.StartTime)
                        .ToList();
                }

                break;
            case "teacher":
                using (var context = new Context())
                {
                    var teacher = context.Teachers.ToList().Find(x => x.Id == Id);
                    
                    var schedule = JsonConvert.DeserializeObject <List<Event>>(teacher.Schedule);

                    return schedule.Where(e => e.StartTime >= StartTime && e.StartTime <= EndTime)
                        .OrderBy(x => x.StartTime)
                        .ToList();
                }

                break;
            case "auditory":
                using (var context = new Context())
                {
                    var auditory = context.Auditories.ToList().Find(x => x.Id == Id);
                    
                    var schedule = JsonConvert.DeserializeObject <List<Event>>(auditory.Schedule);

                    return schedule.Where(e => e.StartTime >= StartTime && e.StartTime <= EndTime)
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