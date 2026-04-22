using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BasTools.Core
{
    class ListerState
    {
        public int LineCount;
        public bool Printme;
        public bool Listme;
        public int Indent;
        public int PendingIndent;
        public bool MultiLineIf;
        public ConsoleColor CurrentForeground;
        public ConsoleColor CurrentBackground;
        public ListerState()
        {
            LineCount = 0;
            Printme = false;
            Listme = false;
            Indent = 0;
            PendingIndent = 0;
            MultiLineIf = false;
            CurrentForeground = ConsoleColor.White;
            CurrentBackground = ConsoleColor.Black;
        }
        public ListerState(ListerState other)
        {
            LineCount = other.LineCount;
            Printme = other.Printme;
            Indent = other.Indent;
            PendingIndent = other.PendingIndent;
            Listme = other.Listme;
            MultiLineIf = other.MultiLineIf;
            CurrentForeground = other.CurrentForeground;
            CurrentBackground = other.CurrentBackground;
        }
    }
    // This maps semantic tags to actual console colours
    static class ConsoleColorMap
    {
        private static readonly Dictionary<string, ConsoleColor> _map =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [SemanticTags.Keyword] = ConsoleColor.Blue,
            [SemanticTags.IndentingKeyword] = ConsoleColor.Blue,
            [SemanticTags.OutdentingKeyword] = ConsoleColor.Blue,
            [SemanticTags.InOutKeyword] = ConsoleColor.Blue,
            [SemanticTags.BuiltInFn] = ConsoleColor.Blue,
            [SemanticTags.StringLiteral] = ConsoleColor.Green,
            [SemanticTags.Number] = ConsoleColor.White,
            [SemanticTags.HexNumber] = ConsoleColor.White,
            [SemanticTags.BinaryNumber] = ConsoleColor.White,
            [SemanticTags.Variable] = ConsoleColor.Magenta,
            [SemanticTags.StaticInteger] = ConsoleColor.DarkYellow,
            [SemanticTags.RemText] = ConsoleColor.Yellow,
            [SemanticTags.AssemblerComment] = ConsoleColor.Yellow,
            [SemanticTags.EmbeddedData] = ConsoleColor.White,
            [SemanticTags.ProcName] = ConsoleColor.Cyan,
            [SemanticTags.FunctionName] = ConsoleColor.Cyan,
            [SemanticTags.Label] = ConsoleColor.Magenta,
            [SemanticTags.Register] = ConsoleColor.Green,
            [SemanticTags.Mnemonic] = ConsoleColor.Blue,
            [SemanticTags.Operator] = ConsoleColor.Red,
            [SemanticTags.IsEqualTo_Operator] = ConsoleColor.Red,
            [SemanticTags.IndirectionOperator] = ConsoleColor.White,
            [SemanticTags.ImmediateOperator] = ConsoleColor.White,
            [SemanticTags.LineNumber] = ConsoleColor.DarkGray,
            [SemanticTags.StarCommand] = ConsoleColor.White,
            [SemanticTags.StatementSep] = ConsoleColor.White,
            [SemanticTags.ListSep] = ConsoleColor.White,
            [SemanticTags.OpenBracket] = ConsoleColor.White,
            [SemanticTags.CloseBracket] = ConsoleColor.White,
        };
        public static bool TryGetColor(string tag, out ConsoleColor color, bool darkMode)
        {
            //Console.WriteLine($" -- {tag} --");
            if (_map.TryGetValue(tag, out color))
            {
                if (!darkMode)
                {
                    switch (color)
                    {
                        case ConsoleColor.Yellow: color = ConsoleColor.DarkYellow; break;
                        case ConsoleColor.White: color = ConsoleColor.Black; break;
                    }
                }
                return true;
            }
            return false;
        }
    }
    public class BasLister
    {
        //****************** Display the Output ***********
        public static void displayProgramLines(Listing formattedListing, ListerOptions switches, ProgInfo progInfo)
        {
            ListerState listerState = new(); // this sets initial conditions

            switches.BackColor = ConsoleColor.Black;
            switches.ForeColor = ConsoleColor.White;
            switches.SwopIfLight();

            Console.ForegroundColor = switches.ForeColor;
            Console.BackgroundColor = switches.BackColor;
            listerState.CurrentForeground = switches.ForeColor;
            listerState.CurrentBackground = switches.BackColor;

            if (switches.Clear && !Console.IsOutputRedirected) Console.Clear();
            int linesprinted = 0;
            //
            // ******** LISTING STARTS HERE ********
            //
            string format = progInfo.BasicDialect;
            if (!switches.Bare && !switches.FlgList) Console.WriteLine($"\nListing '{progInfo.Filename}' from line {switches.FromLine} to {switches.ToLine} ({format} format)\n");

            string sIndent = string.Empty;

            foreach (ProgramLine progline in formattedListing.Lines)
            {
                if (progline.LineNumber >= switches.FromLine && progline.LineNumber <= switches.ToLine)
                {
                    // set flags
                    listerState.Printme = false;
                    if (switches.FlgIf)
                    {
                        foreach (string param in switches.DirectiveParams)
                        {
                            if (progline.NoSpacesLine.Contains(param, StringComparison.OrdinalIgnoreCase)) { listerState.Printme = true; continue; }
                        }
                    }
                    if (switches.FlgIfX)
                    {
                        string cleanline = progline.PlainDetokenisedLine;
                        foreach (string param in switches.DirectiveParams)
                        {
                            if (cleanline.Contains(param, StringComparison.Ordinal)) { listerState.Printme = true; continue; }
                        }
                    }
                    if (switches.FlgList)
                    {
                        if (progline.IsDef)
                        {
                            listerState.Listme = nameMatch(progline.TaggedLine, switches); // automatically cancels ListMe at DEF if no match
                        }
                    }

                    bool insideIf = switches.FlgIf || switches.FlgIfX;
                    bool shouldPrint =
                        (!insideIf && !switches.FlgList) ||
                        (insideIf && listerState.Printme) ||
                        (switches.FlgList && listerState.Listme);

                    if (shouldPrint)
                    {
                        //******************* WRITE ONE LINE TO CONSOLE ************************

                        if (!switches.SplitLines)
                        {
                            printLineOut(progline, switches, listerState, ref linesprinted);
                        }
                        else // SplitLines
                        {
                            bool first = true;

                            Listing sections = new(new List<ProgramLine>());

                            // generate a 'min-program-listing' from the sections
                            foreach (string taggedSection in SplitStatements(progline.TaggedLine))
                            {
                                ProgramLine line = new(progline);
                                line.TaggedLine = taggedSection;

                                line.IndentLevel = progline.IndentLevel;
                                line.LineNumber = progline.LineNumber;
                                line.InAsm = progline.InAsm;
                                line.IsArm = progline.IsArm;
                                line.IsDef = progline.IsDef;
                                line.IsInDef = progline.IsInDef;

                                sections.Lines.Add(line);
                            }

                            // Print normally if only one section - not necessary, just more efficient
                            if (sections.Lines.Count == 1)
                            {
                                printLineOut(progline, switches, listerState, ref linesprinted);
                            }
                            else
                            {
                                // Call the Formatter to format these 'lines'
                                BasToolsEngine engine = new BasToolsEngine();

                                engine.formatLines(sections, switches.copyToFormatOptions(), progline.fstate, progInfo, true);

                                foreach (ProgramLine line in sections.Lines)
                                {
                                    int printedLineLength = 0;
                                    PrintLineNumber(line, switches, ref printedLineLength, first);
                                    first = false;
                                    PrintIndents(line, ref printedLineLength, switches);

                                    PrintLineBody(line.FormattedTagged.TrimStart(), listerState, switches, ref printedLineLength, ref linesprinted);

                                    if (switches.FlgPause)
                                    {
                                        switch (CheckForPause(switches, ref linesprinted))
                                        {
                                            case ConsoleKey.Spacebar: linesprinted = 0; break;
                                            case ConsoleKey.Enter: linesprinted--; break;
                                            case ConsoleKey.Escape: ResetAndExit(switches); break;
                                        }
                                    }
                                }
                            }
                        }
                        // After printing the line, turn off printing PROC (so don't suppress ENDPROC)
                        if (switches.FlgList && listerState.Listme)
                        {
                            if (!progline.IsDef && !progline.IsInDef)
                            {
                                listerState.Listme = false;
                            }
                        }
                        #region debug
                        if (switches.Debug || switches.FullDebug)
                        {
                            Console.WriteLine($"{progline.LineNumber} -");

                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.Write($"  Plain:     ");
                            Console.ForegroundColor = listerState.CurrentForeground;
                            Console.WriteLine($"{progline.PlainDetokenisedLine}");

                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.Write($"  Formatted: ");
                            Console.ForegroundColor = listerState.CurrentForeground;
                            Console.WriteLine($"{progline.FormattedPlain}");

                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.Write($"  Tagged:    ");
                            Console.ForegroundColor = listerState.CurrentForeground;
                            Console.WriteLine($"{progline.TaggedLine}");

                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.Write($"  Formatted: ");
                            Console.ForegroundColor = listerState.CurrentForeground;
                            Console.WriteLine($"{progline.FormattedTagged}");

                            Console.WriteLine();
                        }
                        if (switches.FullDebug)
                        {
                            Console.WriteLine("Indent: {0,-10}IsDef: {1,-10}IsInDef: {2,-10}", progline.IndentLevel, progline.IsDef, progline.IsInDef);
                            Console.WriteLine("InAsm:  {0,-10}IsArm: {1,-10}IsZ80:   {2,-10}", progline.InAsm, progline.IsArm, progline.IsZ80);
                            Console.WriteLine();
                        }
                        #endregion
                    } // end shouldprint
                }
            }
        }

        // ******** PrintLineBody - handles plain and PrettyPrint ********
        static void PrintLineBody(string line, ListerState listerState, ListerOptions switches, ref int printedLineLength, ref int linesprinted)
        {
            // Line contents
            foreach (Token tok in BasToolsEngine.WalkTagged(line))
            {
                if (tok.tag != null && ConsoleColorMap.TryGetColor(tok.tag, out var c, switches.FlgDark))
                {
                    if (switches.Pretty) Console.ForegroundColor = c;

                    Console.Write(tok.value);

                    Console.BackgroundColor = switches.BackColor;
                    Console.ForegroundColor = switches.ForeColor;
                }
                else
                {
                    Console.Write(tok.value);
                }
            }
            Console.WriteLine("");
            int windowWidth = Console.WindowWidth;
            int rows = (printedLineLength / windowWidth) + 1; // For operands of integer types, the result of the / operator an integer, the quotient of the two operands rounded toward zero

            linesprinted += rows;
        }
        static void printLineOut(ProgramLine progline, ListerOptions switches, ListerState listerState, ref int linesprinted)
        {
            int printedLineLength = 0;

            if (!switches.NoFormat)
            {
                // Normal behaviour
                PrintLineNumber(progline, switches, ref printedLineLength, true);
                PrintIndents(progline, ref printedLineLength, switches);
                PrintLineBody(progline.FormattedTagged, listerState, switches, ref printedLineLength, ref linesprinted);
            }
            else
            {
                // plain printout without additional spaces
                string printedLine = (switches.NoLineNumbers ? "" : (progline.FormattedLineNumber +
                    (!switches.NoFormat ? ' ' : ""))) +
                    (switches.FlgIndent ? new string(' ', progline.IndentLevel * 2) : "") +
                    (switches.FlgEmphDefs ? new string(' ', progline.DefIndent * 2) : "") +
                    (switches.NoFormat ? progline.PlainDetokenisedLine : progline.FormattedPlain);
                Console.WriteLine(printedLine);

                printedLineLength += printedLine.Length;

                int windowWidth = Console.WindowWidth;
                int rows = (printedLineLength + (windowWidth - 1)) / windowWidth;

                linesprinted += rows;
            }
            // Deal with pausing
            if (switches.FlgPause)
            {
                switch (CheckForPause(switches, ref linesprinted))
                {
                    case ConsoleKey.Spacebar: linesprinted = 0; break;
                    case ConsoleKey.Enter: linesprinted--; break;
                    case ConsoleKey.Escape: ResetAndExit(switches); break;
                }
            }
        }
        static void PrintLineNumber(ProgramLine progline, ListerOptions switches, ref int printedLineLength, bool first)
        {
            // Line preamble
            if (!switches.NoLineNumbers && progline.FormattedLineNumber.Length > 0)
            {
                string ln = progline.FormattedLineNumber;
                printedLineLength = ln.Length + (switches.NoFormat ? 0 : 1);

                if (!first) ln = new string(' ', ln.Length);
                ln = SemanticTags.LineNumber + ln + SemanticTags.Reset;

                foreach (Token tok in BasToolsEngine.WalkTagged(ln)) // retrieve line no. colour
                {
                    if (tok.tag != null && ConsoleColorMap.TryGetColor(tok.tag, out var c, switches.FlgDark))
                    {
                        if (switches.Pretty) { Console.ForegroundColor = c; }
                        Console.Write(tok.value);
                    }
                    else
                        Console.Write(ln);
                    Console.ForegroundColor = switches.ForeColor;

                    if (!switches.NoFormat)
                        Console.Write(' ');
                }
            }
        }
        static void PrintIndents(ProgramLine progline, ref int printedLineLength, ListerOptions switches)
        {
            //if (!progline.InAsm)
            //{
            if (switches.FlgIndent)
            {
                Console.Write(new string(' ', progline.IndentLevel * 2)); //// ignore indents in assembler - assume is in [OPT opt% loop
                printedLineLength += progline.IndentLevel * 2;
            }
            //}
            if (switches.FlgEmphDefs)
            {
                Console.Write(new string(' ', progline.DefIndent * 2));
                printedLineLength += progline.DefIndent * 2;
            }

        }
        static bool nameMatch(string taggedline, ListerOptions switches)
        {
            if (!switches.FlgList) return false;

            var (type, procName) = readProcFnName(taggedline);

            if (procName == null) return false;
            procName = (type == SemanticTags.ProcName ? "PROC" : "FN") + procName;

            for (int i = 0; i < switches.DirectiveParams.Count; i++)
            {
                //Console.WriteLine($"Matching {procName} - {switches.DirectiveParams[i]}");
                if (procName.Equals(switches.DirectiveParams[i], StringComparison.InvariantCultureIgnoreCase)) return true;
            }
            return false;
        }
        static (string type, string ProcFnName) readProcFnName(string taggedline)
        {
            foreach (Token tok in BasToolsEngine.WalkTagged(taggedline))
            {
                if (tok.tag is SemanticTags.ProcName or SemanticTags.FunctionName) return (tok.tag, tok.value);
            }
            return (null!, null!);
        }
        static List<string> SplitStatements(string code)
        {
            var result = new List<string>();
            var sb = new StringBuilder();

            var tokens = BasToolsEngine.WalkTagged(code).ToList();
            for (int i = 0; i < tokens.Count; i++)
            {
                Token tok = tokens[i];
                sb.Append(tok.tag + tok.value);
                if (!string.IsNullOrEmpty(tok.tag))
                    sb.Append("{/}");

                if (i < tokens.Count - 1 && tok.tag == SemanticTags.StatementSep && tokens[i + 1].value == "THEN")
                    continue;

                if (tok.tag != SemanticTags.StatementSep && i < tokens.Count - 1 && tokens[i + 1].value == "ELSE")
                {
                    result.Add(sb.ToString().TrimEnd());
                    sb.Clear();
                }

                if (tok.isLast || tok.tag == SemanticTags.StatementSep || tok.value == "ELSE" || tok.value == "THEN")
                {
                    result.Add(sb.ToString().TrimEnd());
                    sb.Clear();
                }
            }
            return result;
        }
        //**** Utility functions ******
        private static ConsoleKey CheckForPause(ListerOptions switches, ref int linesprinted)
        {
            if (linesprinted == Console.WindowHeight - 4)
            {
                Console.ForegroundColor = switches.ForeColor;

                string prompt = " -- Enter - next line | Space - Continue | Esc - End --";
                if (Console.WindowWidth <= prompt.Length)
                {
                    prompt = "[Enter | Space | Esc]";
                }
                Console.Write(prompt);
                // Read until a valid key is pressed
                ConsoleKey key;
                while (true)
                {
                    var info = Console.ReadKey(intercept: true);
                    key = info.Key;

                    if (key == ConsoleKey.Spacebar ||
                        key == ConsoleKey.Enter ||
                        key == ConsoleKey.Escape)
                    {
                        break; // valid key
                    }
                }
                // Clear the prompt once
                ClearCurrentConsoleLine();
                return key;
            }
            return ConsoleKey.None;
        }
        private static void ClearCurrentConsoleLine()
        {
            if (Console.IsOutputRedirected) return;
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            for (int i = 0; i < Console.WindowWidth; i++)
                Console.Write(" ");
            Console.SetCursorPosition(0, currentLineCursor);
        }
        static void ResetAndExit(ListerOptions s)
        {
            Console.ForegroundColor = s.ForeColor;
            Console.BackgroundColor = s.BackColor;
            Environment.Exit(0);
        }
    }
}
