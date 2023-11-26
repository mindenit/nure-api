using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

    private static Subject? findSubjectById(JToken subjects, int? id)
    {
        foreach (var subject in subjects)
        {
            if (subject["id"].Value<int>() == id)
            {
                return new Subject()
                {
                    Id = subject["id"].Value<int>(),
                    Brief = subject["brief"].Value<string>(),
                    Title = subject["title"].Value<string>()
                };
            }
        }

        return null; // якщо клас з таким ідентифікатором не знайдено
    }

    private static Teacher? findTeacherById(JToken teachers, int id)
    {
        foreach (var teacher in teachers)
        {
            if (teacher["id"].Value<int>() == id)
            {
                return new Teacher()
                {
                    Id = teacher["id"].Value<int>(),
                    ShortName = teacher["short_name"].Value<string>(),
                    FullName = teacher["full_name"].Value<string>()
                };
            }
        }

        return null; // якщо клас з таким ідентифікатором не знайдено
    }

    private static Group? findGroupById(JToken groups, int id)
    {
        foreach (var group in groups)
        {
            if (group["id"].Value<int>() == id)
            {
                return new Group()
                {
                    Id = group["id"].Value<int>(),
                    Name = group["name"].Value<string>()
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

        using (var context = new Context())
        {
            groups = context.Groups.ToList();
            auditories = context.Auditories.ToList();
            teachers = context.Teachers.ToList();

            foreach (var group in groups)
            {
                var timeFromUpdate = (DateTime.UtcNow - group.lastUpdated).TotalHours;

                Console.WriteLine($"{group.Name} - {timeFromUpdate}");

                if (group.Schedule == "" || timeFromUpdate > 3)
                {
                    try
                    {
                        var json = JsonFixers.TryFix(Download(group.Id, 1));
                        var parsed = Parse(json);
                        group.Schedule = JsonConvert.SerializeObject(parsed);
                        group.lastUpdated = DateTime.UtcNow;
                        context.SaveChangesAsync();
                    }
                    catch (Exception e)
                    {
                        group.Schedule = "[]";
                        group.lastUpdated = DateTime.UtcNow;
                        context.SaveChangesAsync();
                    }
                }
            }


            foreach (var teacher in teachers)
            {
                var timeFromUpdate = (DateTime.UtcNow - teacher.lastUpdated).TotalHours;

                Console.WriteLine($"{teacher.ShortName} - {timeFromUpdate}");

                if (teacher.Schedule == "" || timeFromUpdate > 3)
                {
                    try
                    {
                        var json = JsonFixers.TryFix(Download(teacher.Id, 2));
                        var parsed = Parse(json);
                        teacher.Schedule = JsonConvert.SerializeObject(parsed);
                        teacher.lastUpdated = DateTime.UtcNow;
                        context.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        teacher.Schedule = "[]";
                        teacher.lastUpdated = DateTime.UtcNow;
                        context.SaveChanges();
                    }
                }
            }

            foreach (var auditory in auditories)
            {
                var timeFromUpdate = (DateTime.UtcNow - auditory.lastUpdated).TotalHours;

                Console.WriteLine($"{auditory.Name} - {timeFromUpdate}");

                if (auditory.Schedule == "" || timeFromUpdate > 3)
                {
                    try
                    {
                        var json = JsonFixers.TryFix(Download(auditory.Id, 2));
                        var parsed = Parse(json);
                        auditory.Schedule = JsonConvert.SerializeObject(parsed);
                        auditory.lastUpdated = DateTime.UtcNow;
                        context.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        auditory.Schedule = "[]";
                        auditory.lastUpdated = DateTime.UtcNow;
                        context.SaveChanges();
                    }
                }
            }
        }
    }

    private static List<Event> Parse(string json)
    {
        List<Event> pairs = new List<Event>();
        var data = JsonConvert.DeserializeObject(json);
        var events = JObject.FromObject(data);

        Parallel.ForEach(events["events"], lesson =>
        {
            Event pair = new Event();
            pair.NumberPair = lesson["number_pair"].Value<int>();
            pair.StartTime = lesson["start_time"].Value<long>();
            pair.EndTime = lesson["end_time"].Value<long>();
            pair.Type = getType(lesson["type"].Value<int>());

            pair.Auditory = lesson["auditory"].Value<string>();

            pair.Subject = findSubjectById(events["subjects"], lesson["subject_id"].Value<int>());

            if (lesson["teachers"].Children().Count() == 0)
            {
                pair.Teachers = new List<Teacher>();
            }
            else
            {
                Parallel.ForEach(lesson["teachers"], teacher =>
                {
                    pair.Teachers.Add(findTeacherById(events["teachers"], teacher.Value<int>()));
                });
            }

            if (lesson["groups"].Children().Count() == 0)
            {
            }
            else
            {
                Parallel.ForEach(lesson["groups"], group =>
                {
                    var findedGroup = findGroupById(events["groups"], group.Value<int>());
                    pair.Groups.Add(findedGroup);
                });
            }

            pairs.Add(pair);
        });
        
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
                    
                    var timeFromUpdate = (DateTime.UtcNow - group.lastUpdated).TotalHours;

                    if (group.Schedule == "" || timeFromUpdate > 5)
                    {
                        try
                        {
                            var json = JsonFixers.TryFix(Download(group.Id, 1));
                            var parsed = Parse(json);
                            group.Schedule = JsonConvert.SerializeObject(parsed);
                            group.lastUpdated = DateTime.UtcNow;
                            context.SaveChangesAsync();
                        }
                        catch (Exception e)
                        {
                            group.Schedule = "[]";
                            group.lastUpdated = DateTime.UtcNow;
                            context.SaveChangesAsync();
                        }
                    }

                    List<Event> schedule = JsonConvert.DeserializeObject<List<Event>>(group.Schedule);
                    
                    return schedule.Where(e => e.StartTime >= StartTime && e.StartTime <= EndTime)
                        .OrderBy(x => x.StartTime)
                        .ToList();
                }

                break;
            case "teacher":
                using (var context = new Context())
                {
                    var teacher = context.Teachers.ToList().Find(x => x.Id == Id);
                    
                    var timeFromUpdate = (DateTime.UtcNow - teacher.lastUpdated).TotalHours;

                    if (teacher.Schedule == "" || timeFromUpdate > 5)
                    {
                        try
                        {
                            var json = JsonFixers.TryFix(Download(teacher.Id, 2));
                            var parsed = Parse(json);
                            teacher.Schedule = JsonConvert.SerializeObject(parsed);
                            teacher.lastUpdated = DateTime.UtcNow;
                            context.SaveChangesAsync();
                        }
                        catch (Exception e)
                        {
                            teacher.Schedule = "[]";
                            teacher.lastUpdated = DateTime.UtcNow;
                            context.SaveChangesAsync();
                        }
                    }

                    var schedule = JsonConvert.DeserializeObject<List<Event>>(teacher.Schedule);

                    return schedule.Where(e => e.StartTime >= StartTime && e.StartTime <= EndTime)
                        .OrderBy(x => x.StartTime)
                        .ToList();
                }

                break;
            case "auditory":
                using (var context = new Context())
                {
                    var auditory = context.Auditories.ToList().Find(x => x.Id == Id);

                    var timeFromUpdate = (DateTime.UtcNow - auditory.lastUpdated).TotalHours;

                    Console.WriteLine($"{auditory.Name} - {timeFromUpdate}");

                    if (auditory.Schedule == "" || timeFromUpdate > 5)
                    {
                        try
                        {
                            var json = JsonFixers.TryFix(Download(auditory.Id, 3));
                            var parsed = Parse(json);
                            auditory.Schedule = JsonConvert.SerializeObject(parsed);
                            auditory.lastUpdated = DateTime.UtcNow;
                            context.SaveChangesAsync();
                        }
                        catch (Exception e)
                        {
                            auditory.Schedule = "[]";
                            auditory.lastUpdated = DateTime.UtcNow;
                            context.SaveChangesAsync();
                        }
                    }
                    
                    var schedule = JsonConvert.DeserializeObject<List<Event>>(auditory.Schedule);

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