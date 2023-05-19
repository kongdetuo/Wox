using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Wox.Plugin
{
    public class Result : BaseModel
    {
        public HighlightText Title { get; set; } = HighlightText.Empty;

        public HighlightText SubTitle { get; set; } = HighlightText.Empty;

        /// <summary>
        /// This holds the action keyword that triggered the result.
        /// If result is triggered by global keyword: *, this should be empty.
        /// </summary>
        public Keyword? ActionKeywordAssigned { get; set; }

        public string IcoPath { get; set; }

        /// <summary>
        /// return true to hide wox after select result
        /// </summary>
        public Func<ActionContext, bool> Action { get; set; }

        public int Score { get; set; }

        ///// <summary>
        ///// A list of indexes for the characters to be highlighted in Title
        ///// </summary>
        //public IList<int> TitleHighlightData { get => HighlightTitle.HighlightData; set => HighlightTitle.HighlightData = value; }

        ///// <summary>
        ///// A list of indexes for the characters to be highlighted in SubTitle
        ///// </summary>
        //public IList<int> SubTitleHighlightData { get => HighlightSubTitle.HighlightData; set => HighlightSubTitle.HighlightData = value; }

        /// <summary>
        /// Only results that originQuery match with current query will be displayed in the panel
        /// </summary>
        public Query OriginQuery { get; set; }

        public override bool Equals(object obj)
        {
            var r = obj as Result;
            var equality = r?.PluginID == PluginID && r?.Title == Title && r?.SubTitle == SubTitle;
            return equality;
        }

        public override int GetHashCode()
        {
            int hash1 = PluginID?.GetHashCode() ?? 0;
            int hash2 = Title?.GetHashCode() ?? 0;
            int hash3 = SubTitle?.GetHashCode() ?? 0;
            int hashcode = hash1 ^ hash2 ^ hash3;
            return hashcode;
        }

        public override string ToString()
        {
            return Title.Text + SubTitle.Text;
        }

        public Result()
        { }

        /// <summary>
        /// Additional data associate with this result
        /// </summary>
        public object ContextData { get; set; }

        /// <summary>
        /// Plugin ID that generated this result
        /// </summary>
        public string PluginID { get; set; }
    }

    public class HighlightText
    {
        private HighlightText() : this("", EmptyHighlightData)
        {

        }
        public HighlightText(string text) : this(text, EmptyHighlightData)
        {

        }
        public HighlightText(string text, List<int> highlightData)
        {
            this.Text = text;
            this.HighlightData = highlightData ?? EmptyHighlightData;
        }
        public string Text { get; private set; }

        public IReadOnlyList<int> HighlightData { get; private set; }

        public IEnumerable<Range> GetHighlightRanges()
        {
            if (HighlightData.Any())
            {
                if (HighlightData.Count == 1)
                    yield return new Range(HighlightData[0], HighlightData[0]);

                var s = 0;
                for (int i = 1; i < HighlightData.Count; i++)
                {
                    if (HighlightData[i] != HighlightData[i - 1] + 1)
                    {
                        yield return new Range(HighlightData[s], HighlightData[i - 1] + 1);
                        s = i;
                    }
                }
                if(s != HighlightData.Count - 1)
                    yield return new Range(HighlightData[s], HighlightData[HighlightData.Count - 1] + 1);
            }
        }

        private static readonly List<int> EmptyHighlightData = new(0);

        public static HighlightText Empty { get; } = new();

        public static implicit operator HighlightText(string text)
        {
            return new HighlightText(text);
        }
    }
}