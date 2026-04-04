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
                    if (value == "PROC" || value == "FN")
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
                case SemanticTags.AssemblerComment: return (true, false);
                case SemanticTags.StarCommand: return (false, false);
                case SemanticTags.EmbeddedData: return (true, false);
                case SemanticTags.Proc: return (false, false);
                case SemanticTags.Function: return (false, false);
                case SemanticTags.Label: return (false, true);
                case SemanticTags.Register: return (true, true);
                case SemanticTags.Mnemonic: return (true, true);
                case SemanticTags.LineNumber: return (true, true);
                case SemanticTags.Operator: return (true, true);
                case SemanticTags.IndirectionOperator: return (false, false);
                case SemanticTags.ImmediateOperator: return (true, false);
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

            // 0. Keywords (and structured keywords) must NOT glue to '('
            if ((prevTag == SemanticTags.Keyword ||
                 prevTag == SemanticTags.IndentingKeyword ||
                 prevTag == SemanticTags.OutdentingKeyword ||
                 prevTag == SemanticTags.InOutKeyword) &&
                 tag == SemanticTags.OpenBracket)
                return false;
            
            // (1) Function keywords never followed by space
            if (prevTag == SemanticTags.BuiltInFn)
                return true;

            // (2a) Mnemonic → '('// Prevent gluing Mnemonic → '(' in either direction
            if ((tag == SemanticTags.OpenBracket && prevTag == SemanticTags.Mnemonic) ||
                (tag == SemanticTags.Mnemonic && prevTag == SemanticTags.OpenBracket))
                return false;

            

            // (2b) Identifier/function → '('
            if (tag == SemanticTags.OpenBracket &&
                (prevTag == SemanticTags.Variable ||
                 prevTag == SemanticTags.Proc ||
                 prevTag == SemanticTags.Function ||
                 prevTag == SemanticTags.BuiltInFn ||
                 prevTag == SemanticTags.Operator))
                return true;

            // (3) '(' → number/string/variable
            if (prevTag == SemanticTags.OpenBracket)
                return true;

            // (4) number/string/variable/closeBracket → ')'
            if (tag == SemanticTags.CloseBracket)
                return true;

            // (5) indexed addressing e.g. LDA hi,X  LDA (&70),Y
            if (prevTag == SemanticTags.ListSep && tag == SemanticTags.Register) // exceptionally do not follow listSep with space
                return true;

            // (6) list separator
            if (prevTag == SemanticTags.ListSep) // always follow listSep with space (except as above)
                return false;
            if (tag == SemanticTags.ListSep) // never precede listSep with space
                return true;

            // (7) '#' → number (file numbers) and indirection operators ? ! $
            /*if (prevTag == SemanticTags.IndirectionOperator || tag == SemanticTags.IndirectionOperator)
                return true;*/

            // Indirection operators glue only to what FOLLOWS, not what precedes
            // (7) '?' '!' '$' as indirection operators
            if (prevTag == SemanticTags.IndirectionOperator)
                return true;   // glue '$' to its operand (B%, (B%+X%), etc.)

            if (tag == SemanticTags.IndirectionOperator)
                return false;  // never glue *before* '$' — keep space before it


            // (8) e.g. LDA #&80, LDA #17
            if (prevTag == SemanticTags.ImmediateOperator && (tag == SemanticTags.HexNumber || tag == SemanticTags.Number))
                return true;

            // (9) statement separator ':' → next token
            if (tag == SemanticTags.StatementSep || prevTag == SemanticTags.StatementSep)
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
                    string prevTag = i > 0 ? tokens[i - 1].tag : null;  // previous token
                    string nextTag = GetNextNonSpaceTag(tokens, i);     // next token skipping null tags (e.g. white space)

                    // Detect preserved whitespace before assembler comment
                    bool preserveWhitespace =
                        tag == SemanticTags.AssemblerComment &&
                        i > 0 &&
                        string.IsNullOrWhiteSpace(tokens[i - 1].value);

                    // If preserving whitespace, do NOT trim or re-space
                    if (preserveWhitespace)
                    {
                        // Emit the previous whitespace exactly as-is
                        progline.FormattedPlain += tokens[i - 1].value;
                        progline.FormattedTagged += tokens[i - 1].value;

                        // Then emit the comment normally
                        progline.FormattedPlain += value;
                        progline.FormattedTagged += tag + value + "{/}";

                        // Skip the whitespace token next iteration
                        continue;
                    }


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
                    bool spacebefore = false; // dummy values
                    bool spaceafter = false;
                    value = value.Trim();
                    if (!switches.NoSpaces)
                    {
                        (spacebefore, spaceafter) = BasSpacingRules.GetSpacingRule(tag, value);

                        spacebefore = (spacebefore && i > 0 && !BasSpacingRules.NoSpaceBetween(prevTag, tag));
                        // value = " " + value;

                        spaceafter = (spaceafter && !isLast && !nextTokenStartsWithSpace(tokens[i + 1]) && !BasSpacingRules.NoSpaceBetween(tag, nextTag));
                             //value = value += " ";

                        /*if (tag == SemanticTags.Keyword)
                    {
                        if (value != "PROC" && value != "FN" && !value.EndsWith('(') && !(value == "TO" && nxtchar == 'P'))
                            if (!nextStartsWithSpace && (nxtchar is not ':' and not '(' and not ')'))

                    }*/
                    }

                    string temp = spacebefore ? " " : string.Empty;
                    temp += tag + value;
                    if (tag != null)
                        temp += "{/}";
                    if (spaceafter) temp += " ";

                    progline.FormattedPlain += value;
                    progline.FormattedTagged += temp;
                }
                progline.IndentLevel = state.Indent;
                state.Indent += state.PendingIndent;
                state.PendingIndent = 0;

                //lines.FormattedLines.Add(formattedLine);
                /////////////////////////////////////////////////////////////////////
                if (progline.TaggedLine != progline.FormattedTagged)
                {
                    Console.WriteLine($"{linenumber} {progline.TaggedLine}");
                    Console.WriteLine($"{linenumber} {progline.FormattedTagged}");
                    Console.WriteLine("");
                }
                
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
        static string GetNextNonSpaceTag(List<(string value, string tag, bool isLast)> tokens, int i)
        {
            for (int j = i + 1; j < tokens.Count; j++)
            {
                var (v, t, _) = tokens[j];
                if (!string.IsNullOrWhiteSpace(v))
                    return t;
            }
            return null;
        }
        private bool nextTokenStartsWithSpace((string value, string tag, bool isLast) token)
        {
            var (value, tag, _) = token; // not using isLast

            (bool spacebefore, bool spaceafter) = BasSpacingRules.GetSpacingRule(tag, value);

            return spacebefore || value.StartsWith(' ');
        }
    }
}
