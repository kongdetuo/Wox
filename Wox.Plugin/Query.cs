using System;
using System.Collections.Generic;
using System.Linq;

namespace Wox.Plugin
{
    public class Query
    {
        public Query() { }

        /// <summary>
        /// Raw query, this includes action keyword if it has
        /// We didn't recommend use this property directly. You should always use Search property.
        /// </summary>
        public string RawQuery { get;  set; }

        /// <summary>
        /// Search part of a query.
        /// This will not include action keyword if exclusive plugin gets it, otherwise it should be same as RawQuery.
        /// Since we allow user to switch a exclusive plugin to generic plugin, 
        /// so this property will always give you the "real" query part of the query
        /// </summary>
        public string Search { get;  set; }

        /// <summary>
        /// The raw query splited into a string array.
        /// </summary>
        public string[] Terms { get; set; }

        /// <summary>
        /// Query can be splited into multiple terms by whitespace
        /// </summary>
        public const string TermSeperater = " ";

        /// <summary>
        /// User can set multiple action keywords seperated by ';'
        /// </summary>
        public const string ActionKeywordSeperater = ";";

        public Keyword? ActionKeyword { get; set; }

        /// <summary>
        /// Return first search split by space if it has
        /// </summary>
        public string FirstSearch => SplitSearch(0);


        /// <summary>
        /// Return second search split by space if it has
        /// </summary>
        public string SecondSearch => SplitSearch(1);

        private string SplitSearch(int index)
        {
            try
            {
                return ActionKeyword is null ? Terms[index] : Terms[index + 1];
            }
            catch (IndexOutOfRangeException)
            {
                return string.Empty;
            }
        }

        public override string ToString() => RawQuery;

    }

    public record struct Keyword(string Key)
    {
        public static readonly Keyword GlobalPluginWildcardSign = new("*");

        public bool IsGlobal => Equals(GlobalPluginWildcardSign);

        public override string ToString()
        {
            return Key;
        }
    }
}
