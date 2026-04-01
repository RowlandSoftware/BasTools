using System;
using System.Collections.Generic;
using System.Text;

namespace BasTools.Core
{
    public partial class BasToolsEngine
    {
        public bool FormatProgram(Listing lines, FormattedListing listing, FormattingOptions switches, bool BasicV)
        {
            FormatterState state = new();      // this sets initial conditions

            foreach (ProcessedLine progline in lines.ProgramLines)
            {
                FormattedLine formattedLine = new FormattedLine();

                formattedLine.LineNumber = progline.LineNumber;
                string linenumber = formatLineNumber(progline.LineNumber, switches, state);
                formattedLine.FormattedLineNumber = linenumber;

                var tokens = BasToolsEngine.WalkTagged(progline.TaggedLine).ToList();
                for (int i = 0; i < tokens.Count; i++)
                {
                    var (value, tag, isLast) = tokens[i];

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
                    char nxtchar = i < tokens.Count - 1 ? tokens[i + 1].value[0] : '\0';
                    char lastchar = i > 0 && tokens[i - 1].value.Length > 0 ? tokens[i - 1].value[^1] : '\0';
                    bool nextStartsWithSpace = nxtchar == ' ' || isLast;
                    bool lastEndsWithSpace = lastchar == ' ' || i == 0;

                    if (!switches.NoSpaces)
                    {
                        if (tag == SemanticTags.Operator)
                        {
                            if (!lastEndsWithSpace)
                                value = ' ' + value;
                            if (!nextStartsWithSpace)
                                value += ' ';
                        }

                        if (tag == SemanticTags.Keyword)
                        {
                            if (value != "PROC" && value != "FN" && !value.EndsWith('(') && !(value == "TO" && nxtchar == 'P'))
                                if (!nextStartsWithSpace && (nxtchar is not ':' and not '(' and not ')'))
                                    value += " ";
                        }
                    }

                    string temp = tag + value;
                    if (tag != null)
                        temp += "{/}";

                    formattedLine.LineLineOrSegment += value;
                    formattedLine.TaggedLineLineOrSegment += temp;
                }
                formattedLine.IndentLevel = state.Indent;
                state.Indent += state.PendingIndent;
                state.PendingIndent = 0;

                listing.FormattedLines.Add(formattedLine);
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

            if (!switches.NoSpaces) // consider leaving this for the prettyprinter
            {
                if (linenumber != string.Empty) linenumber += " "; // only space if number present
            }
            return linenumber;
        }
    }
}
