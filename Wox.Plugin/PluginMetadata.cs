using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Wox.Plugin
{
    public class PluginMetadata : BaseModel
    {
        public required string ID { get; set; }
        public required string Name { get; set; }
        public required string Author { get; set; }
        public required string Version { get; set; }
        public required string Language { get; set; }
        public required string Description { get; set; }
        public required string Website { get; set; }
        public required bool Disabled { get; set; }
        public required string ExecuteFilePath { get; set; }

        public required string PluginDirectory { get; set; }

        public required List<Keyword> ActionKeywords { get; set; }

        public required string IcoPath { get; set; }

        // keep plugin raw score by not multiply selected counts
        public bool KeepResultRawScore { get; set; }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Init time include both plugin load time and init time
        /// </summary>
        public long InitTime { get; set; }
        public long AvgQueryTime { get; set; }
        public int QueryCount { get; set; }
    }
}
