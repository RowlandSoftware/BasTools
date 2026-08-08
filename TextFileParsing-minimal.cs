using System;
using System.Collections.Generic;
using System.Text;

namespace BasTools.Core
{
    internal class temp
    {
        public static List<string> keywords;
        public Listing CurrentListing { get; private set; } = null;
        public DisplayLines _LinesForDisplay = null;
        public ProgInfo CurrentProgInfo { get; private set; } = null;
        public Dictionary<string, List<DimInfo>> DimLines = new();
        // for the benefit of BasAnalysis
        public Dictionary<string, SymbolInfo> Symbols { get; private set; } = new();
        public bool Analyzed { get; private set; } = false;
        public Listing LoadAndFormatTextFile(string filename, FormattingOptions formatOptions, ProgInfo progInfo)
        {
            Listing listing = new(new List<ProgramLine>());

            try
            {
                // Load and split
                string rawFile = File.ReadAllText(filename);
                string[] lines = rawFile.Split(new char[] { '\r', '\n' }, StringSplitOptions.TrimEntries); // no need to Trim() each line
                if (lines.Length == 0)
                {
                    throw new BasToolsException($"Text file splits into {lines.Length} lines");
                }

                // loop through the lines
                int fakeLineNumber = 0;
                bool IsInDef = false;

                for (int i = 0; i < lines.Length - 2; i++)
                {
                    if (string.IsNullOrEmpty(lines[i])) // skip empty lines
                        continue;

                    string line = lines[i];
                    ProgramLine progLine = new ProgramLine();

                    // parse line number
                    int lineNumber;
                    int j = 0;

                    if (char.IsAsciiDigit(line[0]))
                    {
                        while (j < line.Length - 1 && char.IsAsciiDigit(line[j]))
                        {
                            j++;
                        }
                        if (!int.TryParse(line.Substring(0, j), out lineNumber))
                        {
                            fakeLineNumber += 10;
                            lineNumber = fakeLineNumber;
                        }
                    }
                    else
                    {
                        fakeLineNumber += 10;
                        lineNumber = fakeLineNumber;
                    }
                    progLine.LineNumber = lineNumber;
                    progLine.FormattedLineNumber = lineNumber.ToString();

                    // parse line body
                    string lineTextBody = line.Substring(j).Trim();
                    progLine.PlainDetokenisedLine = lineTextBody;
                    progLine.FormattedPlain = lineTextBody;
                    bool IsDef = false;
                    progLine.TaggedLine = parseTextLine(lineTextBody, ref IsDef);
                    progLine.IsDef = IsDef;
                    if (IsInDef)
                    {
                        if (lineTextBody.StartsWith("ENDPROC") || lineTextBody.StartsWith("="))
                            IsInDef = false;
                    }
                    progLine.IsInDef = IsInDef;
                    if (IsDef)
                    {
                        progLine.IsInDef = false;
                        IsInDef = true;
                    }

                    listing.Lines.Add(progLine);
                }
                CurrentListing = listing;
                CurrentProgInfo = progInfo;
                Analyzed = false;

                return listing;
            }
            catch (Exception e)
            {
                {
                    throw new BasToolsException("Error in LoadAndFormatTextFile", e);
                }
            }
        } // LoadAndFormatTextFile
        private static string parseTextLine(string textLine, ref bool IsDefLine)
        {
            StringBuilder output = new();
            //bool IsDef = false;
            bool suspendDetokenising = false;

            for (int i = 0; i < textLine.Length; i++)
            {
                bool match = false;
            keywordLoop:;
                foreach (string keyword in keywords)
                {
                    match = false;
                    if (suspendDetokenising) // don't match PROC & Function names e.g. PROCNEWSEASON (NEW & ON are keywords)
                    {
                        if (textLine[i] is ':' or '(' or ' ')
                        {
                            suspendDetokenising = false;
                            output.Append(SemanticTags.Reset);
                        }
                        else
                        {
                            output.Append(textLine[i]);

                            if (i < textLine.Length)
                                i++;
                            break;
                        }
                    }
                    if (textLine.Substring(i).StartsWith(keyword))
                    {
                        output.Append(SemanticTags.Keyword + keyword + SemanticTags.Reset);
                        if (keyword == "PROC")
                        {
                            output.Append(SemanticTags.ProcName);
                            suspendDetokenising = true;
                        }
                        if (keyword == "FN")
                        {
                            output.Append(SemanticTags.FunctionName);
                            suspendDetokenising = true;
                        }
                        if (keyword == "DEF")
                        {
                            //IsDef = true;
                            IsDefLine = true;
                        }
                        if (keyword == "REM")
                        {
                            i += 3;
                            output.Append(SemanticTags.RemText + textLine.Substring(i) + SemanticTags.Reset);
                            return output.ToString();
                        }

                        i += keyword.Length;
                        match = true;
                        break;
                    }
                }
                if (i >= textLine.Length) // if the keyword took us to EOL...
                {
                    if (suspendDetokenising)
                        output.Append(SemanticTags.Reset); // close the tag

                    return output.ToString();
                }
                if (match) goto keywordLoop; // keyword found - loop again

                if (textLine[i] == '"')
                {
                    output.Append(SemanticTags.StringLiteral + '"');
                    while (textLine[++i] != '"' && i < textLine.Length - 1)
                    {
                        output.Append(textLine[i]);
                    }
                    output.Append('"' + SemanticTags.Reset);
                }
                else if (suspendDetokenising && (textLine[i] is ':' or '(' or ' '))
                {
                    output.Append(SemanticTags.Reset + textLine[i]); // close the tag
                    suspendDetokenising = false;
                }
                else
                {
                    output.Append(textLine[i]);
                }
            }
            // EOL
            if (suspendDetokenising) // still not been cancelled
                output.Append(SemanticTags.Reset);

            return output.ToString();
        } // parseTextLine
    }
}
