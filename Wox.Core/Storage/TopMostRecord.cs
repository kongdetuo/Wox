using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using Wox.Plugin;

namespace Wox.Core.Storage
{
    public class TopMostRecord
    {
        [JsonProperty]
        private Dictionary<string, Record> records = new();
        private ConcurrentDictionary<string, Record> recordDic;

        public ConcurrentDictionary<string, Record> RecordDic => recordDic ??= new ConcurrentDictionary<string, Record>(this.records);
        public bool IsTopMost(Result result)
        {
            return RecordDic.TryGetValue(result.OriginQuery.RawQuery, out Record record)
                && record.Title == result.Title
                && record.SubTitle == result.SubTitle
                && record.PluginID == result.PluginID;
        }

        public bool HasTopMost(Query query)
        {
            return query != null && RecordDic.ContainsKey(query.RawQuery);
        }

        public void Remove(Result result)
        {
            RecordDic.Remove(result.OriginQuery.RawQuery, out _);
        }

        public void AddOrUpdate(Result result)
        {
            var record = new Record
            {
                PluginID = result.PluginID,
                Title = result.Title,
                SubTitle = result.SubTitle
            };
            RecordDic[result.OriginQuery.RawQuery] = record;

        }
    }


    public class Record
    {
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public string PluginID { get; set; }
    }
}
