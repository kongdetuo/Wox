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
        private ConcurrentDictionary<string, Record> recordDic=null!;

        public ConcurrentDictionary<string, Record> RecordDic => recordDic ??= new ConcurrentDictionary<string, Record>(this.records);

        public bool IsTopMost(Query query, string pluginid,Result result)
        {
            return RecordDic.TryGetValue(query.RawQuery, out Record? record)
                && record.Title == result.Title.Text
                && record.SubTitle == result.SubTitle.Text
                && record.PluginID == pluginid;
        }


        public bool HasTopMost(Query query)
        {
            return query != null && RecordDic.ContainsKey(query.RawQuery);
        }

        public void Remove(Query query)
        {
            RecordDic.Remove(query.RawQuery, out _);
        }

        public void AddOrUpdate(Query query, string pluginid, Result result)
        {
            var record = new Record
            {
                PluginID = pluginid,
                Title = result.Title.Text,
                SubTitle = result.SubTitle.Text
            };
            RecordDic[query.RawQuery] = record;

        }
    }


    public class Record
    {
        public required string Title { get; set; }
        public required string SubTitle { get; set; }
        public required string PluginID { get; set; }
    }
}
