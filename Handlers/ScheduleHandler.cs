using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using nure_api.Models;
using NureCistBot.JsonParsers;
using Serilog;

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
            if (auditory.name.ToUpper() == Name.ToUpper())
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
                    id = subject["id"].Value<int>(),
                    brief = subject["brief"].Value<string>(),
                    title = subject["title"].Value<string>()
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
                    id = teacher["id"].Value<int>(),
                    shortName = teacher["short_name"].Value<string>(),
                    fullName = teacher["full_name"].Value<string>()
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
                    id = group["id"].Value<int>(),
                    name = group["name"].Value<string>()
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
                                               $"&time_from=1693515600" +
                                               $"&time_to=1725138000" +
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
        }

        Log.Information("Init started.");

        Parallel.ForEach(teachers, new ParallelOptions { MaxDegreeOfParallelism = 10 }, teacher =>
        {
            using (var context = new Context())
            {
                Log.Information($"\nName: {teacher.shortName}" +
                                $"\nId: {teacher.id}");

                if (teacher.Schedule == "")
                {
                    var ping = new Ping();
                    var source = new Uri("https://cist.nure.ua/");
                    var isAlive = ping.Send(source.Host, 150);

                    if (isAlive.Status == IPStatus.Success)
                    {
                        try
                        {
                            var json = JsonFixers.TryFix(Download(teacher.id, 2));
                            var parsed = Parse(json);
                            teacher.Schedule = JsonConvert.SerializeObject(parsed);
                            teacher.lastUpdated = DateTime.UtcNow;
                        }
                        catch (Exception e)
                        {
                            teacher.Schedule = "[]";
                            teacher.lastUpdated = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        teacher.Schedule = "[]";
                        teacher.lastUpdated = DateTime.UtcNow;
                    }
                }

                context.SaveChanges();
            }
        });

        Parallel.ForEach(auditories, new ParallelOptions { MaxDegreeOfParallelism = 10 }, auditory =>
        {
            using (var context = new Context())
            {
                Log.Information($"\nName: {auditory.name}" +
                                $"\nId: {auditory.id}");

                if (auditory.Schedule == "")
                {
                    var ping = new Ping();
                    var source = new Uri("https://cist.nure.ua/");
                    var isAlive = ping.Send(source.Host, 150);

                    if (isAlive.Status == IPStatus.Success)
                    {
                        try
                        {
                            var json = JsonFixers.TryFix(Download(auditory.id, 2));
                            var parsed = Parse(json);
                            auditory.Schedule = JsonConvert.SerializeObject(parsed);
                            auditory.lastUpdated = DateTime.UtcNow;
                        }
                        catch (Exception e)
                        {
                            auditory.Schedule = "[]";
                            auditory.lastUpdated = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        auditory.Schedule = "[]";
                        auditory.lastUpdated = DateTime.UtcNow;
                    }
                }
                context.SaveChanges();
            }
        });


        Parallel.ForEach(groups, new ParallelOptions { MaxDegreeOfParallelism = 10 }, group =>
        {
            using (var context = new Context())
            {
                Log.Information($"\nName: {group.name}" +
                                $"\nId: {group.id}");

                if (group.Schedule == "")
                {
                    var ping = new Ping();
                    var source = new Uri("https://cist.nure.ua/");
                    var isAlive = ping.Send(source.Host, 150);

                    if (isAlive.Status == IPStatus.Success)
                    {
                        try
                        {
                            var json = JsonFixers.TryFix(Download(group.id, 1));
                            var parsed = Parse(json);
                            group.Schedule = JsonConvert.SerializeObject(parsed);
                            group.lastUpdated = DateTime.UtcNow;
                        }
                        catch (Exception e)
                        {
                            group.Schedule = "[]";
                            group.lastUpdated = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        group.Schedule = "[]";
                        group.lastUpdated = DateTime.UtcNow;
                    }
                }
                context.SaveChanges();
            }
        });
    }

    private static List<Event> Parse(string json)
    {
        List<Event> pairs = new List<Event>();
        var data = JsonConvert.DeserializeObject(json);
        var events = JObject.FromObject(data);

        Parallel.ForEach(events["events"], lesson =>
        {
            Event pair = new Event();
            pair.numberPair = lesson["number_pair"].Value<int>();
            pair.startTime = lesson["start_time"].Value<long>();
            pair.endTime = lesson["end_time"].Value<long>();
            pair.type = getType(lesson["type"].Value<int>());

            pair.auditory = lesson["auditory"].Value<string>();

            pair.subject = findSubjectById(events["subjects"], lesson["subject_id"].Value<int>());

            if (lesson["teachers"].Children().Count() == 0)
            {
                pair.teachers = new List<Teacher>();
            }
            else
            {
                Parallel.ForEach(lesson["teachers"],
                    teacher => { pair.teachers.Add(findTeacherById(events["teachers"], teacher.Value<int>())); });
            }

            if (lesson["groups"].Children().Count() == 0)
            {
            }
            else
            {
                Parallel.ForEach(lesson["groups"], group =>
                {
                    var findedGroup = findGroupById(events["groups"], group.Value<int>());
                    pair.groups.Add(findedGroup);
                });
            }

            pairs.Add(pair);
        });

        return pairs.OrderBy(x => x.startTime).ToList();
    }

    public static List<Event> GetEvents(long Id, string Type, long StartTime, long EndTime)
    {
        switch (Type)
        {
            case "group":
                using (var context = new Context())
                {
                    Group group = context.Groups.ToList().Find(x => x.id == Id);

                    var timeFromUpdate = (DateTime.UtcNow - group.lastUpdated).TotalHours;

                    if (timeFromUpdate > 30)
                    {
                        var ping = new Ping();
                        var source = new Uri("https://cist.nure.ua/");
                        var isAlive = ping.Send(source.Host, 500);

                        if (isAlive.Status == IPStatus.Success)
                        {
                            try
                            {
                                var json = JsonFixers.TryFix(Download(group.id, 1));
                                var parsed = Parse(json);
                                group.Schedule = JsonConvert.SerializeObject(parsed);
                                group.lastUpdated = DateTime.UtcNow;
                                Log.Information($"Updated: {group.name} - {timeFromUpdate}");
                                context.SaveChanges();
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                group.Schedule = "[]";
                                group.lastUpdated = DateTime.UtcNow;
                                Log.Information($"Updated: {group.name} - {timeFromUpdate}");
                                context.SaveChanges();
                            }
                        }
                    }

                    List<Event> schedule = JsonConvert.DeserializeObject<List<Event>>(group.Schedule);

                    return schedule.Where(e => e.startTime >= StartTime && e.startTime <= EndTime)
                        .OrderBy(x => x.startTime)
                        .ToList();
                }

                break;
            case "teacher":
                using (var context = new Context())
                {
                    var teacher = context.Teachers.ToList().Find(x => x.id == Id);

                    var timeFromUpdate = (DateTime.UtcNow - teacher.lastUpdated).TotalHours;

                    if (timeFromUpdate > 30)
                    {
                        var ping = new Ping();
                        var source = new Uri("https://cist.nure.ua/");
                        var isAlive = ping.Send(source.Host, 500);

                        if (isAlive.Status == IPStatus.Success)
                        {
                            try
                            {
                                var json = JsonFixers.TryFix(Download(teacher.id, 2));
                                var parsed = Parse(json);
                                teacher.Schedule = JsonConvert.SerializeObject(parsed);
                                teacher.lastUpdated = DateTime.UtcNow;
                                Log.Information($"Updated: {teacher.shortName} - {timeFromUpdate}");
                                context.SaveChanges();
                            }
                            catch (Exception e)
                            {
                                teacher.Schedule = "[]";
                                teacher.lastUpdated = DateTime.UtcNow;
                                Log.Information($"Updated: {teacher.shortName} - {timeFromUpdate}");
                                context.SaveChanges();
                            }
                        }
                    }

                    var schedule = JsonConvert.DeserializeObject<List<Event>>(teacher.Schedule);

                    return schedule.Where(e => e.startTime >= StartTime && e.startTime <= EndTime)
                        .OrderBy(x => x.startTime)
                        .ToList();
                }

                break;
            case "auditory":
                using (var context = new Context())
                {
                    var auditory = context.Auditories.ToList().Find(x => x.id == Id);

                    var timeFromUpdate = (DateTime.UtcNow - auditory.lastUpdated).TotalHours;

                    if (timeFromUpdate > 30)
                    {
                        var ping = new Ping();
                        var source = new Uri("https://cist.nure.ua/");
                        var isAlive = ping.Send(source.Host, 500);

                        if (isAlive.Status == IPStatus.Success)
                        {
                            try
                            {
                                var json = JsonFixers.TryFix(Download(auditory.id, 3));
                                var parsed = Parse(json);
                                auditory.Schedule = JsonConvert.SerializeObject(parsed);
                                auditory.lastUpdated = DateTime.UtcNow;
                                Log.Information($"Updated: {auditory.name} - {timeFromUpdate}");
                                context.SaveChanges();
                            }
                            catch (Exception e)
                            {
                                auditory.Schedule = "[]";
                                auditory.lastUpdated = DateTime.UtcNow;
                                Log.Information($"Updated: {auditory.name} - {timeFromUpdate}");
                                context.SaveChanges();
                            }
                        }
                    }

                    var schedule = JsonConvert.DeserializeObject<List<Event>>(auditory.Schedule);

                    return schedule.Where(e => e.startTime >= StartTime && e.startTime <= EndTime)
                        .OrderBy(x => x.startTime)
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