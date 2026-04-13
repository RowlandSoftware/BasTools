using BasTools.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;

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
                case SemanticTags.HexNumber: return (true, true);
                case SemanticTags.BinaryNumber: return (true, false);
                case SemanticTags.Variable: return (true, true);
                case SemanticTags.StaticInteger: return (true, true);
                case SemanticTags.RemText: return (false, false);
                case SemanticTags.AssemblerComment: return (true, false);
                case SemanticTags.StarCommand: return (false, false);
                case SemanticTags.EmbeddedData: return (false, false);
                case SemanticTags.ProcName: return (false, true);
                case SemanticTags.FunctionName: return (false, true);
                case SemanticTags.Label: return (false, true);
                case SemanticTags.Register: return (false, true);
                case SemanticTags.Mnemonic: return (true, true);
                case SemanticTags.LineNumber: return (true, true);
                case SemanticTags.Operator: return (true, true);
                case SemanticTags.IndirectionOperator: return (false, false);
                case SemanticTags.ImmediateOperator: return (true, false);
                case SemanticTags.StatementSep: return (false, false);
                case SemanticTags.ListSep: return (false, true);
                case SemanticTags.OpenBracket: return (false, false);
                case SemanticTags.CloseBracket: return (false, true);
                case SemanticTags.Reset: return (false, false);
                default: return (false, false);
            }
        }
        internal static bool IsSpaceBetween(Token token1, Token token2)
        {
            // special rule for null Statement separators
            if (token1.tag == SemanticTags.StatementSep && token1.value == "") return true;

            (bool dummy, bool spaceafter) = GetSpacingRule(token1.tag, token1.value);
            (bool spacebefore, dummy)     = GetSpacingRule(token2.tag, token2.value);

            if (spacebefore == spaceafter)
            {
                // they agree!
                return spaceafter;
            }
            // and now the exception rules
            switch (token1.tag)
            {
                case SemanticTags.Keyword:
                case SemanticTags.OutdentingKeyword:
                case SemanticTags.IndentingKeyword:
                case SemanticTags.InOutKeyword:
                    return (token2.tag is SemanticTags.Keyword or SemanticTags.OutdentingKeyword or
                        SemanticTags.InOutKeyword or SemanticTags.IndentingKeyword or
                        SemanticTags.BuiltInFn or SemanticTags.OpenBracket or SemanticTags.IndirectionOperator);
                
                case SemanticTags.Operator:                   
                    return (token2.tag is SemanticTags.OpenBracket or SemanticTags.IndirectionOperator);
                
                case SemanticTags.CloseBracket:
                    return (token2.tag is not SemanticTags.CloseBracket and not SemanticTags.ListSep and not SemanticTags.StatementSep);
                
                case SemanticTags.Mnemonic:
                    return true;
                
                case SemanticTags.ListSep:
                    return true;
            }
            return false;
        }
    }
    public partial class BasToolsEngine
    {
        internal bool FormatProgram(Listing lines, FormattingOptions switches, bool BasicV)
        {
            FormatterState state = new();      // this sets initial conditions

            formatLines(lines, switches, state, BasicV);

            state.InIfCondition = false;
            return true;
        }
        internal void formatLines(Listing lines, FormattingOptions switches, FormatterState state, bool BasicV)
        {
            for (int counter = 0; counter < lines.Lines.Count; counter++)
            {
                ProgramLine progline = lines.Lines[counter];

                string linenumber = formatLineNumber(progline.LineNumber, switches, state);
                progline.FormattedLineNumber = linenumber;

                var tokens = BasToolsEngine.WalkTagged(progline.TaggedLine).ToList();

                state.InIfCondition = false; // start of line, no IF's

                for (int i = 0; i < tokens.Count; i++)
                {
                    var token1 = tokens[i];

                    // Indenting
                    HandleIndents(token1, state, switches, BasicV);

                    // Reached end of token list?
                    if (i == tokens.Count - 1)
                    {
                        // Add and move on
                        AddToBoth(progline, token1);
                        continue;
                    }

                    // track when we're in the expression following IF
                    // so we can suppress outdent in expressions like IF x THEN NEXT
                    if (token1.tag == SemanticTags.Keyword && token1.value == "IF") // starting an IF...        
                    {
                        state.InIfCondition = true;
                        //state.IfConditionStartIndex = i + 1;
                    }

                    if (token1.tag == SemanticTags.Keyword &&                       // reached end of the expression
                        (token1.value == "THEN" || token1.value == "ELSE"))
                        state.InIfCondition = false;

                    if (token1.tag == SemanticTags.StatementSep)
                        state.InIfCondition = false;

                    // skip over white space to next real token
                    var token2 = GetNextNonSpaceTag(tokens, i + 1);

                    // Detect preserved whitespace before assembler comment
                    // An exception to preserve lined-up comments
                    if (string.IsNullOrWhiteSpace(token1.value) && token1.tag == null)
                    {
                        if (token2.tag == SemanticTags.AssemblerComment)
                        {
                            // add both and move on
                            AddToBoth(progline, token1);
                            AddToBoth(progline, token2);

                            i++; // we consumed TWO tokens
                        }
                        // skip the empty token
                        continue;
                    }

                    // add first token
                    AddToBoth(progline, token1);

                    // Spacing out keywords
                    bool spaceafter = BasSpacingRules.IsSpaceBetween(token1, token2);
                    //Console.WriteLine($"{token1.tag}[{token1.value}] {token2.tag}[{token2.value}] - {spaceafter}");
                    // Special check for unary minus
                    if (token1.tag == SemanticTags.Operator && token1.value == "-" && IsUnaryMinus(tokens, i))
                        spaceafter = false;

                    if (spaceafter)
                    {
                        progline.FormattedPlain += ' ';
                        progline.FormattedTagged += ' ';
                    }
                } // NEXT i

                progline.IndentLevel = state.Indent;
                state.Indent += state.PendingIndent;
                state.PendingIndent = 0;

                // Track whether in DEF or not
                if (state.IsDef)
                {
                    progline.IsDef = true;      // This line starts with DEF
                    state.IsDef = false;        // noted; cancel the condition
                    state.InDefInition = true;  // note that future lines are InDef
                }
                else if (state.InDefInition)
                {
                    state.InDefInition = !isEndOfProc(lines, counter);
                }
                progline.IsInDef = !progline.IsDef && state.InDefInition; // update progline flags from state
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
        }
        private void HandleIndents(Token token1, FormatterState state, FormattingOptions switches, bool BasicV)
        {
            if (token1.tag == SemanticTags.Keyword)
            {
                if (token1.value == "THEN" && BasicV && token1.isLast) // TODO ... THEN: is legal
                {
                    state.fMultiLineIf = true;
                    state.PendingIndent++;
                }
                else if (state.fMultiLineIf && token1.value == "ELSE")
                {
                    state.PendingIndent++;
                    state.Indent--;
                }
                else if (state.fMultiLineIf && token1.value == "ENDIF")
                {
                    state.Indent--;
                    state.fMultiLineIf = false;
                }
                if (token1.value == "DEF")
                {
                    state.IsDef = true;
                }
            }
            else
            {
                if (token1.tag == SemanticTags.IndentingKeyword)
                {
                    state.PendingIndent++;
                    if (token1.value == "CASE")
                        state.SeenFirstWhen = false;
                }
                if (token1.tag == SemanticTags.OutdentingKeyword)
                {
                    // don't cancel indent in 'IF x NEXT'
                    if (!state.InIfCondition)
                        state.Indent--; // Limit to NEXT and UNTIL?

                    if (token1.value == "ENDCASE")
                    {
                        //state.Indent--;           // undo CASE indent
                        if (state.SeenFirstWhen)
                            state.Indent--;         // undo last WHEN indent
                        state.SeenFirstWhen = false;
                    }
                }
                if (token1.tag == SemanticTags.InOutKeyword)
                {
                    // WHEN and OTHERWISE
                    if (!state.SeenFirstWhen)
                    {
                        // First WHEN: no outdent
                        state.SeenFirstWhen = true;
                    }
                    else
                    {
                        // Subsequent WHEN: outdent this line
                        state.Indent--;
                    }
                    // All WHEN/OTHERWISE indent the next line
                    state.PendingIndent++;
                }
            }
        }
        static bool isEndOfProc(Listing lines, int i) // Line lookahead to see whether next significant line is end-of-proc
        {
            // dumb checks
            if (lines.Lines[i].TaggedLine.Trim().StartsWith(SemanticTags.Keyword + "ENDPROC" + SemanticTags.Reset))
                return true;

            if (lines.Lines[i].TaggedLine.Trim().StartsWith(SemanticTags.Keyword + "=" + SemanticTags.Reset))
                return true;

            // lookahead
            for (int j = ++i; j < lines.Lines.Count ; j++)
            {
                ProgramLine line = lines.Lines[j];

                string temp = line.NoSpacesLine.Replace(":", ""); // skip empty lines and just colons
                
                int remPos = temp.IndexOf("REM");
                int dataPos = temp.IndexOf("DATA");

                int pos = (remPos == -1) ? dataPos :
                          (dataPos == -1) ? remPos :
                          Math.Min(remPos, dataPos);

                if (pos != -1)
                    temp = temp.Substring(0, pos);

                if (temp.Trim() == string.Empty)
                    continue;

                // so we're looking at the next significant line
                if (line.TaggedLine.StartsWith(SemanticTags.Keyword + "DEF"))
                    return true;
                else
                    return false;
            }
            return true; // last line always closes the indent
        }
        static void AddToBoth(ProgramLine progline, Token token)
        {
            progline.FormattedPlain += token.value;
            progline.FormattedTagged += token.tag + token.value;
            if (!string.IsNullOrEmpty(token.tag)) progline.FormattedTagged += "{/}";
        }
        static Token GetNextNonSpaceTag(List<Token> tokens, int start)
        {
            for (int j = start; j < tokens.Count; j++)
            {
                var (t, v, _) = tokens[j];
                if (t != null)
                    return tokens[j];
                if (!string.IsNullOrWhiteSpace(v))
                    return tokens[j];
            }
            return new Token(null,null, false);
        }
        static bool IsUnaryMinus(List<Token> tokens, int i)
        {
            // token[i] is "-"
            // find previous meaningful token
            Token prev = new Token(null, null, false);
            for (int j = i - 1; j >= 0; j--)
            {
                if (!string.IsNullOrWhiteSpace(tokens[j].value))
                {
                    prev = tokens[j];
                    break;
                }
            }

            // Start of line → unary
            if (prev.tag == null && prev.value == null)
                return true;

            // Unary after these tokens:
            if (prev.tag == SemanticTags.OpenBracket ||
                prev.tag == SemanticTags.ListSep ||
                prev.tag == SemanticTags.StatementSep ||
                prev.tag == SemanticTags.Operator ||
                prev.tag == SemanticTags.Keyword ||
                prev.tag == SemanticTags.IndentingKeyword ||
                prev.tag == SemanticTags.OutdentingKeyword ||
                prev.tag == SemanticTags.InOutKeyword ||
                prev.tag == SemanticTags.BuiltInFn)
            {
                return true;
            }
            // Otherwise binary
            return false;
        }
        bool IsEndOfIfExpression(List<Token> tokens, int start, int current)
        {
            int depth = 0;
            bool complete = false;

            // We scan up to the token BEFORE the current one
            for (int j = start; j < current; j++)
            {
                var t = tokens[j];
                //Console.WriteLine("--" + t.tag + " " + t.value);

                if (string.IsNullOrWhiteSpace(t.value))
                    continue;
                
                // Parentheses tracking
                if (t.tag == SemanticTags.OpenBracket)
                {
                    depth++;
                    complete = false;
                    continue;
                }

                if (t.tag == SemanticTags.CloseBracket)
                {
                    depth--;
                    complete = true;   // closing a bracket completes a subexpression
                    continue;
                }

                // If inside parentheses, expression cannot be complete
                if (depth > 0)
                {
                    complete = false;
                    continue;
                }

                // Continuation tokens reset completeness
                if (IsExpressionContinuation(t))
                {
                    complete = false;
                    continue;
                }

                // Otherwise this token is a value → potentially complete
                complete = true;
            }
            //Console.WriteLine("*** " + complete.ToString());

            // Expression is complete only if depth == 0 AND last token was complete
            return (depth == 0 && complete);
        }
        bool IsExpressionContinuation(Token t)
        {
            if (t.tag == SemanticTags.Operator) return true;
            if (t.tag == SemanticTags.BuiltInFn) return true;
            if (t.tag == SemanticTags.OpenBracket) return true;
            if (t.tag == SemanticTags.IndirectionOperator) return true;
            if (t.value == "AND" || t.value == "OR" || t.value == "NOT") return true;

            return false;
        }
    }
}
