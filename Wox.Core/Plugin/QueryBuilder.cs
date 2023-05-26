using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using Wox.Plugin;

namespace Wox.Core.Plugin
{
    public static class QueryBuilder
    {
        public static Query Build(string text)
        {
            // replace multiple white spaces with one white space
            var terms = text.Split(new[] { Query.TermSeperater }, StringSplitOptions.RemoveEmptyEntries);
            if (terms.Length == 0)
            { // nothing was typed
                return Query.Empty;
            }

            var rawQuery = string.Join(Query.TermSeperater, terms);
            string actionKeyword, search;
            var possibleActionKeyword = new Keyword(terms[0]);

            if ((terms.Length > 1 || text.EndsWith(Query.TermSeperater))
                && PluginManager.AllPlugins.Where(p => !p.Metadata.Disabled).Any(p => p.MatchKeyWord(terms[0])))
            { // use non global plugin for query
                actionKeyword = possibleActionKeyword.Key;

                search = terms.Skip(1).Any()
                    ? rawQuery.Substring(actionKeyword.Length + 1)
                    : string.Empty;
            }
            else
            { // non action keyword
                actionKeyword = string.Empty;
                search = rawQuery;
            }

            var query = new Query
            {
                Terms = terms,
                RawQuery = rawQuery,
                ActionKeyword = string.IsNullOrEmpty(actionKeyword) ? null : new Keyword(actionKeyword),
                Search = search,
            };

            return query;
        }
    }
}