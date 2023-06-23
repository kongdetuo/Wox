using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data.Converters;
using Avalonia.Media;
using NLog;
using Wox.Infrastructure.Logger;
using Wox.Plugin;
using Wox.ViewModel;

namespace Wox.Converters
{
    public class QuerySuggestionBoxConverter : IMultiValueConverter
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            var inlines = HighlightText.Empty;
            if (values.Count != 2 || values.All(p => p == null || p is UnsetValueType))
            {
                return inlines;
            }

            // first prop is the current query string
            var queryText = (string)values[0]!;
            var val = values[1] as ResultViewModel;

            if (string.IsNullOrEmpty(queryText) || val is null)
            {
                return inlines;
            }

            try
            {
                var selectedResult = val.Result;
                var selectedResultActionKeyword = selectedResult.ActionKeywordAssigned is null ? "" : selectedResult.ActionKeywordAssigned + " ";
                var selectedResultPossibleSuggestion = selectedResultActionKeyword + selectedResult.Title;

                if (!selectedResultPossibleSuggestion.StartsWith(queryText, StringComparison.CurrentCultureIgnoreCase))
                    return inlines;

                // When user typed lower case and result title is uppercase, we still want to display suggestion
                var textConverter = new MultilineTextConverter();
                var text = (string)textConverter.Convert(string.Concat(queryText, selectedResultPossibleSuggestion.AsSpan(queryText.Length)), targetType, parameter!, culture);

                inlines = new HighlightText(text, new[] { new Range(0, queryText.Length) });


                return inlines;
            }
            catch (Exception e)
            {
                Logger.WoxError("fail to convert text for suggestion box", e);
                return inlines;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}