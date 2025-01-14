﻿using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Wox.Plugin
{
    [JsonObject(MemberSerialization.OptOut)]
    public class PluginMetadata : BaseModel
    {
        private string _pluginDirectory;
        public string ID { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public string Language { get; set; }
        public string Description { get; set; }
        public string Website { get; set; }
        public bool Disabled { get; set; }
        public string ExecuteFilePath { get; private set;}

        public string ExecuteFileName { get; set; }

        public string PluginDirectory
        {
            get { return _pluginDirectory; }
            set
            {
                _pluginDirectory = value;
                ExecuteFilePath = Path.Combine(value, ExecuteFileName);
                IcoPath = Path.Combine(value, IcoPath);
            }
        }

        public Keyword ActionKeyword { get; set; }

        public List<Keyword> ActionKeywords { get; set; }

        public string IcoPath { get; set;}

        // keep plugin raw score by not multiply selected counts
        public bool KeepResultRawScore { get; set; }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Init time include both plugin load time and init time
        /// </summary>
        [JsonIgnore]
        public long InitTime { get; set; }
        [JsonIgnore]
        public long AvgQueryTime { get; set; }
        [JsonIgnore]
        public int QueryCount { get; set; }
    }
}
