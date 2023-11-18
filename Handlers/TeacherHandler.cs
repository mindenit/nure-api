using System.Net;
using System.Text;
using nure_api.Models;

namespace nure_api.Handlers;

public class TeachersHandler
{
    public static List<Teacher> RemoveDuplicates(List<Teacher> list)
    {
        return list.GroupBy(x => new { x.Id })
            .Select(x => x.First())
            .ToList();
    }

    public static void Init()
    {
        using (HttpClient httpClient = new HttpClient())
        {
            var webRequest = WebRequest.Create("https://cist.nure.ua/ias/app/tt/P_API_PODR_JSON") as HttpWebRequest;

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
                
                json = json.Remove(json.Length - 2);
                

                json += "]}}";

                var teachers = RemoveDuplicates(Teacher.Parse(json));

                using (var context = new Context())
                {
                    if (context.Teachers.Any())
                    {
                        foreach (var teacher in context.Teachers)
                        {
                            context.Teachers.Remove(teacher);
                        }
                    }
                    context.Teachers.AddRange(teachers.ToArray());
                    context.SaveChanges();
                }
            }
        }
    }
    
    public static List<Teacher> Get()
    {
        List<Teacher> teachers;
        using (var context = new Context())
        {
            teachers = context.Teachers.ToList();
        }

        return teachers;
    }
}