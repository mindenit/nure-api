using System.Net;
using System.Text;
using nure_api.Models;

namespace nure_api.Handlers;

public class GroupsHandler
{
    public static void Init()
    {
        using (HttpClient httpClient = new HttpClient())
        {
            var webRequest = WebRequest.Create("https://cist.nure.ua/ias/app/tt/P_API_GROUP_JSON") as HttpWebRequest;

            webRequest.ContentType = "application/json";

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (var webResponse = webRequest.GetResponse())
            using (var streamReader = new StreamReader(webResponse.GetResponseStream(), Encoding.GetEncoding("windows-1251")))
            using (var memoryStream = new MemoryStream())
            using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8))
            {
                streamWriter.Write(streamReader.ReadToEnd());
                streamWriter.Flush();
                memoryStream.Position = 0;

                var json = Encoding.UTF8.GetString(memoryStream.ToArray());
            
                // Remove BOM
                json = json.TrimStart('\uFEFF');
            
                var groups = Group.Parse(json);
                using (var context = new GroupContext())
                {
                    context.Groups.AddRange(groups.ToArray());
                    context.SaveChanges();
                }
            }
        }
    }
}