using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Wox.Plugin
{
    public interface IResult
    {
        public string Title { get; }

        public string? SubTitle { get; }

        public int Score { get; }

        public virtual List<IResult> LoadContextMenu(ActionContext context)
        {
            return new List<IResult>();
        }

        public IconLoader? IconLoader { get; }

        public Task<bool> InvokeAsync(ActionContext context);
    }

    public class ImageLoadContext
    {
        public string WoxDirectory { get; set; } = "";

        public string? PluginDirectory { get; set; }

    }

    public abstract class IconLoader
    {
        public abstract object? Load(ImageLoadContext context);
    }

    public class Result1 : IResult
    {
        public required string Title { get; init; }

        public string? SubTitle { get; init; }

        public int Score { get; init; }

        public IconLoader? IconLoader { get; init; }


        /// <summary>
        /// return true to hide wox after select result
        /// </summary>
        public Func<ActionContext, bool>? Action { get; init; }

        public Func<ActionContext, Task<bool>>? AsyncAction { get; init; }


        public async Task<bool> InvokeAsync(ActionContext context)
        {
            if (AsyncAction != null)
            {
                await AsyncAction.Invoke(context);
            }
            else if (Action != null)
            {
                return Action.Invoke(context);
            }
            return false;
        }
    }

    public class Result : IEquatable<Result>, IResult
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

        public Func<ActionContext, Task<bool>>? AsyncAction { get; set; }

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


        public async Task<bool> InvokeAsync(ActionContext context)
        {
            if (this.Action != null)
            {
                return this.Action(context);
            }
            else if (this.AsyncAction != null)
            {
                return await this.AsyncAction(context);
            }else
            {
                return false;
            }
        }

        public Result()
        { }

        /// <summary>
        /// Additional data associate with this result
        /// </summary>
        public object? ContextData { get; set; }

        string IResult.Title => this.Title.Text;

        string? IResult.SubTitle => this.SubTitle.Text;

        IconLoader icon;
        public IconLoader IconLoader
        {
            get
            {
                return null;
            }
        }
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

        public HighlightText(string text, IReadOnlyList<Range> highlightRange)
        {
            this.Text = text;
            this.HighlightRanges = highlightRange;
        }

        public string Text { get; private set; }

        public IReadOnlyList<int> HighlightData { get; private set; }

        IReadOnlyList<Range>? HighlightRanges { get; set; }
        public IEnumerable<Range> GetHighlightRanges()
        {
            if (HighlightRanges != null)
                return HighlightRanges;
            return getHighlightRanges();
        }
        private IEnumerable<Range> getHighlightRanges()
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