﻿using System.IO;
using System.Windows.Media;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Wox.Plugin.WebSearch
{
    public class SearchSource : BaseModel
    {
        public const string DefaultIcon = "web_search.png";
        public string Title { get; set; }
        public string ActionKeyword { get; set; }

        [NotNull]
        public string Icon { get; set; } = DefaultIcon;

        /// <summary>
        /// All icon should be put under Images directory
        /// </summary>
        [NotNull]
        [JsonIgnore]
        internal string IconPath => Path.Combine(Main.ImagesDirectory, Icon);

        public string Url { get; set; }
        public bool Enabled { get; set; }

        public SearchSource DeepCopy()
        {
            var webSearch = new SearchSource
            {
                Title = Title,
                ActionKeyword = ActionKeyword,
                Url = Url,
                Icon = Icon,
                Enabled = Enabled
            };
            return webSearch;
        }
    }
}