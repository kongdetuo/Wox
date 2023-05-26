using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Wox.Plugin
{
    public class Result : IEquatable<Result>
    {
        public HighlightText Title { get; set; } = HighlightText.Empty;

        public HighlightText SubTitle { get; set; } = HighlightText.Empty;

        /// <summary>
        /// This holds the action keyword that triggered the result.
        /// If result is triggered by global keyword: *, this should be empty.
        /// </summary>
        public Keyword? ActionKeywordAssigned { get; set; }

        public string? IcoPath { get; set; }

        /// <summary>
        /// return true to hide wox after select result
        /// </summary>
        public Func<ActionContext, bool>? Action { get; set; }

        public Func<ActionContext, ValueTask<bool>>? AsyncAction { get; set; }

        public int Score { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is Result && Equals((Result)obj);

        }

        public override int GetHashCode()
        {
            int hash2 = Title?.Text?.GetHashCode() ?? 0;
            int hash3 = SubTitle?.Text?.GetHashCode() ?? 0;
            int hashcode = hash2 ^ hash3;
            return hashcode;
        }

        public override string ToString()
        {
            return Title.Text + SubTitle.Text;
        }

        public bool Equals(Result? other)
        {
            var equality = other is not null
                && other.Title.Text == Title.Text
                && other.SubTitle.Text == SubTitle.Text;
            return equality;
        }

        public Result()
        { }

        /// <summary>
        /// Additional data associate with this result
        /// </summary>
        public object? ContextData { get; set; }
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

                var range = new Range(HighlightData[0], HighlightData[0] + 1);
                for (int i = 1; i < HighlightData.Count; i++)
                {
                    if (HighlightData[i] != HighlightData[i - 1] + 1)
                    {
                        yield return range;
                        range = new Range(HighlightData[i], HighlightData[i] + 1);
                    }
                    else
                    {
                        range = new Range(range.Start, HighlightData[i] + 1);
                    }
                }
                if (range.End.Value > range.Start.Value)
                    yield return range;
            }
        }

        private static readonly List<int> EmptyHighlightData = new(0);

        public static HighlightText Empty { get; } = new();

        public static implicit operator HighlightText(string text)
        {
            return new HighlightText(text);
        }

        public override string ToString() => this.Text;
    }
}