using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Wox.Plugin
{
    public class Result : BaseModel
    {
        public HighlightText HighlightTitle { get; set; } = new HighlightText("");
        public HighlightText HighlightSubTitle { get; set; } = new HighlightText("");

        public string Title { get => HighlightTitle.Text; set => HighlightTitle.Text = value; }

        public string SubTitle { get => HighlightSubTitle.Text; set => HighlightSubTitle.Text = value; }

        /// <summary>
        /// This holds the action keyword that triggered the result.
        /// If result is triggered by global keyword: *, this should be empty.
        /// </summary>
        public Keyword? ActionKeywordAssigned { get; set; }

        public string IcoPath { get; set; }

        public delegate ImageSource IconDelegate();

        public IconDelegate Icon;

        /// <summary>
        /// return true to hide wox after select result
        /// </summary>
        public Func<ActionContext, bool> Action { get; set; }

        public int Score { get; set; }

        /// <summary>
        /// A list of indexes for the characters to be highlighted in Title
        /// </summary>
        public IList<int> TitleHighlightData { get => HighlightTitle.HighlightData; set => HighlightTitle.HighlightData = value; }

        /// <summary>
        /// A list of indexes for the characters to be highlighted in SubTitle
        /// </summary>
        public IList<int> SubTitleHighlightData { get => HighlightSubTitle.HighlightData; set => HighlightSubTitle.HighlightData = value; }

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
            return Title + SubTitle;
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
        public HighlightText(string text) : this(text, Empty)
        {

        }
        public HighlightText(string text, List<int> highlightData)
        {
            this.Text = text;
            this.HighlightData = highlightData;
        }
        public string Text { get; set; }

        public IList<int> HighlightData { get; set; }

        private static readonly List<int> Empty = new List<int>();

        public static implicit operator HighlightText(string text)
        {
            return new HighlightText(text);
        }
    }
}