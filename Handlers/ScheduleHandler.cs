using System.Net;
using System.Text;
using Newtonsoft.Json;
using nure_api.Models;
using NureCistBot.JsonParsers;

namespace nure_api.Handlers;

public class ScheduleHandler
{
    private static string getType(int? id)
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

    private static Subject? findSubjectById(ParseSubject[] subjects, int? id)
    {
        foreach (var subject in subjects)
        {
            if (subject.id == id)
            {
                return new Subject()
                {
                    Id = subject.id,
                    Brief = subject.brief,
                    Title = subject.title
                };
            }
        }

        return null; // якщо клас з таким ідентифікатором не знайдено
    }

    private static Teacher? findTeacherById(Teacher[] teachers, int id)
    {
        foreach (var teacher in teachers)
        {
            if (teacher.Id == id)
            {
                return teacher;
            }
        }

        return null; // якщо клас з таким ідентифікатором не знайдено
    }

    private static Group? findGroupById(Group[] groups, int id)
    {
        foreach (var group in groups)
        {
            if (group.Id == id)
            {
                return group;
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

        using (var context = new Context())
        {
            groups = context.Groups.ToList();
            auditories = context.Auditories.ToList();
            teachers = context.Teachers.ToList();

            foreach (var group in groups)
            {

                var timeFromUpdate = (DateTime.Now - group.lastUpdated).TotalHours;
                
                Console.WriteLine($"{group.Name} - {timeFromUpdate}");

                if (group.Schedule == "" || timeFromUpdate > 3)
                {
                    try
                    {
                        var jsonT = JsonFixers.TryFix(Download(group.Id, 1));
                        var parsedT = Parse(jsonT);
                        group.Schedule = JsonConvert.SerializeObject(parsedT);
                        group.lastUpdated = DateTime.Now;
                        context.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        group.Schedule = "[]";
                        group.lastUpdated = DateTime.Now;
                        context.SaveChanges();
                    }
                }
            }
            

            foreach (var teacher in teachers)
            {
                var timeFromUpdate = (DateTime.Now - teacher.lastUpdated).TotalHours;
                
                Console.WriteLine($"{teacher.ShortName} - {timeFromUpdate}");

                if (teacher.Schedule == "" || timeFromUpdate > 3)
                {
                    try
                    {
                        var json = JsonFixers.TryFix(Download(teacher.Id, 2));
                        var parsed = Parse(json);
                        teacher.Schedule = JsonConvert.SerializeObject(parsed);
                        teacher.lastUpdated = DateTime.Now;
                        context.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        teacher.Schedule = "[]";
                        teacher.lastUpdated = DateTime.Now;
                        context.SaveChanges();
                    }
                }
            }

            foreach (var auditory in auditories)
            {
                var timeFromUpdate = (DateTime.Now - auditory.lastUpdated).TotalHours;
                
                Console.WriteLine($"{auditory.Name} - {timeFromUpdate}");

                if (auditory.Schedule == "" || timeFromUpdate > 3)
                {
                    try
                    {
                        var json = JsonFixers.TryFix(Download(auditory.Id, 2));
                        var parsed = Parse(json);
                        auditory.Schedule = JsonConvert.SerializeObject(parsed);
                        auditory.lastUpdated = DateTime.Now;
                        context.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        auditory.Schedule = "[]";
                        auditory.lastUpdated = DateTime.Now;
                        context.SaveChanges();
                    }
                }
            }
        }
    }

    private static List<Event> Parse(string json)
    {
        List<Event> pairs = new List<Event>();
        var events = JsonConvert.DeserializeObject<ScheduleGroup>(json);

        foreach (var lesson in events.events)
        {
            Event pair = new Event();
            pair.NumberPair = lesson.number_pair;
            pair.StartTime = lesson.start_time;
            pair.EndTime = lesson.end_time;
            pair.Type = getType(lesson.type);

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

            pair.Subject = findSubjectById(events.subjects, lesson.subject_id);

            if (lesson.teachers.Length == 0)
            {
                pair.Teachers = new List<Teacher>();
            }
            else
            {
                foreach (var teacher in lesson.teachers)
                {
                    var context = new Context();
                    pair.Teachers.Add(findTeacherById(context.Teachers.ToArray(), teacher));
                }
            }

            if (lesson.groups.Length == 0)
            { }
            else
            {
                foreach (var group in lesson.groups)
                {
                    var context = new Context();
                    var findedGroup = findGroupById(context.Groups.ToArray(), group);
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