using System.Net;
using System.Text;
using nure_api.Models;

namespace nure_api.Handlers;

public class AuditoriesHandler
{
    public static List<Auditory> RemoveDuplicates(List<Auditory> list)
    {
        return list.GroupBy(x => new { x.Id, x.Name })
            .Select(x => x.First())
            .ToList();
    }
    
    public static void Init()
    {
        using (HttpClient httpClient = new HttpClient())
        {
            var webRequest = WebRequest.Create("https://cist.nure.ua/ias/app/tt/P_API_AUDITORIES_JSON") as HttpWebRequest;

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

                var auditories = RemoveDuplicates(Auditory.Parse(json));

                using (var context = new Context())
                {
                    if (context.Auditories.Any())
                    {
                        foreach (var auditory in context.Auditories)
                        {
                            context.Auditories.Remove(auditory);
                        }
                    }
                    context.Auditories.AddRange(auditories.ToArray());
                    context.SaveChanges();
                }
            }
        }
    }
    
    public static List<Auditory> Get()
    {
        List<Auditory> auditories;
        using (var context = new Context())
        {
            auditories = context.Auditories.ToList();
        }

        return auditories;
    }
}