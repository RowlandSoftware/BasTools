namespace BasTools.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Runtime;
    using System.Text;
    using System.Text.RegularExpressions;
    using static System.Net.Mime.MediaTypeNames;

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
        public DisplayLines _LinesForDisplay = null;
        public ProgInfo CurrentProgInfo { get; private set; } = null;
        public Dictionary<string, List<DimInfo>> DimLines = new();

        // for the benefit of BasAnalysis
        public Dictionary<string, SymbolInfo> Symbols { get; private set; } = new();
        public bool Analyzed { get; private set; } = false;

        // The public 'pipeline' for BasList
        public bool LoadAndFormatFile(string filename, FormattingOptions formatOptions, ProgInfo progInfo, bool NotBasicV)
        {
            Listing listing = new(new List<ProgramLine>());

            if (ProcessRawProgram(filename, listing, progInfo, NotBasicV))  // load, detokenise and tag
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

        // The public 'pipeline' for Text2Basic
        public bool LoadAndTokeniseFile(TokeniserCommandSwitches switches, ProgInfo progInfo)
        {
            Listing listing = new(new List<ProgramLine>());
            try
            {
                // TEMP LOAD FILE - check for already tokenised
                string fn = Path.GetFileName(switches.inputfile); // filename & ext only
                byte[] raw = File.ReadAllBytes(switches.inputfile);

                // determine file type(Acorn or Z80)
                if (raw.Length > 3)
                {
                    int ll = raw[3];
                    if (raw[0] == 13 && raw[ll] == 13)
                    {
                        //return false; // Acorn format tokenised file
                        throw new BasToolsException("\'" + fn + "\' is already a tokenised BASIC program");
                    }
                    else
                    {
                        ll = raw[0];
                        if (raw[ll - 1] == 13)
                        {
                            //return false; // Z80 tokenised file
                            throw new BasToolsException("\'" + fn + "\' is already a tokenised BASIC program");
                        }
                    }
                }

                // LOAD FILE
                string[] lines = Tokeniser.ReadLines(switches.inputfile);
                TokeniserState State = new();
                int FakeLineNum = 0;

                // IDENTIFY ASSEMBLER BLOCKS
                //  1. Convert into list
                List<LineRecord> list = ParseTextLines(lines);
                //  2. Identify
                List<AsmBlock> asmBlocks = DetectAssemblerBlocks(list);
                Dictionary<int, AsmDialect> asmDialects = DetectAssemblerDialects(list, asmBlocks);

                // TOKENISE
                ParserState parserState = new();

                foreach (string textline in lines)
                {
                    ProgramLine result = ProgramLineFromText(textline, parserState, progInfo, asmBlocks, asmDialects, false, false, State, ref FakeLineNum);
                    listing.Lines.Add(result);
                }

                // FORMAT
                FormattingOptions formatOptions = new FormattingOptions(true);
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
            catch (Exception ex)
            {
                throw new BasToolsException ("Error in LoadAndTokeniseFile", ex);
                //return false;
            }
        }
        public ProgramLine ProgramLineFromText(string text, ParserState parserState, ProgInfo progInfo, List<AsmBlock> asmBlocks, Dictionary<int, AsmDialect> asmDialects,
                                                bool Z80, bool SkipSpaces, TokeniserState State, ref int FakeLineNum)
        {
            ProgramLine ProgLine = new();

            // --- First pass ---
            ProgLine.PlainDetokenisedLine = text;
            ProgLine.TokenisedLine = Tokeniser.TokeniseLine(text, Z80, SkipSpaces, State, this, out int linenum, ref FakeLineNum);
            ProgLine.LineNumber = linenum;

            // --- Second pass - detokenise and tag ---
            ProcessLineBody(parserState, ProgLine, asmBlocks, asmDialects, progInfo, false);

            if (ProgLine.LineNumber == 1720)
                Console.WriteLine(ProgLine.TaggedLine);

            // --- Fourth pass - compact tokenised line, inserting implied THEN as required ---
            ProgLine.TokenisedLine = Tokeniser.NormaliseTokenised(ProgLine, this);

            return ProgLine;
        }
        static List<LineRecord> ParseTextLines(string[] lines)
        {
            List<LineRecord> result = new();
            int FakeLineNum = 0;

            foreach (string line in lines)
            {
                string sline = line;
                int num = ParseNumber(ref sline, ref FakeLineNum);
                result.Add(new LineRecord(num, Encoding.Latin1.GetBytes(sline)));
            }
            return result;
        }
        public void Analyse(BasToolsEngine engine, ref bool analyzed)
        {
            Symbols.Clear();
            analyzed = false;
            Analyser.Analyse(engine, ref analyzed);
            Analyzed = analyzed;
        }
        // The public 'pipeline' for text files in BasViewer
        public bool LoadAndFormatTextFile(string filename, ProgInfo progInfo)
        {
            TokeniserCommandSwitches switches = new();
            switches.inputfile = filename;

            try
            {
                LoadAndTokeniseFile(switches, progInfo);
                return true;
            }
            catch (Exception e) {
                throw new BasToolsException("Error tokenising text file", e);
                //return false;
            }
        }
        public DisplayLines PrepLinesForDisplay(ListerOptions listerOptions)
        {
            var LinesForDisplay = BasLister.PrepLinesForDisplay(CurrentListing, listerOptions, CurrentProgInfo);
            _LinesForDisplay = LinesForDisplay;
            return LinesForDisplay; // could return void
        }
        public static bool PrintOneLine(ProgramLine progline, ref int linesprinted)
        {
            return BasLister.PrintOneLine(progline, ref linesprinted);
        }
        public byte[] TokeniseLine(string textline, ref int FakeLineNum)
        {
            TokeniserState State = new();
            return Tokeniser.TokeniseLine(textline, false, false, State, this, out int linenum, ref FakeLineNum);
        }
        /********** UTILITIES *********/
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
        private static int ParseNumber(ref string line, ref int fakeNumber)
        {
            int i = 0;
            line = line.Trim();
            while (i < line.Length && char.IsDigit(line[i]))
                i++;

            if (i == 0)
            {
                // No leading digits at all — line numbers were stripped.
                fakeNumber += 10;
                return fakeNumber;
            }

            string digits = line.Substring(0, i);
            line = line.Substring(i).Trim();

            if (int.TryParse(digits, out int num))
            {
                fakeNumber = num; // keep fake numbering in step, in case later lines lack numbers too
                return num;
            }

            fakeNumber += 10;
            return fakeNumber;
        }
        static void DumpResourceNames()
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (var name in asm.GetManifestResourceNames())
                Console.WriteLine(name);
        }
        public static SymbolKind InferKind(string tag, string name)
        {
            bool array = false;

            if (tag is SemanticTags.Variable or SemanticTags.Array)
            {
                if (name.EndsWith("()"))
                {
                    name = name[..^2];
                    array = true;
                }            
                if (!array && name.EndsWith('%') && name.Length == 2 && (char.IsAsciiLetterUpper(name[0]) || name[0] == '@'))
                    return SymbolKind.StaticInt;
                if (name.EndsWith('%'))
                    return SymbolKind.IntVar;
                if (name.EndsWith('$'))
                    return SymbolKind.StringVar;
                if (name.StartsWith('.'))
                    return SymbolKind.Label;
                // else
                return SymbolKind.RealVar;
            }
            if (tag == SemanticTags.FunctionName)
                return SymbolKind.Fn;
            if (tag == SemanticTags.ProcName)
                return SymbolKind.Proc;
            if (tag == SemanticTags.Label)
                return SymbolKind.Label;
            if (tag == SemanticTags.StringLiteral)
                return SymbolKind.LiteralString;
            if (tag == SemanticTags.RemText)
                return SymbolKind.RemText;
            return SymbolKind.Unknown;
        }
    }//public BasToolsEngine()
}
