using BasTools.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using Token = (string value, string tag, bool isLast);

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
                case SemanticTags.RemText: return (true, false);
                case SemanticTags.AssemblerComment: return (true, false);
                case SemanticTags.StarCommand: return (false, false);
                case SemanticTags.EmbeddedData: return (true, false);
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

            for (int counter=0; counter < lines.Lines.Count; counter++)
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

                    if (state.IsDef)
                    {
                        progline.IsDef = true;      // This line starts with DEF
                        state.IsDef = false;        // noted; cancel the condition
                        state.InDefInition = true;  // note that future lines are InDef
                    }
                    else if (state.InDefInition)
                    {
                        state.InDefInition = !isEndOfProc(lines,  counter);
                    }
                    progline.IsInDef = !progline.IsDef && state.InDefInition;

                    if (i == tokens.Count - 1)
                    {
                        // reached end of list - add and move on
                        AddToBoth(progline, token1);
                        continue;
                    }

                    // track when we're in the expression following IF
                    if (token1.tag == SemanticTags.Keyword && token1.value == "IF")        // starting an IF...        
                        state.InIfCondition = true;

                    if (token1.tag == SemanticTags.Keyword && (token1.value == "THEN" ||   // reached end of the expression
                        token1.value == "ELSE"))                    
                        state.InIfCondition = false;

                    if (token1.tag == SemanticTags.StatementSep)                    
                        state.InIfCondition = false;

                    // skip over white space to next real token
                    var token2 = tokens[i+1];
                    if (string.IsNullOrWhiteSpace(token2.value))
                        token2 = GetNextNonSpaceTag(tokens, i+1);

                    // Detect preserved whitespace before assembler comment
                    // An exception to preserve lined-up comments
                    if (string.IsNullOrWhiteSpace(token1.value))
                    {
                        if (token2.tag == SemanticTags.AssemblerComment)
                        {
                            // add both and move on
                            AddToBoth(progline, token1);
                            AddToBoth(progline, token2);

                            i++; // we consumed TWO tokens
                            continue;
                        }
                        else
                            // skip the empty token
                            continue;
                    }
                    
                    // add first token
                    AddToBoth(progline, token1);

                    // Spacing out keywords
                    if (!switches.NoSpaces)
                    {
                        bool spaceafter = BasSpacingRules.IsSpaceBetween(token1, token2);
                        // 1. Special check for unary minus
                        if (token1.tag == SemanticTags.Operator && token1.value == "-" && IsUnaryMinus(tokens, i))
                            spaceafter = false;

                        // 2. Check for implied THEN followed by indirection operator
                        //    IF <expr> <indirection>  → force a space
                        if (state.InIfCondition && token2.tag == SemanticTags.IndirectionOperator &&
                            IsEndOfIfExpression(tokens, i))
                        {
                            spaceafter = true;
                        }

                        if (spaceafter)
                        {
                            progline.FormattedPlain += ' ';
                            progline.FormattedTagged += ' ';
                        }
                    }
                }
                progline.IndentLevel = state.Indent;
                state.Indent += state.PendingIndent;
                state.PendingIndent = 0;

                /*lines.FormattedLines.Add(formattedLine);
                /////////////////////////////////////////////////////////////////////
                if (progline.TaggedLine != progline.FormattedTagged)
                {
                    Console.WriteLine($"{linenumber} {progline.TaggedLine}");
                    Console.WriteLine($"{linenumber} {progline.FormattedTagged}");
                    Console.WriteLine("");
                }*/
                
            }
            state.InIfCondition = false;
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
        private void HandleIndents(Token token1, FormatterState state, FormattingOptions switches, bool BasicV)
        {
            if (token1.tag == SemanticTags.Keyword)
            {
                if (token1.value == "THEN" && BasicV && token1.isLast)
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
            for (int j = ++i; i < lines.Lines.Count - 1; i++)
            {
                ProgramLine line = lines.Lines[j];
                string temp = line.NoSpacesLine.Replace(":", ""); // skip empty lines and just colons
                if (temp.Trim() != string.Empty)
                    return line.TaggedLine.StartsWith(SemanticTags.Keyword + "DEF");
            }
            return true; // End of program
        }
        static void AddToBoth(ProgramLine progline, Token token)
        {
            progline.FormattedPlain += token.value;
            progline.FormattedTagged += token.tag + token.value;
            if (!string.IsNullOrEmpty(token.tag)) progline.FormattedTagged += "{/}";
        }
        static Token GetNextNonSpaceTag(List<(string value, string tag, bool isLast)> tokens, int i)
        {
            for (int j = i + 1; j < tokens.Count; j++)
            {
                var (v, t, _) = tokens[j];
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
        bool IsEndOfIfExpression(List<Token> tokens, int i)
        {
            int depth = 0;

            for (int j = i + 1; j < tokens.Count; j++)
            {
                var t = tokens[j];

                // Skip whitespace
                if (string.IsNullOrWhiteSpace(t.value))
                    continue;

                // Track parentheses
                if (t.value == "(") depth++;
                if (t.value == ")") depth--;

                // If we're still inside parentheses, expression continues
                if (depth > 0)
                    return false;

                // Expression terminators
                if (t.value == "THEN") return true;
                if (t.value == "ELSE") return true;
                if (t.tag == SemanticTags.StatementSep) return true;
                if (j == tokens.Count - 1) return true;

                // Expression continuation tokens
                if (IsExpressionContinuation(t))
                    return false;

                // Otherwise: this token is not part of the expression
                return true;
            }
            return true;
        }
        bool IsExpressionContinuation(Token t)
        {
            if (t.tag == SemanticTags.Operator) return true;
            if (t.tag == SemanticTags.Variable) return true;
            if (t.tag == SemanticTags.Number) return true;
            if (t.tag == SemanticTags.HexNumber) return true;
            if (t.tag == SemanticTags.StringLiteral) return true;
            if (t.tag == SemanticTags.BuiltInFn) return true;
            if (t.tag == SemanticTags.OpenBracket) return true;
            if (t.tag == SemanticTags.CloseBracket) return true;
            if (t.tag == SemanticTags.IndirectionOperator) return true;

            return false;
        }

    }
}
