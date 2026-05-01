using BasTools.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
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
                case SemanticTags.Array: return (true, false);
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
            (bool spacebefore, dummy) = GetSpacingRule(token2.tag, token2.value);

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
                    return (!(token2.tag == null && token2.value.Contains('['))); // this is specifically for ARM assembler like LDR R3, [R2]. 'true' -> double spaces.
            }
            return false;
        }
    }
    public partial class BasToolsEngine
    {
     #region Assembler Columns
        public sealed class AsmColumnConfig
        {
            public int LabelCol { get; init; }      // 0‑based index where label starts
            public int MnemonicCol { get; init; }   // where mnemonic starts
            public int OperandCol { get; init; }    // where operands start
            public int CommentCol { get; init; }    // where comment starts
        }

        static readonly AsmColumnConfig ArmColumns = new()
        {
            LabelCol = 10,
            MnemonicCol = 6,
            OperandCol = 15,
            CommentCol = 40,
        };

        static readonly AsmColumnConfig M6502Columns = new()
        {
            LabelCol = 10,
            MnemonicCol = 4,
            OperandCol = 10,
            CommentCol = 40,
        };
        #endregion
        internal bool FormatProgram(Listing lines, FormattingOptions switches, ProgInfo progInfo)
        {
            FormatterState state = new();      // this sets initial conditions

            formatLines(lines, switches, state, progInfo, false);

            return true;
        }
        public void formatLines(Listing lines, FormattingOptions switches, FormatterState state, ProgInfo progInfo, bool IsSplitLines)
        {
            state.InIf = false; // for the benefit of SplitLines

            for (int counter = 0; counter < lines.Lines.Count; counter++)
            {
                ProgramLine progline = lines.Lines[counter];
                state.LineCount++;

                // Capture formatter state at start of this line
                progline.fstate = new(state);

                formatLineNumber(progline, switches, state, progInfo);

                var tokens = BasToolsEngine.WalkTagged(progline.TaggedLine).ToList();
                
                if (progline.InAsm && switches.AssemblerColumns)
                {
                    //DBG($"{progline.LineNumber} {progline.TaggedLine} == {progline.InAsm} [{switches.AssemblerColumns}]");

                    if (FormatAssemblerColumnsForLine(tokens, progline, switches))
                    {
                        // safe to set this IF it's in a PROC - assembler can't really be a DEF
                        progline.IsInDef = state.InDefInition;
                        continue;
                    }
                }

                state.InIfCondition = false; // start of line, no IF's
                if (!IsSplitLines) state.InIf = false;
                state.LoopsOnThisLine = 0;   // start of line

                for (int i = 0; i < tokens.Count; i++)
                {
                    var token1 = tokens[i];

                    // Indenting
                    HandleIndents(token1, state, progInfo, IsSplitLines);

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
                        state.InIf = true;
                    }

                    if (token1.tag == SemanticTags.Keyword &&
                       (token1.value == "THEN" || token1.value == "ELSE"))
                    {
                        state.InIfCondition = false;      // reached end of the expression
                    }

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
                    state.InDefInition = !isEndOfProc(lines, counter, IsSplitLines);
                }
                progline.IsInDef = !progline.IsDef && state.InDefInition; // update progline flags from state
            }
        }
        static void formatLineNumber(ProgramLine progLine, FormattingOptions switches, FormatterState State, ProgInfo progInfo)
        {
            string formattedLineNumber = progLine.LineNumber.ToString();

            if (progLine.LineNumber == 0 && progInfo.Z80)
            {
                formattedLineNumber = string.Empty;
                if (switches.FlgAddNums)
                {
                    progLine.LineNumber = State.LineCount * 10;
                    formattedLineNumber = progLine.LineNumber.ToString();
                }
            }
            if (switches.Align && formattedLineNumber.Length > 0)
                formattedLineNumber = formattedLineNumber.PadLeft(5);

            progLine.FormattedLineNumber = formattedLineNumber;
        }        
        private void  HandleIndents(Token token1, FormatterState state, ProgInfo progInfo, bool InSplitLines)
        {
            if (token1.tag == SemanticTags.Keyword)
            {
                if (token1.value == "THEN" && ((progInfo.BasicV || progInfo.Z80) && token1.isLast)) // s.a. below
                {
                    state.MultiLineIfDepth++;
                    state.PendingIndent++;
                }
                else if (state.MultiLineIfDepth > 0 && token1.value == "ELSE")
                {
                    state.PendingIndent++;
                    state.Indent--;
                }
                else if (state.MultiLineIfDepth > 0 && token1.value == "ENDIF")
                {
                    state.Indent--;
                    state.MultiLineIfDepth--;
                }
                if (token1.value == "DEF")
                {
                    state.IsDef = true;
                }
            }
            else if (InSplitLines && (token1.tag == SemanticTags.StatementSep     // s.a. above
                && token1.value == "") || (token1.tag == SemanticTags.Keyword && token1.value == "THEN"))
            {
                state.MultiLineIfDepth++;
                state.PendingIndent++;
            }
            else
            {
                if (token1.tag == SemanticTags.IndentingKeyword) // FOR REPEAT WHILE CASE
                {
                    state.PendingIndent++;
                    state.LoopsOnThisLine++;
                    if (state.InIf)
                        state.LoopInIf = true;
                    if (token1.value == "CASE")
                        state.SeenFirstWhen = false;
                }

                if (token1.tag == SemanticTags.OutdentingKeyword) // NEXT UNTIL ENDWHILE ENDCASE
                {
                    // don't cancel indent in e.g. 'IF x NEXT'
                    if (!state.InIf || state.LoopInIf)
                    {
                        state.LoopInIf = false;
                        if (state.LoopsOnThisLine > 0) // handle loops on one line different from separate lines
                        {
                            state.PendingIndent--;
                            state.LoopsOnThisLine--;
                        }
                        else
                        {
                            state.Indent--;
                        }
                    }

                    if (token1.value == "ENDCASE")
                    {
                        //state.Indent--;           // undo CASE indent (done already)
                        if (state.SeenFirstWhen)
                            state.Indent--;         // undo last WHEN indent
                        state.SeenFirstWhen = false;
                    }
                }
                if (token1.tag == SemanticTags.InOutKeyword) // WHEN and OTHERWISE
                {
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
        static bool isEndOfProc(Listing lines, int i, bool IsSplitLines) // Line lookahead to see whether next significant line is end-of-proc
        {
            if (IsSplitLines) return false;

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
        // True if line has been formatted for assembler
        private static bool FormatAssemblerColumnsForLine(List<Token> tokens, ProgramLine progline, FormattingOptions switches)
        {
            if (!switches.AssemblerColumns || progline.TaggedLine.StartsWith('['))
                return false;
            //DBG("Hello");
            var cols = progline.IsArm ? ArmColumns : M6502Columns;

            // Final output builders
            var sbTagged = new StringBuilder();
            var sbPlain = new StringBuilder();

            // Current statement fields
            string label = "";
            string mnemonic = "";
            string comment = "";
            var plainOperandParts = new List<string>();
            var taggedOperandParts = new List<string>();

            bool seenMnemonic = false;
            bool seenComment = false;
            bool first = true;

            // Helper: flush one statement into sbTagged/sbPlain
            void FlushStatement(bool first)
            {
                if (label == "" && mnemonic == "" && plainOperandParts.Count == 0 && comment == "")
                    return; // nothing to flush

                // Build padded label
                if (first || label.Length > 0)
                {
                    sbTagged.Append(padOut(SemanticTags.Label, ref label, cols.LabelCol + switches.ExtraColumnWidth));
                    sbPlain.Append(label);
                }

                // Build padded mnemonic
                sbTagged.Append(padOut(SemanticTags.Mnemonic, ref mnemonic, cols.MnemonicCol));
                sbPlain.Append(mnemonic);

                // Build padded operands
                string plainOperands = string.Concat(plainOperandParts);
                string taggedOperands = string.Concat(taggedOperandParts);

                int padlen = cols.OperandCol + switches.ExtraColumnWidth - plainOperands.Length;
                if (padlen < 1) padlen = 1;

                plainOperands += new string(' ', padlen);
                taggedOperands += new string(' ', padlen);

                sbTagged.Append(taggedOperands);
                sbPlain.Append(plainOperands);

                // Comment (no padding needed) ??
                if (!string.IsNullOrWhiteSpace(comment))
                {
                    int targetCol = cols.LabelCol + switches.ExtraColumnWidth + cols.MnemonicCol + cols.OperandCol + switches.ExtraColumnWidth;

                    sbPlain = new(sbPlain.ToString().TrimEnd());
                    sbTagged = new(sbTagged.ToString().TrimEnd());

                    if (sbPlain.Length == 0){
                        sbTagged.Clear();
                    }
                    int cldbe = targetCol - sbPlain.Length;
                    if (cldbe < 1) cldbe = 1;

                    //Console.WriteLine($"{sbPlain.Length} - {comment} [{cldbe}]");
                    sbPlain.Append(new string(' ', cldbe));
                    sbTagged.Append(new string(' ', cldbe));
                    
                    sbTagged.Append(padOut(SemanticTags.AssemblerComment, ref comment, cols.CommentCol));
                    sbPlain.Append(comment);
                }

                // Reset for next statement
                label = "";
                mnemonic = "";
                comment = "";
                plainOperandParts.Clear();
                taggedOperandParts.Clear();
                seenMnemonic = false;
                seenComment = false;
            }

            // MAIN TOKEN LOOP
            //int c = 1;
            foreach (Token tok in tokens)
            {
                //DBG($"{c++}: {tok.tag} {tok.value}");
                // --- STATEMENT SEPARATOR ---
                if (tok.tag == SemanticTags.StatementSep)
                {
                    // Flush the current statement
                    FlushStatement(first);

                    // Append the ":" itself (untagged or tagged)
                    sbTagged = new(sbTagged.ToString().TrimEnd() + tok.tag + tok.value + SemanticTags.Reset);
                    sbPlain = new(sbPlain.ToString().TrimEnd() + tok.value);

                    first = false;
                    continue;
                }

                // --- LABEL ---
                if (tok.tag == SemanticTags.Label)
                {
                    label = tok.value;
                    continue;
                }

                // --- MNEMONIC ---
                if (tok.tag == SemanticTags.Mnemonic)
                {
                    mnemonic = tok.value;
                    seenMnemonic = true;
                    continue;
                }

                // --- COMMENT ---
                if (tok.tag == SemanticTags.AssemblerComment)
                {
                    comment = tok.value;
                    seenComment = true;
                    continue;
                }

                // --- OPERANDS ---
                if (seenMnemonic && !seenComment)
                {
                    plainOperandParts.Add(tok.value.Trim());
                    taggedOperandParts.Add(tok.tag + tok.value.Trim() + (tok.tag != null ? SemanticTags.Reset : ""));
                }
            }

            // Flush final statement
            FlushStatement(first);

            progline.FormattedTagged = sbTagged.ToString().TrimEnd();
            progline.FormattedPlain = sbPlain.ToString().TrimEnd();

            return true;
        }
        private static string padOut(string tag, ref string value, int width)
        {
            value = value.Trim();       // sanity
            int padlen = width - value.Length;
            if (padlen < 1) padlen = 1;

            string padded = tag + value;
            if (tag != null) padded += SemanticTags.Reset;

            padded += new string(' ', padlen);
            value += new string(' ', padlen);

            return padded;
        }
    }
}