using System;
using System.Collections.Generic;
using System.Text;

namespace BasTools.Core
{
    internal static class BasSpacingRules
    {
        internal static readonly Dictionary<SemanticTags, (bool before, bool after)> Rules =
            new()
            {
        { SemanticTags.Operator, (true, true) },
        { SemanticTags.Keyword,  (true, true) },
        { SemanticTags.Comma,    (false, true) },
        { SemanticTags.Colon,    (false, true) },
        { SemanticTags.Identifier, (false, false) },
                // etc.
            };
    }

}