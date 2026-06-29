namespace BasTools.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;

    //***************** Exceptions *****************
    public class BasToolsException : Exception
    {
        public BasToolsException() { }

        public BasToolsException(string message)
            : base(message) { }

        public BasToolsException(string message, Exception inner)
            : base(message, inner) { }
    }
    //
    //***************** The Engine *****************
    //
    public partial class BasToolsEngine
    {
        public Listing CurrentListing { get; private set; } = null;
        public ProgInfo CurrentProgInfo { get; private set; } = null;

        public Dictionary<string, int> DimLines = new();
        // for the benefit of BasAnalysis
        public Dictionary<string, SymbolInfo> Symbols { get; private set; } = new();
        public bool Analyzed { get; private set; } = false;

        // The public 'pipeline' for BasList
        public bool LoadAndFormatFile(string filename, FormattingOptions formatOptions, ProgInfo progInfo)
        {
            Listing listing = new(new List<ProgramLine>());

            if (ProcessRawProgram(filename, listing, progInfo)) // load, detokenise and tag
            {
                //Console.WriteLine($"ProcessRawProgram returned true");
                if (FormatProgram(listing, formatOptions, progInfo))
                {
                    //Console.WriteLine($"FormatProgram returned true");
                    CurrentListing = listing;
                    CurrentProgInfo = progInfo;
                    Analyzed = false;

                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }
        public void Analyse(BasToolsEngine engine, ref bool analyzed)
        {
            Symbols.Clear();
            analyzed = false;
            Analyser.Analyse(engine, ref analyzed);
            Analyzed = analyzed;
        }
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
        public List<DisplayLine> PrepLinesForDisplay(ListerOptions listerOptions)
        {
            return BasLister.PrepLinesForDisplay(CurrentListing, listerOptions, CurrentProgInfo);
        }
        public static bool PrintOneLine(ProgramLine progline, ref int linesprinted)
        {
            return BasLister.PrintOneLine(progline, ref linesprinted);
        }
        public static IEnumerable<Token> WalkTagged(string line)
        {
            if (line == null) yield break;

            // First, collect all items into a temporary list
            List<(string value, string tag)> items = tokenListFromTaggedLine(line);

            // Now yield with correct isLast flag
            for (int n = 0; n < items.Count; n++)
            {
                Token token = new(items[n].tag, items[n].value, (n == items.Count - 1));
                yield return token;
            }
        }
        private static List<(string value, string tag)> tokenListFromTaggedLine(string line)
        {
            var items = new List<(string value, string tag)>();
            int i = 0;

            while (i < line.Length)
            {
                // Tagged token?
                if (line[i] == '{' && i + 2 < line.Length && line[i + 1] == '=')
                {
                    int tagStart = i;

                    int tagEnd = line.IndexOf('}', tagStart);
                    if (tagEnd < 0) break;

                    string tag = line.Substring(tagStart, tagEnd - tagStart + 1);
                    //DBG($"string tag = line.Substring(tagStart, tagEnd - tagStart + 1);\n" +
                    //    $"string {tag} = line.Substring({tagStart}, {tagEnd} - {tagStart} + 1);");

                    int valueStart = tagEnd + 1;
                    int close = line.IndexOf("{/}", valueStart);
                    if (close < 0) break;

                    string value = line.Substring(valueStart, close - valueStart);
                    //DBG($"string value = line.Substring(valueStart, close - valueStart);\n" +
                    //    $"string {value} = line.Substring({valueStart}, {close} - {valueStart});");

                    items.Add((value, tag));

                    i = close + 3;
                }
                else
                {
                    // Untagged text — collect until next '{'
                    int start = i;
                    int next = line.IndexOf('{', i + 1);
                    if (next < 0) next = line.Length;

                    string text = line.Substring(start, next - start);
                    items.Add((text, null));

                    i = next;
                }
            }
            return items;
        }
        public static string getTagValueFromLine(string line, string tag)
        {
            foreach (Token tok in WalkTagged(line))
            {
                if (tok.tag == tag) return tok.value;
            }
            return null;
        }
        static void DumpResourceNames()
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (var name in asm.GetManifestResourceNames())
                Console.WriteLine(name);
        }
        public static SymbolKind InferKind(string tag, string name)
        {
            if (name.EndsWith("()"))
                name = name[..^2];

            if (name.EndsWith('%') && name.Length == 2 && (char.IsAsciiLetterUpper(name[0]) || name[0] == '@'))
                return SymbolKind.StaticInt;
            if (name.EndsWith('%'))
                return SymbolKind.IntVar;
            if (name.EndsWith('$'))
                return SymbolKind.StringVar;
            if (name.StartsWith('.'))
                return SymbolKind.Label;
            if (tag == SemanticTags.Variable)
                return SymbolKind.RealVar;
            if (tag == SemanticTags.FunctionName)
                return SymbolKind.Fn;
            if (tag == SemanticTags.ProcName)
                return SymbolKind.Proc;
            if (tag == SemanticTags.Label)
                return SymbolKind.Label;
            if (tag == SemanticTags.StringLiteral)
                return SymbolKind.LiteralString;
            return SymbolKind.Unknown;
        }
    }//public BasToolsEngine()
}
