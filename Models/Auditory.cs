using Newtonsoft.Json;
using System.Collections.Generic;

namespace nure_api.Models
{
    public class Auditory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        
        [JsonIgnore]
        public string Schedule { get; set; }

        [JsonIgnore]
        public DateTime lastUpdated;

        public static List<Auditory> Parse(string json)
        {
            List<Auditory> auditories = new List<Auditory>();
            dynamic cistAuditories = JsonConvert.DeserializeObject(json);

            if (cistAuditories?.university?.buildings != null)
            {
                foreach (var building in cistAuditories.university.buildings)
                {
                    if (building.auditories != null)
                    {
                        foreach (var auditory in building.auditories)
                        {
                            auditories.Add(new Auditory
                            {
                                Id = int.Parse(auditory.id.ToString()),
                                Name = auditory.short_name.ToString(),
                                Schedule = ""
                            });
                        }
                    }
                }
            }

            return auditories;
        }
    }
}