using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Wox.Plugin;

namespace Wox.Core.Storage
{
    // todo this class is not thread safe.... but used from multiple threads.
    public class TopMostRecord
    {
        [JsonProperty]
        private Dictionary<string, Record> records = new Dictionary<string, Record>();

        public bool IsTopMost(Result result)
        {
            return records.Count > 0
                && records.TryGetValue(result.OriginQuery.RawQuery, out Record record)
                && record.Title == result.Title
                && record.SubTitle == result.SubTitle
                && record.PluginID == result.PluginID;
        }

        public bool HasTopMost(Query query)
        {
            return query != null && records.ContainsKey(query.RawQuery);
        }

        public void Remove(Result result)
        {
            records.Remove(result.OriginQuery.RawQuery);
        }

        public void AddOrUpdate(Result result)
        {
            var record = new Record
            {
                PluginID = result.PluginID,
                Title = result.Title,
                SubTitle = result.SubTitle
            };
            records[result.OriginQuery.RawQuery] = record;

        }

        public void Load(Dictionary<string, Record> dictionary)
        {
            records = dictionary;
        }
    }


    public class Record
    {
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public string PluginID { get; set; }
    }
}
