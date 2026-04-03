using System;
using System.Collections.Generic;
using System.Text;

namespace BasTools.Core
{
    internal static class BasSpacingRules
    {
        internal static (bool before, bool after) GetSpacingRule(string tag, string value)
        {
            switch (tag)
            {
                case SemanticTags.Keyword:
                    if (value == "PROC" || value == "FN" || value == "INKEY")
                        return (true, false);
                    else return (true, true);
                case SemanticTags.IndentingKeyword:
                case SemanticTags.OutdentingKeyword:
                case SemanticTags.InOutKeyword: return (true, true);
                case SemanticTags.BuiltInFn: return (true, false);
                case SemanticTags.StringLiteral: return (true, true);
                case SemanticTags.Number: return (true, true);
                case SemanticTags.HexNumber: return (true, false);
                case SemanticTags.BinaryNumber: return (true, false);
                case SemanticTags.Variable: return (true, true);
                case SemanticTags.StaticInteger: return (true, true);
                case SemanticTags.RemText: return (true, false);
                case SemanticTags.AssemblerComment: return (false, false);
                case SemanticTags.StarCommand: return (false, false);
                case SemanticTags.EmbeddedData: return (false, false);
                case SemanticTags.Proc: return (false, false);
                case SemanticTags.Function: return (false, false);
                case SemanticTags.Label: return (false, false);
                case SemanticTags.Register: return (false, true);
                case SemanticTags.Mnemonic: return (true, true);
                case SemanticTags.LineNumber: return (true, true);
                case SemanticTags.Operator: return (true, true);
                case SemanticTags.StatementSep: return (false, false);
                case SemanticTags.ListSep: return (false, true);
                case SemanticTags.OpenBracket: return (false, false);
                case SemanticTags.CloseBracket: return (false, false);
                case SemanticTags.Reset: return (false, false);
                default: return (false, false);
            }
        }
        internal static bool NoSpaceBetween(string prevTag, string tag)
        {
            if (prevTag == null || tag == null)
                return false;

            // (1) Identifier/function → '('
            if (tag == SemanticTags.OpenBracket &&
                (prevTag == SemanticTags.Variable ||
                 prevTag == SemanticTags.Proc ||
                 prevTag == SemanticTags.Function ||
                 prevTag == SemanticTags.BuiltInFn ||
                 prevTag == SemanticTags.Operator ||
                 prevTag == SemanticTags.Keyword))
                return true;

            // (2) '(' → number/string/variable
            if (prevTag == SemanticTags.OpenBracket &&
                (tag == SemanticTags.Number ||
                 tag == SemanticTags.StringLiteral ||
                 tag == SemanticTags.Variable))
                return true;

            // (3) number/string/variable/closeBracket → ')'
            if (tag == SemanticTags.CloseBracket &&
                (prevTag == SemanticTags.Number ||
                 prevTag == SemanticTags.StringLiteral ||
                 prevTag == SemanticTags.Variable ||
                 prevTag == SemanticTags.CloseBracket))
                return true;

            // (4) number/string/variable/closeBracket → list separator
            if (tag == SemanticTags.ListSep &&
                (prevTag == SemanticTags.Number ||
                 prevTag == SemanticTags.StringLiteral ||
                 prevTag == SemanticTags.Variable ||
                 prevTag == SemanticTags.CloseBracket))
                return true;

            // (5) list separator → number/string/variable/openBracket
            if (prevTag == SemanticTags.ListSep &&
                (tag == SemanticTags.Number ||
                 tag == SemanticTags.StringLiteral ||
                 tag == SemanticTags.Variable ||
                 tag == SemanticTags.OpenBracket))
                return true;

            // (6) '#' → number   (file numbers)
            if (prevTag == SemanticTags.Operator && prevTag == "#" &&
                tag == SemanticTags.Number)
                return true;

            // (7) statement separator ':' → next token
            if (prevTag == SemanticTags.StatementSep)
                return true;

            return false;
        }
    }
    public partial class BasToolsEngine
    {
        internal bool FormatProgram(Listing lines, FormattingOptions switches, bool BasicV)
        {
            FormatterState state = new();      // this sets initial conditions

            for (int counter=0; counter < lines.Lines.Count; counter++)
            {
                ProgramLine progline = lines.Lines[counter];

                string linenumber = formatLineNumber(progline.LineNumber, switches, state);
                progline.FormattedLineNumber = linenumber;

                var tokens = BasToolsEngine.WalkTagged(progline.TaggedLine).ToList();
                for (int i = 0; i < tokens.Count; i++)
                {
                    var (value, tag, isLast) = tokens[i];
                    string prevTag = i > 0 ? tokens[i - 1].tag : null; // previous token
                    string nextTag = i < tokens.Count-1 ? tokens[i + 1].tag : null; // next token

                    // Indenting
                    if (tag == SemanticTags.Keyword)
                    {
                        if (value == "THEN" && BasicV && isLast)
                        {
                            state.fMultiLineIf = true;
                            state.PendingIndent++;
                        }
                        else if (state.fMultiLineIf && value == "ELSE")
                        {
                            state.PendingIndent++;
                            state.Indent--;
                        }
                        else if (state.fMultiLineIf && value == "ENDIF")
                        {
                            state.Indent--;
                            state.fMultiLineIf = false;
                        }
                    }
                    else
                    {
                        if (tag == SemanticTags.IndentingKeyword) state.PendingIndent++;
                        if (tag == SemanticTags.OutdentingKeyword) state.Indent--;
                        if (tag == SemanticTags.InOutKeyword && switches.BreakApart && false) // && NExt line not WHEN && not OTHERWISE && already indented
                        {
                            state.Indent--;
                            state.PendingIndent++;
                        }
                    }

                    // Spacing out keywords
                    value = value.Trim();
                    if (!switches.NoSpaces)
                    {
                        (bool spacebefore, bool spaceafter) = BasSpacingRules.GetSpacingRule(tag, value);

                        if (spacebefore && i > 0 && !BasSpacingRules.NoSpaceBetween(prevTag, tag))
                            value = " " + value;

                        if (spaceafter && !isLast && !nextTokenStartsWithSpace && !BasSpacingRules.NoSpaceBetween(tag, nextTag))
                            value += " ";

                        /*if (tag == SemanticTags.Keyword)
                    {
                        if (value != "PROC" && value != "FN" && !value.EndsWith('(') && !(value == "TO" && nxtchar == 'P'))
                            if (!nextStartsWithSpace && (nxtchar is not ':' and not '(' and not ')'))

                    }*/
                    }

                    string temp = tag + value;
                    if (tag != null)
                        temp += "{/}";

                    progline.FormattedPlain += value;
                    progline.FormattedTagged += temp;
                }
                progline.IndentLevel = state.Indent;
                state.Indent += state.PendingIndent;
                state.PendingIndent = 0;

                //lines.FormattedLines.Add(formattedLine);
            }
            return true;
        }
        static string formatLineNumber(int lineNumber, FormattingOptions switches, FormatterState State)
        {
            string linenumber = lineNumber.ToString();

            if (lineNumber == 0 && State.Z80)
            {
                linenumber = string.Empty;
                if (switches.FlgAddNums) linenumber = (State.LineCount * 10).ToString();
            }
            else if (switches.Align)
                linenumber = linenumber.PadLeft(5);

            return linenumber;
        }
        private bool nextTokenStartsWithSpace((string value, string tag, bool isLast) token)
        {
            var (value, tag, _) = token; // not using isLast

            (bool spacebefore, bool spaceafter) = BasSpacingRules.GetSpacingRule(tag, value);

            return spacebefore || value.StartsWith(' ');
        }
    }
}
