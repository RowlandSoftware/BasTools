namespace BasTools.Core
{
    using Microsoft.Win32;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Reflection.PortableExecutable;
    using System.Text;
    using System.Text.RegularExpressions;
    using static System.Runtime.InteropServices.JavaScript.JSType;

    //***************** Exceptions *****************
    public class BasToolsException : Exception
    {
        public BasToolsException() { }

        public BasToolsException(string message)
            : base(message) { }

        public BasToolsException(string message, Exception inner)
            : base(message, inner) { }
    }

    //***************** ProgInfo *****************
    public class ProgInfo
    {
        public bool Z80;
        public bool BasicV;
        public int NumberOfLines;
        public int LengthInBytes;
        public string Filename;
        public string BasicDialect =>
            Z80 ? "Z80 Basic"
                : BasicV ? "Acorn Basic V"
                    : "Acorn Basic I–IV";
        public ProgInfo(bool z80, bool basicV, string filename)
        {
            Z80 = z80;
            BasicV = basicV;
            NumberOfLines = 0;
            LengthInBytes = 0;
            Filename = filename;
        }
    }

    //***************** SemanticTags *****************
    public static class SemanticTags
    {
        // These are the literal tags you insert into the output
        public static string Keyword => "{=keyword}";
        public static string IndentingKeyword => "{=indentingkeyword}";
        public static string OutdentingKeyword => "{=outdentingkeyword}";
        public static string InOutKeyword => "{=inout_keyword}";
        public static string StringLiteral => "{=string}";
        public static string Variable => "{=var}";
        public static string StaticInteger => "{=staticint}";
        public static string RemText => "{=remtext}";
        public static string AssemblerComment => "{=assemcomment}";
        public static string StarCommand => "{=starcommand}";
        public static string EmbeddedData => "{=embeddeddata}";
        public static string Proc => "{=proc}";
        public static string Function => "{=fn}";
        public static string Label => "{=label}";
        public static string Register => "{=register}";
        public static string Mnemonic => "{=mnemonic}";
        public static string LineNumber => "{=linenumber}";
        public static string Operator => "{=operator}";
        public static string Reset => "{/}";
    }

    //***************** Listing Classes and Records *****************
    public record Listing(
    List<ProcessedLine> ProgramLines //, List<Token> Tokens
    );
    public record class ProcessedLine
    {
        public int LineNumber { get; set; }
        public byte[] TokenisedLine { get; set; } = Array.Empty<byte>();
        public string NoSpacesLine { get; set; } = "";
        public string PlainDetokenisedLine { get; set; } = "";
        public string TaggedLine { get; set; } = "";
    }
    public record Token(
        string Value,
        string SemanticTag
    );
    public record FormattedListing(
    List<FormattedLine> FormattedLines
    );
    public record class FormattedLine
    {
        public int LineNumber { get; set; }
        public string? FormattedLineNumber { get; set; }
        public int IndentLevel { get; set; }
        public string LineLineOrSegment { get; set; } = "";
        public string TaggedLineLineOrSegment { get; set; } = "";
    }
    public record LineRecord(
        int linenumber,
        byte[] lineContent
    );
    //***************** ParserState *****************
    class ParserState
    {
        public byte[] Data;
        public int Ptr;
        public bool Z80;
        public int LineCount;
        public bool InAsm;

        public List<string> DirectiveParams = new();
        public ParserState()
        {
            Data = Array.Empty<byte>();
            Z80 = false;
            Ptr = 0;
            LineCount = 0;
            InAsm = false;
        }
    }

    //***************** FormatterState *****************
    class FormatterState
    {
        public bool Z80;
        public int LineCount;
        private int _indent;
        public int PendingIndent;
        public bool fMultiLineIf;
        public FormatterState()
        {
            Z80 = false;
            LineCount = 0;
            Indent = 0;
            PendingIndent = 0;
            fMultiLineIf = false;
        }
        public FormatterState(FormatterState other)
        {
            Z80 = other.Z80;
            LineCount = other.LineCount;
            Indent = other.Indent;
            PendingIndent = other.PendingIndent;
            fMultiLineIf |= other.fMultiLineIf;
        }
        public int Indent
        {
            get => _indent;
            set => _indent = value < 0 ? 0 : value;
        }
    }

    //***************** FormattingOptions *****************
    public class FormattingOptions
    {
        public bool FlgAddNums;
        public bool FlgIndent;
        public bool FlgEmphDefs;
        public bool Align;
        public bool NoSpaces;
        public bool Bare;
        public bool BreakApart;
        public FormattingOptions()
        {
            FlgAddNums = false;
            FlgIndent = false;
            FlgEmphDefs = false;
            Align = false;
            NoSpaces = false;
            Bare = false;
            BreakApart = false;
        }
    }

    //***************** The Engine *****************
    public class BasToolsEngine
    {
        // These are FIELDS — they persist for the lifetime of the engine
        private readonly Dictionary<int, string> token = new();
        private readonly Dictionary<int, string> Vtoken = new();
        private readonly HashSet<string> Mnemonics6502;
        private readonly HashSet<string> ArmMnemonics;
        private readonly HashSet<string> ArmRegisters;

        public BasToolsEngine()
        {
            // Initialise the fields
            Mnemonics6502 = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
            "ADC","AND","ASL","BCC","BCS","BEQ","BIT","BMI","BNE","BPL","BRA","BRK","BVC","BVS",
            "CLC","CLD","CLI","CLR","CLV","CMP","CPX","CPY","DEC","DEX","DEY","EOR","INC","INX",
            "INY","JMP","JSR","LDA","LDX","LDY","LSR","NOP","OPT","ORA","PHA","PHP","PHX","PHY",
            "PLA","PLP","PLX","PLY","ROL","ROR","RTI","RTS","SBC","SEC","SED","SEI","STA","STX",
            "STY","STZ","TAX","TAY","TRB","TSB","TSX","TXA","TXS","TYA"
            };

            ArmMnemonics = LoadMnemonicTable("BasTools.Core.ArmMnemonics.txt");

            ArmRegisters = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "R0","R1","R2","R3","R4","R5","R6","R7",
                "R8","R9","R10","R11","R12","R13","R14","R15",
                "SP","LR","PC"
            };
            readTokenTable(token, "BasTools.Core.TokenTable.txt");
            readTokenTable(Vtoken, "BasTools.Core.VTokenTable.txt");
        }

        public bool ProcessRawProgram(string fn, Listing listing, ProgInfo progInfo)
        {
            ParserState State = new();
            bool result = LoadFile(fn, State);
            if (!result) return false;

            // determine file type (Acorn or Z80)
            int ll = State.Data[3];
            if (State.Data[0] == 13 && State.Data[ll] == 13)
                State.Z80 = false;
            else
            {
                ll = State.Data[0];
                if (State.Data[ll - 1] == 13)
                    State.Z80 = true;
                else
                {
                    throw new BasToolsException("\'" + fn + "\' is not a BASIC program");
                }
            }
            progInfo.Z80 = State.Z80;

            // Split the file into lines
            foreach (LineRecord progline in ParseLines(State.Data, progInfo))
            {
                ProcessedLine thisline = new();
                thisline.LineNumber = progline.linenumber;
                thisline.TokenisedLine = progline.lineContent.ToArray(); // Because we need to _copy_ the array

                // Line contents

                processLineBody(State, thisline.TokenisedLine, thisline, progInfo);

                listing.ProgramLines.Add(thisline);
#if DEBUG
                /*Console.WriteLine($"{progline.linenumber}" + ' ' + thisline.PlainDetokenisedLine);
                Console.WriteLine($"{progline.linenumber}" + ' ' + thisline.NoSpacesLine);
                Console.WriteLine($"{progline.linenumber}" + ' ' + thisline.TaggedLine);
                string untaggedline = Regex.Replace(thisline.TaggedLine, @"\{.*?\}", "");
                if (untaggedline != thisline.PlainDetokenisedLine)
                {
                    Console.WriteLine("Mismatch");
                    Console.WriteLine(untaggedline);
                }
                Console.WriteLine();*/
#endif
            }
            // update program metadata
            progInfo.LengthInBytes = State.Data.Count();
            progInfo.NumberOfLines = listing.ProgramLines.Count;

            return true;

        } // End ProcessRawProgram()
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
                        if (tag == SemanticTags.OutdentingKeyword) state.PendingIndent--;
                        if (tag == SemanticTags.InOutKeyword && switches.BreakApart && false) // && NExt line not WHEN && not OTHERWISE && already indented
                        {
                            state.Indent--;
                            state.PendingIndent++;
                        }
                        char nxtchar = i < tokens.Count - 1 ? tokens[i + 1].value[0] : '\0';
                        char lastchar = i > 0 && tokens[i - 1].value.Length > 0 ? tokens[i - 1].value[^1] : '\0';
                        bool nextStartsWithSpace = nxtchar == ' ';
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
                                value += "@";
                                //if (true) //(value != "PROC" && value != "FN" && !value.EndsWith('(') && !(value == "TO" && nxtchar == 'P'))
                                //if (!lastEndsWithSpace && !nextStartsWithSpace && nxtchar != ':' && nxtchar != '(')
                                //    value += "@";
                            }                        
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
        private bool LoadFile(string fn, ParserState State)
        {
            using (FileStream stream = new(fn, FileMode.Open, FileAccess.Read))
            {
                int size = (int)stream.Length;
                State.Data = new byte[size];
                int read = stream.Read(State.Data, 0, size);
                if (read != size)
                {
                    throw new IOException("File read incomplete.");
                }
                //State.DataSize = size;
            }
            return true;
        }
        IEnumerable<LineRecord> ParseLines(byte[] data, ProgInfo progInfo)
        {
            int ptr = progInfo.Z80 ? 0 : 1;   // skip initial CR for 6502
            int LineCount = 0;

            while (ptr < data.Length)
            {
                bool endOfProg = false;
                if (!progInfo.Z80 && ((progInfo.BasicV && data[ptr] == 255) || data[ptr] > 127)) endOfProg = true; // End of program marker (Acorn)
                if (progInfo.Z80 && data[ptr + 1] == 255 && data[ptr + 2] == 255) endOfProg = true; // End of program marker (Z80)
                if (endOfProg) break;

                if (ptr + 3 > data.Length)
                    throw new BasToolsException("Unexpected end of file");

                byte length;
                ushort lineNumber;
                if (progInfo.Z80)
                {
                    lineNumber = (ushort)(data[ptr + 1] | (data[ptr + 2] << 8));
                    length = data[ptr];
                }
                else
                {
                    lineNumber = (ushort)(data[ptr] << 8 | (data[ptr + 1]));
                    length = data[ptr + 2];
                }

                LineCount++;
                if (data[ptr + length - 1] != 13)
                {
                    string ordinal = "th";
                    if (LineCount.ToString().EndsWith('1') && LineCount != 11) ordinal = "st";
                    if (LineCount.ToString().EndsWith('2') && LineCount != 12) ordinal = "nd";
                    if (LineCount.ToString().EndsWith('3') && LineCount != 13) ordinal = "rd";

                    throw new BasToolsException(
                        "Line structure error at &" + ptr.ToString("X4") +
                        $" after {LineCount}{ordinal} line of program");
                }

                var content = data.AsSpan(ptr + 3, length - 4); // lose line number & ll bytes and final CR
                byte[] slice = content.ToArray();

                ptr += length;

                yield return new LineRecord(lineNumber, slice);
            }
        }
        private void processLineBody(ParserState State, byte[] tokenisedLine, ProcessedLine returnObject, ProgInfo progInfo)
        {
            string plainline = string.Empty;
            string linenospaces = string.Empty;
            string taggedline = string.Empty;

            bool quote = false;
            bool rem = false;
            bool flgFnOrProc = false;
            bool flgVar = false;
            bool flgHex = false;
            bool startOfStatement = true;
            bool asmComment = false;
            int prevbyte = 0;

            for (int i = 0; i <= tokenisedLine.Length - 1; i++)
            {
                byte curbyte = tokenisedLine[i];
                char curchar = (char)curbyte;
                char nxtchar = (i == tokenisedLine.Length - 1) ? '\0' : (char)tokenisedLine[i + 1];

                // 1. Stuff we do BEFORE adding curchar to the listings

                // 1a) Setting flags
                if (startOfStatement && curbyte == '[')
                    State.InAsm = true;
                
                if (startOfStatement && curbyte == ']')
                    State.InAsm = false;

                // deal with quotes
                if (curbyte == 34 && !rem)
                {
                    // toggle quote mode
                    if (!quote)
                        taggedline += SemanticTags.StringLiteral + '"';
                    else
                        taggedline += '"' + SemanticTags.Reset;
                    plainline += '"';
                    linenospaces += '"';

                    quote = !quote;
                    continue;
                }

                if (curbyte == '*' && startOfStatement) // star command
                {
                    taggedline += SemanticTags.StarCommand;
                    rem = true;                         // rem ensures no further processing
                    startOfStatement = false;
                }

                // 1b) Wrapping semantic tokens

                // Mathematical operators
                if (IsOperator(curchar) && !rem && !quote) // not good with unary minus??
                {
                    char p = (char)prevbyte;
                    if (!IsExponentSign(p, curchar))       // trying to deal with e.g. 1E-5
                    {
                        taggedline += SemanticTags.Operator;

                        while (IsOperator(curchar) && i < tokenisedLine.Length - 1)
                        {
                            addtoall(curchar, ref plainline, ref linenospaces, ref taggedline);
                            curchar = (char)tokenisedLine[++i];
                        }
                        i--;
                        taggedline += SemanticTags.Reset;
                        continue;
                    }
                }

                #region Assembler
                //if (State.InAsm) Console.WriteLine("In asm");
                if (State.InAsm && !quote)
                {
                    if (!asmComment && curchar is '\\' or ';' or '\'')
                    {
                        asmComment = true;
                        rem = true;
                        startOfStatement = false;
                        taggedline += SemanticTags.AssemblerComment;
                    }
                    else if (asmComment && curchar == ':')
                    {
                        asmComment = false;
                        startOfStatement = true;
                        taggedline += SemanticTags.Reset;
                        // fall through so ':' still acts as statement separator
                    }

                    if (!asmComment)
                    {
                        string mnemonic = readMnemonic(tokenisedLine, i);
                        if (mnemonic != string.Empty)
                        {
                            bool isMnemonic;
                            if (progInfo.BasicV)
                            {
                                isMnemonic = ArmMnemonics.Contains(mnemonic);
                            }
                            else
                            {
                                isMnemonic = Mnemonics6502.Contains(mnemonic) || Regex.IsMatch(mnemonic, "EQU[BDSW]", RegexOptions.IgnoreCase);
                            }

                            if (isMnemonic)
                            {
                                taggedline += SemanticTags.Mnemonic + mnemonic.ToUpper() + SemanticTags.Reset;
                                plainline += mnemonic;
                                linenospaces += mnemonic;

                                i += mnemonic.Length - 1;
                                curbyte = tokenisedLine[i];
                                curchar = (char)curbyte;
                                nxtchar = (i == tokenisedLine.Length - 1) ? '\0' : (char)tokenisedLine[i + 1];
                                prevbyte = (byte)plainline[^1];
                                continue;
                            }
                        }
                    }
                    if (progInfo.BasicV)
                    {
                        // ARM: Detect tokens like R0, R12, SP, LR, PC
                        char uchar = char.ToUpperInvariant(curchar);
                        char unext = char.ToUpperInvariant(nxtchar);
                        if (uchar is 'R' && char.IsDigit(nxtchar) || (curchar is 'S' or 'L' or 'P' && unext is 'P' or 'R' or 'C'))
                        {
                            // R0–R15, SP, LR, PC
                            string reg = readRegister(tokenisedLine, i);   // similar to readMnemonic
                            if (ArmRegisters.Contains(reg))
                            {
                                taggedline += SemanticTags.Register + reg.ToUpper() + SemanticTags.Reset;
                                plainline += reg;
                                linenospaces += reg;

                                i += reg.Length - 1;   // advance past the whole register
                                curbyte = tokenisedLine[i];
                                curchar = (char)curbyte;
                                nxtchar = (i == tokenisedLine.Length - 1) ? '\0' : (char)tokenisedLine[i + 1];
                                prevbyte = (byte)plainline[^1];

                                continue;
                            }
                        }
                    }
                    else // 6502 registers
                    { // 6502: deal with ROL A, STA (&70),Y, LDA &80,X
                        bool isIndexRegister = (curchar is 'X' or 'x' or 'Y' or 'y') && prevbyte == ',';   // or previous non-space char if you want to be stricter
                        bool isAccumulator = char.ToUpperInvariant(curchar) == 'A' && !char.IsAsciiLetterOrDigit((char)prevbyte);
                        bool isRegister = (isIndexRegister || isAccumulator);

                        if (isRegister && !asmComment)
                        {
                            curchar = char.ToUpperInvariant(curchar);
                            taggedline += SemanticTags.Register + char.ToUpper(curchar) + SemanticTags.Reset;
                            plainline += curchar;
                            linenospaces += curchar;

                            continue;
                        }
                    }
                } //end of assembler section
                #endregion

                // Variables
                if (!flgVar && !rem && (char.IsAsciiLetter(curchar) || curbyte == '_') && !flgFnOrProc && !quote && !flgHex) // variables may start with letter or underline
                {
                    flgVar = true;
                    taggedline += SemanticTags.Variable;
                }
                
                // *** 2. Printable characters - NOW we add to listings ***
                if ((curbyte < 127 && curbyte > 31) || rem || quote) // Anything not a BASIC token
                {
                    if (curbyte > 32 || rem || quote)
                        linenospaces += curchar;
                    plainline += curchar;
                    taggedline += curchar;
                    
                    // 3. Things we do AFTER adding the character to the listings

                    if (flgFnOrProc && !char.IsAsciiLetterOrDigit(curchar) && curbyte != '_')
                    {
                        flgFnOrProc = false;
                        taggedline += SemanticTags.Reset;
                    }
                    if (flgVar && !(char.IsAsciiLetterOrDigit(nxtchar) || nxtchar is '%' or '$' or '_'))
                    {
                        flgVar = false;
                        taggedline += SemanticTags.Reset;
                    }

                    if (curchar is ':' or ']' && !rem && !quote)
                        startOfStatement = true;  // a colon outside of quotes or REM is new statement; so is assembler delimiter
                    else if (curchar != ' ')
                        startOfStatement = false; // anything else isn't

                    if (flgHex && !char.IsAsciiHexDigit(curchar)) { flgHex = false; }

                    if (curbyte == '&') { flgHex = true; }
                }
                else
                // 4. Now deal with tokens
                {
                    string keyword = getKeywordOrLineNumber(tokenisedLine, curbyte, ref i, ref nxtchar, progInfo, State);

                    if (keyword == "THEN") startOfStatement = true; // THEN signals new statement
                    if (keyword == "FN" || keyword == "PROC") flgFnOrProc = true;

                    if (curbyte == 0x8D)
                        taggedline += SemanticTags.LineNumber + keyword + SemanticTags.Reset; // a GOTO linenumber
                    else
                    {
                        string tag = SemanticTags.Keyword;
                        switch (keyword)
                        {
                            // No If ... Then ... Else; handles in code because depends on whether in multilineIf or not (unless /breakapart)
                            case "FOR":
                            case "REPEAT":
                            case "WHILE":
                            case "CASE":
                                tag = SemanticTags.IndentingKeyword;
                                break;
                            case "NEXT":
                            case "UNTIL":
                            case "ENDWHILE":
                            case "ENDCASE":
                                tag = SemanticTags.OutdentingKeyword;
                                break;
                            // Ones we only want to affect indent when /breakapart used
                            // These cancel 1 indent for the line they're on, and indent following lines
                            case "OTHERWISE":
                            case "WHEN":
                                tag = SemanticTags.InOutKeyword;
                                break;
                        }
                        taggedline += tag + keyword + SemanticTags.Reset;

                        if (keyword == "PROC")
                            taggedline += SemanticTags.Proc;
                        if (keyword == "FN")
                            taggedline += SemanticTags.Function;

                        if (keyword == "DATA")
                        {
                            startOfStatement = false; // What follows not new statement
                            rem = true;
                            taggedline += SemanticTags.EmbeddedData;
                        }

                        plainline += keyword;
                    }

                    linenospaces += keyword;

                    if (keyword == "REM")
                    {
                        rem = true;
                        startOfStatement = false;
                        taggedline += SemanticTags.RemText;
                    }
                }
                prevbyte = curbyte;
            }
            if (rem || flgFnOrProc || flgVar || asmComment) taggedline += SemanticTags.Reset; // close hanging tags at end of line

            returnObject.PlainDetokenisedLine = plainline;
            returnObject.TaggedLine = taggedline;
            returnObject.NoSpacesLine = linenospaces;

            return;
        }
        private string getKeywordOrLineNumber(byte[] tokenisedLine, byte curbyte, ref int ptr, ref char nxtchar, ProgInfo progInfo, ParserState s)
        {
            try
            {
                if (curbyte > 197 && curbyte < 201)
                {
                    progInfo.BasicV = true;
                    int token = curbyte * 256 + tokenisedLine[++ptr];
                    nxtchar = (ptr == tokenisedLine.Length - 1) ? '\0' : (char)tokenisedLine[ptr+1];
                    return Vtoken[token];
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

                int byte1 = tokenisedLine[++ptr];
                int byte2 = tokenisedLine[++ptr];
                int byte3 = tokenisedLine[++ptr];
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
                throw new BasToolsException(e.Message);
            }
        }
        static void addtoall(char addition,
            ref string plainline,
            ref string linenospaces,
            ref string taggedline)
        {
            plainline += addition;
            linenospaces += addition;
            taggedline += addition;
        }
        static string readMnemonic(byte[] tokenisedLine, int ptr)
        {
            string result = string.Empty;

            while (ptr <= tokenisedLine.Length-1 && (char.IsAsciiLetterOrDigit((char)tokenisedLine[ptr]) || ((char)tokenisedLine[ptr] is '%' or '$' or '_'))) // if we capture MORE than a mnemonic, it is a variable, e.g. lda123, opt%
            {
                result += (char)tokenisedLine[ptr++];
            }
            return result;
        }
        static string readRegister(byte[] tokenisedLine, int index)
        {
            int i = index;
            while (i < tokenisedLine.Length-1 && char.IsAsciiLetterOrDigit((char)tokenisedLine[i]))
                i++;

            return Encoding.ASCII.GetString(tokenisedLine, index, i - index);
        }
        static bool IsExponentSign(char prev, char curr)
        {
            return (curr == '+' || curr == '-') && char.ToUpper(prev) == 'E';
        }
        static bool IsOperator(char c) => c is '+' or '-' or '/' or '*' or '=' or '<' or '>' or '^';
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
        private void readTokenTable(Dictionary<int, string> toktable, string filename)
        {
            string table = GetEmbeddedResourceContent(filename);
            string[] tokenlist = table.Split("\r\n");

            foreach (string tline in tokenlist)
            {
                if (string.IsNullOrWhiteSpace(tline))
                    continue;

                string[] temp = tline.Split('\t');

                // BASIC token table: name \t byte
                if (temp.Length == 2)
                {
                    int key = byte.Parse(temp[1]);
                    toktable.Add(key, temp[0]);
                }
                // BASIC V token table: name \t hi \t lo
                else if (temp.Length == 3)
                {
                    int key = (byte.Parse(temp[1]) << 8) | byte.Parse(temp[2]);
                    toktable.Add(key, temp[0]);
                }
                else
                {
                    throw new Exception($"Unexpected token table format in line: {tline}");
                }
            }
        }
        public static IEnumerable<(string value, string? tag, bool isLast)> WalkTagged(string? line)
        {
            if (line == null) yield break;
            int i = 0;

            // First, collect all items into a temporary list
            var items = new List<(string value, string? tag)>();

            while (i < line.Length)
            {
                // Tagged token?
                if (line[i] == '{' && i + 2 < line.Length && line[i + 1] == '=')
                {
                    int tagStart = i;
                    int tagEnd = line.IndexOf('}', tagStart);
                    if (tagEnd < 0) break;

                    string tag = line.Substring(tagStart, tagEnd - tagStart + 1);

                    int valueStart = tagEnd + 1;
                    int close = line.IndexOf("{/}", valueStart);
                    if (close < 0) break;

                    string value = line.Substring(valueStart, close - valueStart);

                    items.Add((value, tag));

                    i = close + 3;
                }
                else
                {
                    // Untagged text — collect until next '{'
                    int start = i;
                    int next = line.IndexOf('{', i);
                    if (next < 0) next = line.Length;

                    string text = line.Substring(start, next - start);
                    items.Add((text, null));

                    i = next;
                }
            }

            // Now yield with correct isLast flag
            for (int n = 0; n < items.Count; n++)
            {
                var (value, tag) = items[n];
                bool isLast = (n == items.Count - 1);
                yield return (value, tag, isLast);
            }
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
    }
}
