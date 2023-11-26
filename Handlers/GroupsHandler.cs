using System.Net;
using System.Text;
using nure_api.Models;

namespace nure_api.Handlers;

public class GroupsHandler
{
    public static List<Group> RemoveDuplicates(List<Group> list)
    {
        return list.GroupBy(x => new { x.Id, x.Name })
            .Select(x => x.First())
            .ToList();
    }


    public static void Init()
    {
        /*using (HttpClient httpClient = new HttpClient())
        {*/
            var webRequest = WebRequest.Create("https://cist.nure.ua/ias/app/tt/P_API_GROUP_JSON") as HttpWebRequest;

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

                var groups = RemoveDuplicates(Group.Parse(json));

                using (var context = new Context())
                {
                    if (context.Groups.Any())
                    {
                        foreach (var group in context.Groups)
                        {
                            context.Groups.Remove(group);
                        }
                    }
                    context.Groups.AddRange(groups.ToArray());
                    context.SaveChanges();
                }
            }
        /*}*/
    }

    public static List<Group> Get()
    {
        List<Group> groups;
        using (var context = new Context())
        {
            groups = context.Groups.ToList();
        }

        return groups;
    }
}