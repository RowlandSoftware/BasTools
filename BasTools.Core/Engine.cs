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
    //using System.Windows.Forms;
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

    //***************** ParserState *****************
    class ParserState
    {
        public byte[] Data;
        public int Ptr;
        public bool Z80;
        public int LineCount;
        public bool InAsm;

        public List<string> DirectiveParams = new();

        public int DataSize;
        public ParserState()
        {
            Data = Array.Empty<byte>();
            Z80 = false;
            Ptr = 0;
            LineCount = 0;
            DataSize = 0;
            InAsm = false;
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
        public static string RemText => "{=remtext}";
        public static string AssemblerComment => "{=assemcomment}";
        public static string StarCommand => "{=starcommand}";
        public static string EmbeddedData => "{=embeddeddata}";
        public static string ProcFunction => "{=proc_fn}";
        public static string Label => "{=label}";
        public static string Register => "{=register}";
        public static string Mnemonic => "{=mnemonic}";
        public static string LineNumber => "{=linenumber}";
        public static string Operator => "{=operator}";
        public static string Reset => "{/}";
    }

    public record Listing(
    List<ProcessedLine> ProgramLines,
    List<Token> Tokens
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
    public record LineRecord(
        int linenumber,
        byte[] lineContent
    );

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

        public bool Process(string fn, ref bool flgZ80, bool BasicV, Listing listing)
        {
            listing = new(new List<ProcessedLine>(), new List<Token>());
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
            flgZ80 = State.Z80;

            // Split the file into lines
            foreach (LineRecord progline in ParseLines(State.Data, flgZ80, BasicV))
            {
                ProcessedLine thisline = new();
                thisline.LineNumber = progline.linenumber;
                thisline.TokenisedLine = progline.lineContent.ToArray(); // Because we need to _copy_ the array

                // Line contents

                processLineBody(State, thisline.TokenisedLine, thisline, BasicV);

                listing.ProgramLines.Add(thisline);

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
            }

            return true;
        } // End Process()
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
                State.DataSize = size;
            }
            return true;
        }
        IEnumerable<LineRecord> ParseLines(byte[] data, bool flgZ80, bool BasicV)
        {
            int ptr = flgZ80 ? 0 : 1;   // skip initial CR for 6502
            int LineCount = 0;

            while (ptr < data.Length)
            {
                bool endOfProg = false;
                if (!flgZ80 && ((BasicV && data[ptr] == 255) || data[ptr] > 127)) endOfProg = true; // End of program marker (Acorn)
                if (flgZ80 && data[ptr + 1] == 255 && data[ptr + 2] == 255) endOfProg = true; // End of program marker (Z80)
                if (endOfProg) break;

                if (ptr + 3 > data.Length)
                    throw new BasToolsException("Unexpected end of file");

                byte length;
                ushort lineNumber;
                if (flgZ80)
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
        private void processLineBody(ParserState State, byte[] tokenisedLine, ProcessedLine returnObject, bool BasicV)
        {
            string plainline = string.Empty;
            string linenospaces = string.Empty;
            string taggedline = string.Empty;
            //string prettyLine = string.Empty;

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
                if (curchar is '+' or '-' or '/' or '*' or '=' or '<' or '>' or '^' && !rem && !quote) // not good with unary minus
                {
                    char p = (char)prevbyte;
                    if (!IsExponentSign(p, curchar))       // trying to deal with e.g. 1E-5
                    {
                        taggedline += SemanticTags.Operator;

                        while (i < tokenisedLine.Length - 1 && curchar is '+' or '-' or '/' or '*' or '=' or '<' or '>' or '^')
                        {
                            addtoall(curchar, ref plainline, ref linenospaces, ref taggedline);
                            curchar = (char)State.Data[++i];
                        }
                        taggedline += SemanticTags.Reset;
                        i--;
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

                    /*if (asmComment)
                    {
                        // In assembler comment: just copy the character and skip all other logic
                        plainline += curchar;
                        linenospaces += curchar;
                        taggedline += curchar;
                        prevbyte = curbyte;
                        continue;
                    }*/

                    if (!asmComment)
                    {
                        string mnemonic = readMnemonic(tokenisedLine, i);
                        if (mnemonic != string.Empty)
                        {
                            bool isMnemonic;
                            if (BasicV)
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
                    if (BasicV)
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

                        if (isRegister)
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
                    if (flgVar && !char.IsAsciiLetterOrDigit(curchar) && curchar is not '%' and not '$' and not '_')
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
                    string keyword = getKeywordOrLineNumber(tokenisedLine, curbyte, ref i, ref nxtchar, BasicV, State);

                    if (keyword == "THEN") startOfStatement = true; // THEN signals new statement
                    if (keyword == "FN" || keyword == "PROC") flgFnOrProc = true;

                    if (curbyte == 0x8D)
                        taggedline += SemanticTags.LineNumber + keyword + SemanticTags.Reset; // a GOTO linenumber
                    else
                    {
                        string tag = SemanticTags.Keyword;
                        switch (keyword)
                        {
                            case "IF":
                            case "FOR":
                            case "REPEAT":
                            case "WHILE":
                            case "CASE":
                                tag = SemanticTags.IndentingKeyword;
                                break;
                            case "ENDIF":
                            case "NEXT":
                            case "UNTIL":
                            case "ENDWHILE":
                            case "ENDCASE":
                                tag = SemanticTags.OutdentingKeyword;
                                break;
                            case "THEN":
                            case "ELSE":
                            case "OTHERWISE":
                                tag = SemanticTags.InOutKeyword;
                                break;
                        }
                        taggedline += tag + keyword + SemanticTags.Reset;
                        if (flgFnOrProc)
                            taggedline += SemanticTags.ProcFunction;

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
        private string getKeywordOrLineNumber(byte[] tokenisedLine, byte curbyte, ref int ptr, ref char nxtchar, bool BasicV, ParserState s)
        {
            try
            {
                if (BasicV)
                {
                    if (curbyte > 197 && curbyte < 201)
                    {
                        int token = curbyte * 256 + tokenisedLine[++ptr];
                        nxtchar = (ptr == tokenisedLine.Length - 1) ? '\0' : (char)tokenisedLine[ptr+1];
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
        static void addtoall(char curchar,
            ref string plainline,
            ref string linenospaces,
            ref string taggedline)
        {
            plainline += curchar;
            linenospaces += curchar;
            taggedline += curchar;
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
