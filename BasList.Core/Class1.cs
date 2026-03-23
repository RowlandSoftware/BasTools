namespace BasList.Core
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Reflection.PortableExecutable;
    using System.Text;
    using System.Text.RegularExpressions;
    using static System.Runtime.InteropServices.JavaScript.JSType;

    class ParserState
    {
        public byte[] Data;
        public int Ptr;
        public bool Z80;
        public int LineCount;
        public bool Printme;

        public List<string> DirectiveParams = new();

        public int DataSize;
        public int Bound;
        public int Indent;
        public bool Listme;
        public bool InAsm;
        public ParserState()
        {
            Data = Array.Empty<byte>();
            Z80 = false;
            Ptr = 0;
            LineCount = 0;
            Printme = false;
            DataSize = 0;
            Bound = 0;
            Indent = 0;
            InAsm = false;
        }
        public ParserState(ParserState other)
        {
            Data = other.Data;                     // shared buffer (correct for lookahead)
            Ptr = other.Ptr;
            Z80 = other.Z80;
            LineCount = other.LineCount;
            Printme = other.Printme;

            DataSize = other.DataSize;
            Bound = other.Bound;
            Indent = other.Indent;
            Listme = other.Listme;
            InAsm = other.InAsm;

            // Deep copy so lookahead doesn't mutate the real list
            DirectiveParams = new List<string>(other.DirectiveParams);
        }
    }
    class CommandSwitches
    {
        public bool BasicV;
        public bool FlgAddNums;
        public bool FlgIndent;
        public bool Align;
        public bool NoSpaces;
        public bool NoLineNumbers;
        public bool Bare;
        public bool Pretty;
        public bool FlgIf;
        public bool FlgIfX;
        public bool FlgList;
        private bool _flgPause;
        public bool FlgDark;
        public int FromLine;
        public int ToLine;

        public ConsoleColor ForeColor;
        public ConsoleColor BackColor;
        public CommandSwitches()
        {
            BasicV = false;
            FlgAddNums = false;
            FromLine = 0;
            ToLine = -1;
            FlgIndent = false;
            Align = false;
            NoSpaces = false;
            NoLineNumbers = false;
            Bare = false;
            Pretty = false;
            FlgIf = false;
            FlgIfX = false;
            FlgList = false;
            FlgPause = false;
            FlgDark = true;
        }
        public bool FlgPause
        {
            get => !Console.IsOutputRedirected && _flgPause;
            set => _flgPause = value;
        }
        public void checkFromTo()
        {
            if (FromLine > ToLine) (FromLine, ToLine) = (ToLine, FromLine); // swop using tuple
        }
        public void SwopIfLight()
        {
            if (!FlgDark) (ForeColor, BackColor) = (BackColor, ForeColor);
        }
    }
    class Program
    {
        static readonly Dictionary<byte, string> token = new();
        static readonly Dictionary<int, string> Vtoken = new();
        static readonly HashSet<string> Mnemonics6502 = new(StringComparer.OrdinalIgnoreCase)
        {
        "ADC", "AND", "ASL", "BCC", "BCS", "BEQ", "BIT", "BMI", "BNE", "BPL", "BRA", "BRK", "BVC", "BVS", "CLC", "CLD", "CLI",
        "CLR", "CLV", "CMP", "CPX", "CPY", "DEC", "DEX", "DEY", "EOR", "INC", "INX", "INY", "JMP", "JSR", "LDA", "LDX", "LDY",
        "LSR", "NOP", "OPT", "ORA", "PHA", "PHP", "PHX", "PHY", "PLA", "PLP", "PLX", "PLY", "ROL", "ROR", "RTI", "RTS", "SBC",
        "SEC", "SED", "SEI", "STA", "STX", "STY", "STZ", "TAX", "TAY", "TRB", "TSB", "TSX", "TXA", "TXS", "TYA"
        };
        static readonly HashSet<string> ArmMnemonics = LoadMnemonicTable("Baslist.ArmMnemonics.txt");
        static readonly HashSet<string> ArmRegisters = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
        "R0","R1","R2","R3","R4","R5","R6","R7",
        "R8","R9","R10","R11","R12","R13","R14","R15",
        "SP","LR","PC"
        };

        static void Main(string[] args)
        {
            //DumpResourceNames();

            if (args.Length == 0)
            {
                help();
                Environment.Exit(0);
            }

            ParserState State = new();
            CommandSwitches switches = new();
            string fn = string.Empty;
            string format = string.Empty;

            try
            {
                foreach (string arg in args)
                {
                    bool recognised = false;
                    if ((arg.StartsWith('/') || arg.StartsWith('-')) && arg.Length > 1)
                    {
                        string arg2 = arg.Substring(1).ToUpper();
                        if (arg2 == "V") { switches.BasicV = true; recognised = true; }
                        if ("ADDNUMBERS".StartsWith(arg2)) { switches.FlgAddNums = true; recognised = true; }
                        if (arg2 == "?" || arg2 == "H") { help(); Environment.Exit(0); }
                        if ("BARE".StartsWith(arg2)) { switches.Bare = true; recognised = true; }
                        if ("PAUSE".StartsWith(arg2)) { switches.FlgPause = true; recognised = true; }
                        if (arg2.Contains('='))
                        {
                            string arg1 = arg2.Substring(0, arg2.IndexOf('='));

                            if ("FILE".StartsWith(arg1))
                            {
                                fn = arg2.Substring(arg2.IndexOf('=') + 1);
                                recognised = true;
                            }
                        }
                        if ("PRETTYPRINT".StartsWith(arg2)) { switches.Pretty = true; recognised = true; }
                        if ("ALIGN".StartsWith(arg2)) { switches.Align = true; recognised = true; }
                        if ("INDENT".StartsWith(arg2)) { switches.FlgIndent = true; recognised = true; }
                        if ("NONUMBERS".StartsWith(arg2)) { switches.NoLineNumbers = true; recognised = true; }
                        if ("NOSPACES".StartsWith(arg2)) { switches.NoSpaces = true; recognised = true; }
                        if ("DARK".StartsWith(arg2)) { switches.FlgDark = true; recognised = true; }
                        if ("LIGHT".StartsWith(arg2)) { switches.FlgDark = false; recognised = true; }
                        if (!recognised && !switches.Bare) Console.Error.WriteLine("Option " + arg.ToLower() + " not recognised");
                    }
                    // not a switch ...
                    if (IsNumeric(arg))
                    {
                        recognised = true;
                        if (arg.EndsWith(','))
                        {
                            switches.FromLine = int.Parse(arg.Substring(0, arg.Length - 1));
                            switches.ToLine = 0xFFFF;
                        }
                        else if (arg.StartsWith(','))
                            switches.ToLine = int.Parse(arg.Substring(1));
                        else if (arg.Contains(','))
                        {
                            string[] temp = arg.Split(',');
                            switches.FromLine = int.Parse(temp[0]);
                            switches.ToLine = int.Parse(temp[1]);
                        }
                        else
                        {
                            if (switches.FromLine == 0) switches.FromLine = int.Parse(arg); else switches.ToLine = int.Parse(arg);
                        }
                        if (switches.ToLine == -1) switches.ToLine = switches.FromLine;
                        switches.checkFromTo(); // reverse From Line and To Line if wrong way round
                    }
                    else if (fn.Length == 0 && !recognised) // This is where we pick up the filename if not already found
                    {
                        if (arg != "IF" && arg != "IFX" && arg != "LIST")
                            fn = arg;
                    }
                    if (string.Equals(arg, "IF", StringComparison.OrdinalIgnoreCase))
                    {
                        switches.FlgIf = true;
                        getDirectiveParams(args, State);
                        break;
                    }
                    else if (string.Equals(arg, "IFX", StringComparison.OrdinalIgnoreCase))
                    {
                        switches.FlgIfX = true;
                        getDirectiveParams(args, State);
                        break;
                    }
                    else if (string.Equals(arg, "LIST", StringComparison.OrdinalIgnoreCase))
                    {
                        switches.FlgList = true;
                        getDirectiveParams(args, State);
                        break;
                    }
                }
                if (switches.FlgList) { switches.FromLine = 0; switches.ToLine = 0xFFFF; } // line numbers ignored for LIST
            }
            catch (System.FormatException fe)
            {
                Console.Error.WriteLine("Line numbers not in correct format");
                //Console.WriteLine(fe.Message);
                Environment.Exit(0);
            }
            if (switches.ToLine < 0) switches.ToLine = 0xffff;

            // no filename found:
            if (fn.Length == 0)
            {
                Console.Error.WriteLine("Error: No filename found");
                help();
                Environment.Exit(0);
            }
            if (switches.Bare) switches.FlgPause = false;
            //List<string> listing = new();

            try
            {
                using (FileStream stream = new(fn, FileMode.Open, FileAccess.Read))
                {
                    int size = (int)stream.Length;
                    State.Data = new byte[size];
                    int read = stream.Read(State.Data, 0, size);
                    if (read != size)
                    {
                        Console.Error.WriteLine("File read incomplete.");
                        Environment.Exit(0);
                    }
                    State.DataSize = size;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Environment.Exit(0);
            }
            // now we've loaded the file, show message
            Console.Error.WriteLine("Processing, please wait...");

            // determine file type (Acorn or Z80)
            int ll = State.Data[3];
            if (State.Data[0] == 13 && State.Data[ll] == 13)
            {
                State.Z80 = false;
                format = "Acorn";
            }
            else
            {
                ll = State.Data[0];
                if (State.Data[ll - 1] == 13)
                {
                    State.Z80 = true;
                    format = "Z80";
                }
                else
                {
                    Console.Error.WriteLine("\n" + fn + " not a BASIC program");
                    Environment.Exit(0);
                }
            }
            string table = GetEmbeddedResourceContent("Baslist.TokenTable.txt");
            string[] tokenlist = table.Split("\r\n");
            foreach (string tline in tokenlist)
            {
                string[] temp = tline.Split('\t');
                token.Add(byte.Parse(temp[1]), temp[0]);
            }
            if (switches.BasicV)
            {
                table = GetEmbeddedResourceContent("Baslist.VTokenTable.txt");
                string[] vtokenlist = table.Split("\r\n");
                foreach (string tline in vtokenlist)
                {
                    string[] temp = tline.Split('\t');
                    int vtoken = byte.Parse(temp[1]) * 256 + byte.Parse(temp[2]);
                    Vtoken.Add(vtoken, temp[0]);
                }
            }
            switches.BackColor = ConsoleColor.Black;
            switches.ForeColor = ConsoleColor.White;
            switches.SwopIfLight();

            Console.ForegroundColor = switches.ForeColor;
            Console.BackgroundColor = switches.BackColor;
            //Console.Clear();
            int linesprinted = 0;

            //
            // ******** LISTING STARTS HERE ********
            //

            if (!switches.Bare && !switches.FlgList) Console.WriteLine($"\nListing {fn} from line {switches.FromLine} to {switches.ToLine} ({format} format)\n");

            State.Ptr = 0; if (State.Z80) --State.Ptr; // Acorn prefaces program with CR; Russel doesn't
            string line;
            string linenospaces;
            string linenumber;
            string sIndent = string.Empty;
            State.LineCount = 0;
            State.Indent = 0;
            State.Listme = false;

            while (State.Ptr < State.DataSize)
            {
                // start with line number
                linenumber = "";
                State.LineCount++;
                if (!State.Z80 && ((switches.BasicV && State.Data[State.Ptr + 1] == 255) || State.Data[State.Ptr + 1] > 127)) ResetAndExit(switches); // End of program marker (Acorn)
                if (State.Z80 && State.Data[State.Ptr + 2] == 255 && State.Data[State.Ptr + 3] == 255) ResetAndExit(switches);                        // End of program marker (Z80)

                int lineno = GetLineNumber(State, switches);

                linenumber = lineno.ToString();
                if (lineno == 0 && State.Z80) linenumber = string.Empty;
                if (switches.Align) linenumber = linenumber.PadLeft(5);
                if (switches.Pretty) linenumber = "{=DarkGray}" + linenumber + "{/}";
                if (!switches.NoSpaces)
                {
                    linenumber += " ";
                    sIndent = "".PadLeft(State.Indent * 2, ' ');
                }

                // Line contents

                line = getLineBody(State, switches, out linenospaces); // leaves State.Ptr pointing to next line (or EOF)

                //Check that indent hasn't reduced in this line
                if (State.Indent * 2 < sIndent.Length)
                {
                    sIndent = "".PadLeft(State.Indent * 2, ' ');
                }

                if (switches.FlgIf)
                {
                    State.Printme = false;
                    foreach (string param in State.DirectiveParams)
                    {
                        if (linenospaces.Contains(param, StringComparison.OrdinalIgnoreCase)) { State.Printme = true; continue; }
                    }
                }
                if (switches.FlgIfX)
                {
                    State.Printme = false;
                    string cleanline = Regex.Replace(line, @"\{.*?\}", "");
                    foreach (string param in State.DirectiveParams)
                    {
                        if (cleanline.Contains(param, StringComparison.Ordinal)) { State.Printme = true; continue; }
                    }
                }
                if (!switches.FlgIndent) sIndent = "";
                if (switches.NoLineNumbers) linenumber = "";

                if (lineno >= switches.FromLine && lineno <= switches.ToLine)
                {
                    bool insideIf = switches.FlgIf || switches.FlgIfX;
                    bool shouldPrint =
                        (!insideIf && !switches.FlgList) ||
                        (insideIf && State.Printme) ||
                        (switches.FlgList && State.Listme);

                    if (shouldPrint)
                    {
                        //******************* WRITE TO CONSOLE ************************
                        if (switches.Pretty)
                        {
                            PrettyPrint(linenumber + sIndent + line.TrimEnd(), State, switches);
                        }
                        else
                        {
                            Console.WriteLine(linenumber + sIndent + line.TrimEnd());
                            //Console.WriteLine(linenumber + linenospaces.TrimEnd());
                        }
                        // See whether listme should be cancelled
                        if (switches.FlgList && State.Listme)
                        {
                            // LOOKAHEAD
                            var cloneState = new ParserState(State);
                            if (isEndOfProc(cloneState, switches))
                            {
                                State.Listme = false;
                                Console.WriteLine();
                            }
                        }

                        // Deal with pausing
                        if (switches.FlgPause)
                        {
                            linesprinted++;
                            if (linesprinted == Console.WindowHeight - 2)
                            {
                                Console.ForegroundColor = switches.ForeColor;
                                Console.Write(" -- Enter - next line | Space - Continue | Esc - End --");
                                System.ConsoleKeyInfo key = Console.ReadKey(false);
                                ClearCurrentConsoleLine();
                                switch (key.Key)
                                {
                                    case ConsoleKey.Spacebar: linesprinted = 0; break;
                                    case ConsoleKey.Enter: linesprinted--; break;
                                    case ConsoleKey.Escape: ResetAndExit(switches); break;
                                }
                            }
                        }
                    }
                }
            } // Endwhile
        } // End Main()
        static int GetLineNumber(ParserState s, CommandSwitches switches)
        {
            int lineno;
            int linelen;

            if (s.Z80)
            {
                lineno = s.Data[s.Ptr + 2] + s.Data[s.Ptr + 3] * 256;
                linelen = s.Data[s.Ptr + 1];
                if (lineno == 0 && switches.FlgAddNums)
                    lineno = s.LineCount * 10;
            }
            else
            {
                lineno = s.Data[s.Ptr + 1] * 256 + s.Data[s.Ptr + 2];
                linelen = s.Data[s.Ptr + 3];
            }

            int bound = s.Ptr + linelen - 1;
            //Console.WriteLine($"Ptr={s.Ptr}, linelen={linelen}, bound={bound}, data[bound+1]={s.Data[bound + 1]}");

            if (s.Data[bound + 1] != 13)
            {
                string ordinal = "th";
                if (s.LineCount.ToString().EndsWith('1') && s.LineCount != 11) ordinal = "st";
                if (s.LineCount.ToString().EndsWith('2') && s.LineCount != 12) ordinal = "nd";
                if (s.LineCount.ToString().EndsWith('3') && s.LineCount != 13) ordinal = "rd";

                Console.WriteLine(
                    "Line structure error at &" + bound.ToString("X4") +
                    $" after {s.LineCount}{ordinal} line of program");

                ResetAndExit(switches);
            }
            s.Bound = bound;
            return lineno;
        }
        static string getLineBody(ParserState State, CommandSwitches switches, out string linenospaces)
        {
            string line = string.Empty;
            linenospaces = string.Empty;

            bool quote = false;
            bool rem = false;
            bool flgFnOrProc = false;
            State.Printme = false;
            bool flgVar = false;
            bool flgHex = false;
            bool startOfStatement = true;
            int prevbyte = 0;
            bool asmComment = false;

            for (int i = State.Ptr + 4; i <= State.Bound && i < State.DataSize; i++)
            {
                byte curbyte = State.Data[i];
                char curchar = (char)curbyte;
                char nxtchar = (char)State.Data[i + 1]; // should really check, but never out of bounds because of end-of-program markers

                if (curbyte == '*' && startOfStatement) // star command
                    rem = true;                         // rem ensures no further processing

                if (startOfStatement && curbyte == '[')
                    State.InAsm = true;

                if (startOfStatement && curbyte == ']')
                    State.InAsm = false;

                if ((curbyte < 127 && curbyte > 31) || rem || quote) // copy these bytes verbatim
                {
                    if (curbyte > 32 || rem || quote) linenospaces += curchar;
                    if (rem)
                        line += curchar;
                    else
                    {
                        if (curbyte == 34)
                        {
                            if (switches.Pretty)
                            {
                                if (!quote) line += "{=Green}" + '"'; else line += '"' + "{/}";
                            }
                            else
                            {
                                line += '"';
                            }
                            quote = !quote;
                        }
                        else
                        {
                            // things that aren't quotes "
                            if (State.InAsm && !quote)
                            {
                                if (!asmComment && curchar is '\\' or ';' or '\'')
                                {
                                    asmComment = true;
                                    if (switches.Pretty)
                                        if (switches.FlgDark) line += "{=Yellow}"; else line += "{=DarkYellow}";
                                }
                                else if (asmComment && curchar == ':')
                                {
                                    asmComment = false;
                                    if (switches.Pretty) line += "{/}";
                                    // fall through so ':' still acts as statement separator
                                }

                                if (asmComment)
                                {
                                    // In assembler comment: just copy the character and skip all other logic
                                    line += curchar;
                                    linenospaces += curchar;
                                    prevbyte = curbyte;
                                    continue;
                                }
                            }

                            if (State.InAsm && switches.BasicV)
                            {
                                // ARM: Detect tokens like R0, R12, SP, LR, PC
                                char uchar = char.ToUpperInvariant(curchar);
                                char unext = char.ToUpperInvariant(nxtchar);
                                if (uchar is 'R' && char.IsDigit(nxtchar) || (curchar is 'S' or 'L' or 'P' && unext is 'P' or 'R' or 'C'))
                                {
                                    // R0–R15, SP, LR, PC
                                    string reg = readRegister(i, State);   // similar to readMnemonic
                                    if (ArmRegisters.Contains(reg))
                                    {
                                        if (switches.Pretty)
                                            line += "{=Green}" + reg.ToUpper() + "{/}";
                                        else
                                            line += reg;

                                        i += reg.Length - 1;   // advance past the whole register
                                        curbyte = State.Data[i];
                                        curchar = (char)curbyte;
                                        nxtchar = (char)State.Data[i + 1];
                                        prevbyte = (byte)line[^1];
                                        continue;
                                    }
                                }
                            }

                            // 6502: deal with ROL A, STA (&70),Y, LDA &80,X
                            bool isIndexRegister = State.InAsm && (curchar is 'X' or 'x' or 'Y' or 'y') && prevbyte == ',';   // or previous non-space char if you want to be stricter
                            bool isAccumulator = State.InAsm && char.ToUpperInvariant(curchar) == 'A' && !char.IsAsciiLetterOrDigit((char)prevbyte);
                            bool isRegister = !switches.BasicV && (isIndexRegister || isAccumulator);
                            if (isRegister) curchar = char.ToUpperInvariant(curchar);

                            if (State.InAsm)
                            {
                                string mnemonic = readMnemonic(i, State);
                                if (mnemonic != string.Empty)
                                {
                                    bool isMnemonic;
                                    if (switches.BasicV)
                                    {
                                        isMnemonic = ArmMnemonics.Contains(mnemonic);
                                    }
                                    else
                                    {
                                        isMnemonic = Mnemonics6502.Contains(mnemonic) || Regex.IsMatch(mnemonic, "EQU[BDSW]", RegexOptions.IgnoreCase);
                                    }

                                    if (isMnemonic)
                                    {
                                        if (switches.Pretty)
                                        {
                                            line += "{=Blue}" + mnemonic.ToUpper() + "{/}";
                                        }
                                        else
                                        {
                                            line += mnemonic;
                                        }
                                        i += mnemonic.Length - 1;
                                        curbyte = State.Data[i];
                                        curchar = (char)curbyte;
                                        nxtchar = (char)State.Data[i + 1];
                                        prevbyte = (byte)line[^1];
                                        continue;
                                    }
                                }
                            }

                            if (flgFnOrProc && !char.IsAsciiLetterOrDigit(curchar) && curbyte != '_')
                            {
                                flgFnOrProc = false;
                                if (switches.Pretty)
                                    line += "{/}";
                            }
                            else
                            {
                                if (flgVar && !char.IsAsciiLetterOrDigit(curchar) && curchar is not '%' and not '$' and not '_')
                                {
                                    flgVar = false;
                                    if (switches.Pretty)
                                        line += "{/}";
                                }
                            }
                            if (curchar is ':' or ']' && !rem && !quote)
                                startOfStatement = true;  // a colon outside of quotes or REM is new statement; so is assembler delimiter
                            else if (curchar != ' ')
                                startOfStatement = false; // anything else isn't

                            if (flgHex && !char.IsAsciiHexDigit(curchar)) { flgHex = false; }

                            if (curbyte == '&') { flgHex = true; }

                            if (switches.Pretty && curchar is '+' or '-' or '/' or '*' or '=' or '<' or '>' or '^' && !rem && !quote) // fails on 1E-5, for example
                            {
                                char p = (char)prevbyte;
                                if (p is not '+' and not '-' and not '/' and not '*' and not '=' and not '<' and not '>' and not '^')
                                {
                                    if (!line.EndsWith(' ') && p is not ' ')
                                        line += ' ';
                                    line += "{=Red}";
                                }
                                line += curchar;
                                if (nxtchar is not '+' and not '-' and not '/' and not '*' and not '=' and not '<' and not '>')
                                {
                                    line += "{/}";
                                    if (nxtchar is not ' ') line += ' ';
                                }
                            }
                            else
                            {
                                if (!flgVar && (char.IsAsciiLetter(curchar) || curbyte == '_') && !flgFnOrProc && !quote && !flgHex) // variables may start with letter or underline
                                {
                                    if (!isRegister)
                                    {
                                        flgVar = true;
                                        if (switches.Pretty)
                                            line += "{=Magenta}";
                                    }
                                }
                                line += curchar;
                            }
                        }
                    }
                }
                else // now deal with tokens
                {
                    string keyword = getKeywordOrLineNumber(curbyte, ref i, ref nxtchar, State, switches);

                    if (switches.FlgList && keyword == "DEF") { State.Listme = nameMatch(i, State, switches); } // this automatically cancels at DEF if no match

                    if (keyword == "FOR" || keyword == "REPEAT" || keyword == "CASE") State.Indent++;
                    if ((keyword == "NEXT" || keyword == "UNTIL" || keyword == "ENDCASE") && State.Indent > 0) State.Indent--;
                    if (keyword == "THEN") startOfStatement = true; // THEN signals new statement
                    if (keyword == "FN" || keyword == "PROC") flgFnOrProc = true;

                    if (switches.Pretty)
                    {
                        if (curbyte == 0x8D) line += "{=DarkMagenta}" + keyword + "{/}"; // a GOTO linenumber // Grey?
                        else
                        {
                            line += "{=Blue}" + keyword + "{/}";
                            if (flgFnOrProc)
                                line += "{=Cyan}";
                        }

                        if (!line.EndsWith(' ') && nxtchar != ' ' && nxtchar != ':' && nxtchar != '('
                            && keyword != "PROC" && keyword != "FN" && !keyword.EndsWith('(') && !(keyword == "TO" && nxtchar == 'P'))
                            line += " ";
                    }
                    else
                    {
                        line += keyword;
                    }

                    linenospaces += keyword;
                    if (keyword == "REM")
                    {
                        rem = true;
                        if (switches.Pretty)
                            if (switches.FlgDark) line += "{=Yellow}"; else line += "{=DarkYellow}";
                    }
                }
                prevbyte = curbyte;
            }
            State.Ptr = State.Bound + 1;
            return line;
        }
        static string getKeywordOrLineNumber(byte curbyte, ref int ptr, ref char nxtchar, ParserState s, CommandSwitches switches)
        {
            try
            {
                if (switches.BasicV)
                {
                    if (curbyte > 197 && curbyte < 201)
                    {
                        int token = curbyte * 256 + s.Data[++ptr];
                        nxtchar = (char)s.Data[ptr + 1];
                        return Vtoken[token];
                    }
                }
                if (s.Z80) // RT Russell tokens that could not be included in tokentable
                {
                    switch (curbyte)
                    {
                        case 0xC6: return "SUM";
                        case 0xC7: return "WHILE";
                        case 0xC8: return "CASE";
                        case 0xC9: return "WHEN";
                        case 0xCA: return "OF";
                        case 0xCB: return "ENDCASE";
                        case 0xCC: return "OTHERWISE";
                        case 0xCD: return "ENDIF";
                        case 0xCE: return "ENDWHILE";
                    }
                }
                if (curbyte != 0x8D) return token[curbyte];
                int byte1 = s.Data[++ptr];
                int byte2 = s.Data[++ptr];
                int byte3 = s.Data[++ptr];
                byte1 <<= 2;              // Move byte1 up 2 bits
                int A = byte1 & 0xC0;     // Transfer to A and mask off lower 6 bits
                byte2 ^= A;               // EOR with byte2; this is now LSB
                byte1 <<= 2;              // Move byte1 up another 2 bits
                A = byte1 & 0xC0;
                byte3 ^= A;                // EOR with byte3; this is now MSB
                return (byte3 * 256 + byte2).ToString();
            }
            catch (KeyNotFoundException k)
            {
                return "[" + curbyte.ToString("X") + "]";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                ResetAndExit(switches);
                return "";
            }
        }
        static bool nameMatch(int ptr, ParserState s, CommandSwitches switches)
        {
            if (!switches.FlgList) return false;

            string procName = readProcFnName(++ptr, s, switches);
            if (procName == null) return false;

            for (int i = 0; i < s.DirectiveParams.Count; i++)
            {
                //Console.WriteLine($"Matching {procName} - {s.DirectiveParams[i]}");
                if (procName.Equals(s.DirectiveParams[i], StringComparison.InvariantCultureIgnoreCase)) return true;
            }
            return false;
        }
        static string readProcFnName(int ptr, ParserState s, CommandSwitches switches)
        {
            while (s.Data[ptr] == 32) ptr++; // this is correct: leave ptr pointing at first non-space char

            char dummy = ' ';
            string result = getKeywordOrLineNumber(s.Data[ptr], ref ptr, ref dummy, s, switches);

            if (result == null) return null;
            if (result != "FN" && result != "PROC") throw new Exception("Invalid DEF"); // return null;

            ptr++;
            result += readNextToken(ptr, s);
            //Console.WriteLine(result);
            return result;
        }
        static string readNextToken(int ptr, ParserState s)
        {
            string result = string.Empty;

            while (s.Data[ptr] == 32) ptr++;

            while (ptr <= s.Bound && (char.IsAsciiLetterOrDigit((char)s.Data[ptr]) || s.Data[ptr] == '_'))
            {
                result += (char)s.Data[ptr++];
            }
            return result;
        }
        static string readMnemonic(int ptr, ParserState s)
        {
            string result = string.Empty;

            while (ptr <= s.Bound && (char.IsAsciiLetterOrDigit((char)s.Data[ptr]) || ((char)s.Data[ptr] is '%' or '$' or '_'))) // if we capture MORE than a mnemonic, it is a variable, e.g. lda123, opt%
            {
                result += (char)s.Data[ptr++];
            }
            return result;
        }
        static string readRegister(int index, ParserState State)
        {
            int i = index;
            while (i < State.Data.Length && char.IsAsciiLetterOrDigit((char)State.Data[i]))
                i++;

            return Encoding.ASCII.GetString(State.Data, index, i - index);
        }
        static void PrettyPrint(string msg, ParserState state, CommandSwitches switches)
        {
            string[] ss = msg.Split('{', '}');
            foreach (var s in ss)
            {
                if (s.Equals("/"))
                {
                    //Console.ResetColor();
                    Console.BackgroundColor = switches.BackColor;
                    Console.ForegroundColor = switches.ForeColor;
                }
                else
                {
                    try
                    {
                        if (s.StartsWith('=') && Enum.TryParse(s.Substring(1), out ConsoleColor c))
                            Console.ForegroundColor = c;
                        else
                            Console.Write(s);
                    }
                    catch (Exception e)
                    {
                        Console.Write(s);
                    }
                }
            }
            Console.WriteLine("");
        }
        static bool isEndOfProc(ParserState s, CommandSwitches switches) // s is a CLONE so can use freely
        {
            string templine = getNextLine(s, switches);

            if (templine == null) return true; // End of program

            templine = Regex.Replace(templine, @"\{.*?\}", "");
            templine = templine.Replace(" ", "").ToUpper();
            templine = templine.Replace(":", "");
            templine = Regex.Replace(templine, @"REM.*$", "", RegexOptions.IgnoreCase);

            if (templine.Length == 0) return isEndOfProc(s, switches);

            return templine.StartsWith("DEF");
        }
        static string getNextLine(ParserState s, CommandSwitches switches)
        {
            // End of program?
            if (!s.Z80 && ((switches.BasicV && s.Data[s.Ptr + 1] == 255) || s.Data[s.Ptr + 1] > 127)) return null;
            if (s.Z80 && s.Data[s.Ptr + 2] == 255 && s.Data[s.Ptr + 3] == 255) return null;

            // compute Bound for the clone (saved in s.Bound)
            int dummy = GetLineNumber(s, switches);

            getLineBody(s, switches, out string line);

            return line;
        }
        static void getDirectiveParams(string[] args, ParserState s)
        {
            s.DirectiveParams = new();

            int i = 0;
            while (i < args.Length &&
                    !args[i].Equals("IF", StringComparison.InvariantCultureIgnoreCase) &&
                    !args[i].Equals("IFX", StringComparison.InvariantCultureIgnoreCase) &&
                    !args[i].Equals("LIST", StringComparison.InvariantCultureIgnoreCase))
            { i++; }

            // Move past the directive keyword
            if (i < args.Length) i++;

            for (int j = i; j < args.Length; j++)
                s.DirectiveParams.Add(args[j]);
        }
        /**** Utility functions ******/
        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            for (int i = 0; i < Console.WindowWidth; i++)
                Console.Write(" ");
            Console.SetCursorPosition(0, currentLineCursor);
        }
        static bool IsNumeric(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (!"0123456789,".Contains(s.Substring(i, 1))) return false;
            }
            return true;
        }
        static HashSet<string> LoadMnemonicTable(string resourceName)
        {
            string fileContent = GetEmbeddedResourceContent(resourceName);

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] mnemonics = fileContent.Split("\r\n");

            foreach (string mnemonic in mnemonics)
            {
                if (!string.IsNullOrEmpty(mnemonic))
                    set.Add(mnemonic.Trim());
            }

            return set;
        }
        static string GetEmbeddedResourceContent(string resourceName)
        {
            var asm = Assembly.GetExecutingAssembly();

            using Stream? stream = asm.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        static void DumpResourceNames()
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (var name in asm.GetManifestResourceNames())
                Console.WriteLine(name);
        }

        static void help()
        {
            string vs = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion; //string vs = Assembly.Application.FileVersion; // requires ref to System.Windows.Forms
            if (vs == null || vs == string.Empty) vs = "1.1.0";

            Console.WriteLine($"\nBasList vs {vs} (C) Andrew Rowland 2022-26");
            Console.WriteLine("\nLists a BBC BASIC program file\n");
            Console.WriteLine("BasList [/file=]filename ([[from line] [to line]) | [line,line]]) [Options] ([IF ...] | [IFX ...] | [LIST ...])");
            Console.WriteLine("BasList [/file=]filename [/V] [/addnumbers] [/align] [/indent] [/nonumbers] [/nospaces] [/bare] [/pause] [/prettyprint]");
            Console.WriteLine("BasList [/file=]filename [/mode=(dark | light | none)]");
            Console.WriteLine("BasList /? - help\n");
            Console.WriteLine("  /file=       Filename to follow without spaces. Quote if contains spaces.");
            Console.WriteLine("               '/file=' may be omitted if filename is first item");
            Console.WriteLine("  /V           Allow BASIC V keywords");
            Console.WriteLine("  /addnumbers  Supply missing line numbers (Z80 only)");
            Console.WriteLine("  /align       Right-align line numbers");
            Console.WriteLine("  /indent      Indent listing of loops (unless Nospaces specified)");
            Console.WriteLine("  /nonumbers   Omits line numbers");
            Console.WriteLine("  /nospaces    Omits spaces and indent after line numbers");
            Console.WriteLine("  /bare        Omits additional messages (cancels pause)");
            Console.WriteLine("  /pause       Pause at bottom of each screenful");
            Console.WriteLine("  /prettyprint Adds spaces and syntax colouring");
            Console.WriteLine("  /dark        Dark mode – black background (default)");
            Console.WriteLine("  /light       Light mode – white background");
            Console.WriteLine("\nE.g.");
            Console.WriteLine("  BasList program ,200        - List up to line 200");
            Console.WriteLine("  BasList program 1000,       - List from line 1000");
            Console.WriteLine("  BasList program 200,1000    - List from line 200 to 1000");
            Console.WriteLine("  BasList program 200 1000    - List from line 200 to 1000");
            Console.WriteLine("  BasList program IF PRINTTAB - List only lines containing PRINTTAB or PRINT TAB, case insensitive");
            Console.WriteLine("  BasList program IFX         - IF, but respecting spaces and case");
            Console.WriteLine("  BasList program LIST FNinp  - List named function/procedure");
            Console.WriteLine("\nOptions may be specified in any order, may start with / or - and can be abbreviated.");
            Console.WriteLine("Parameters containing spaces must be enclosed by double quotes.");
            Console.WriteLine("Any IF or IFX clause must be at the end. Multiple matches may be entered\nand BasList will list any line containing at least one of them.");
            Console.WriteLine("\nFor further help, see ReadMe.");
        }
        static void ResetAndExit(CommandSwitches s)
        {
            Console.ForegroundColor = s.ForeColor;
            Console.BackgroundColor = s.BackColor;
            Environment.Exit(0);
        }
    }
}
