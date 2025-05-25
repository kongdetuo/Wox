using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Mages.Core;
using Wox.Infrastructure.Storage;
using Wox.Plugin.Caculator.ViewModels;

namespace Wox.Plugin.Caculator
{
    public class Main : IPlugin, IPluginI18n
    {
        private static readonly Regex RegValidExpressChar = new Regex(
                        @"^(" +
                        @"ceil|floor|exp|pi|e|max|min|det|abs|log|ln|sqrt|" +
                        @"sin|cos|tan|arcsin|arccos|arctan|" +
                        @"eigval|eigvec|eig|sum|polar|plot|round|sort|real|zeta|" +
                        @"bin2dec|hex2dec|oct2dec|" +
                        @"==|~=|&&|\|\||" +
                        @"[ei]|[0-9]|[\+\-\*\/\^\., ""]|[\(\)\|\!\[\]]" +
                        @")+$", RegexOptions.Compiled);
        private static readonly Regex RegBrackets = new Regex(@"[\(\)\[\]]", RegexOptions.Compiled);
        private static readonly Engine MagesEngine;
        private PluginInitContext Context { get; set; } = null!;

        private static SettingsViewModel _viewModel = null!;
        private static Settings _settings=>_viewModel.Settings;

        IconLoader IconLoader { get; set; }

        static Main()
        {
            MagesEngine = new Engine();
        }

        public void Init(PluginInitContext context)
        {
            Context = context;
            this.IconLoader = context.API.IconHelper.FromImage("Images/calculator.png");
            _viewModel = new SettingsViewModel();
        }

        public List<IResult> Query(Query query)
        {
            if (!CanCalculate(query))
            {
                return new List<IResult>();
            }

            try
            {
                var expression = query.Search.Replace(",", ".");
                var result = MagesEngine.Interpret(expression);
                if(result == null)
                {
                    return new();
                }

                if (result.ToString() == "NaN")
                    result = Context.API.GetTranslation("wox_plugin_calculator_not_a_number");

                if (result is Function)
                    result = Context.API.GetTranslation("wox_plugin_calculator_expression_not_complete");

                if (!string.IsNullOrEmpty(result?.ToString()))
                {
                    decimal roundedResult = Math.Round(Convert.ToDecimal(result), _settings.MaxDecimalPlaces, MidpointRounding.AwayFromZero);
                    string newResult = ChangeDecimalSeparator(roundedResult, GetDecimalSeparator());

                    return new List<IResult>
                    {
                        new CaculatorResult
                        {
                            Title = newResult,
                            Score = 300,
                            SubTitle = Context.API.GetTranslation("wox_plugin_calculator_copy_number_to_clipboard"),
                            IconLoader = this.IconLoader
                        }
                    };
                }
            }
            catch
            {
                // ignored
            }

            return new List<IResult>();
        }

        private bool CanCalculate(Query query)
        {
            // Don't execute when user only input "e" or "i" keyword
            if (query.Search.Length < 2)
            {
                return false;
            }

            if (!RegValidExpressChar.IsMatch(query.Search))
            {
                return false;
            }

            if (!IsBracketComplete(query.Search))
            {
                return false;
            }

            return true;
        }

        private string ChangeDecimalSeparator(decimal value, string newDecimalSeparator)
        {
            if (String.IsNullOrEmpty(newDecimalSeparator))
            {
                return value.ToString();
            }

            var numberFormatInfo = new NumberFormatInfo
            {
                NumberDecimalSeparator = newDecimalSeparator
            };
            return value.ToString(numberFormatInfo);
        }

        private string GetDecimalSeparator()
        {
            string systemDecimalSeperator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            switch (_settings.DecimalSeparator)
            {
                case DecimalSeparator.UseSystemLocale: return systemDecimalSeperator;
                case DecimalSeparator.Dot: return ".";
                case DecimalSeparator.Comma: return ",";
                default: return systemDecimalSeperator;
            }
        }

        private bool IsBracketComplete(string query)
        {
            var matchs = RegBrackets.Matches(query);
            var leftBracketCount = 0;
            foreach (Match match in matchs)
            {
                if (match.Value == "(" || match.Value == "[")
                {
                    leftBracketCount++;
                }
                else
                {
                    leftBracketCount--;
                }
            }

            return leftBracketCount == 0;
        }

        public string GetTranslatedPluginTitle()
        {
            return Context.API.GetTranslation("wox_plugin_caculator_plugin_name");
        }

        public string GetTranslatedPluginDescription()
        {
            return Context.API.GetTranslation("wox_plugin_caculator_plugin_description");
        }

        public IEnumerable<PluginOption> Options => [
            new ComboBoxOption(){
                Key = "wox_plugin_calculator_output_decimal_seperator",
                Items = ComboBoxOption.ItemsFromEnum<DecimalSeparator>(),
                Value = _settings.DecimalSeparator,
            },
            new NumberBoxOption(){
                Key = "wox_plugin_calculator_max_decimal_places",
                MinValue = 0,
                MaxValue = 20,
                Value = _settings.MaxDecimalPlaces
            }
        ];

        public void SaveOptions(IEnumerable<PluginOption> options)
        {
            _settings.DecimalSeparator = (DecimalSeparator) options.OfType<ComboBoxOption>().FirstOrDefault()!.Value;
            _settings.MaxDecimalPlaces = (int) options.OfType<NumberBoxOption>().FirstOrDefault()!.Value;
            _viewModel.Save();
        }
    }

    public class CaculatorResult : IResult
    {
        public required string Title { get; init; }

        public string? SubTitle { get; init; }

        public int Score { get; init; }

        public IconLoader? IconLoader { get; init; }

        public Task<bool> InvokeAsync(ActionContext context)
        {
            return Actions.CopyTextToClipboard(context, Title);
        }
    }
}
