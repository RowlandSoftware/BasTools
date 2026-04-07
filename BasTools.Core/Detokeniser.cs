using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;

namespace BasTools.Core
{
    public partial class BasToolsEngine
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
        internal bool ProcessRawProgram(string fn, Listing listing, ProgInfo progInfo)
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
            foreach (LineRecord progline in ParseLines(State.Data, progInfo)) // LineRecord is a temporary structure to hold int linenumber, byte[] lineContent here only
            {
                ProgramLine thisline = new();                                 // This is where our ProgramLine object is created, to be added to the List listing
                thisline.LineNumber = progline.linenumber;
                thisline.TokenisedLine = progline.lineContent.ToArray();      // Because we need to _copy_ the array

                // Line contents

                processLineBody(State, thisline.TokenisedLine, thisline, progInfo);

                listing.Lines.Add(thisline);

            } // end foreach

            // update program metadata
            progInfo.LengthInBytes = State.Data.Count();
            progInfo.NumberOfLines = listing.Lines.Count;

            return true;

        } // End ProcessRawProgram()

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
        private void processLineBody(ParserState State, byte[] tokenisedLine, ProgramLine returnObject, ProgInfo progInfo)
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
            bool closeTag = false;
            //bool flgLabel = false;

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
                // star command
                if (curbyte == '*' && startOfStatement)
                {
                    taggedline += SemanticTags.StarCommand;
                    rem = true;                         // rem ensures no further processing
                    startOfStatement = false;
                }

                // 1b) Wrapping semantic tokens
                // hex
                if (!rem && !quote)
                {
                    if (LexToken(
                    tokenisedLine, ref i,
                    startCondition: c => c == '&',
                    continueCondition: char.IsAsciiHexDigit,
                    tag: SemanticTags.HexNumber,
                    ref plainline, ref linenospaces, ref taggedline))
                    {
                        continue;
                    }
                    /* Binary (Basic V)
                    if (LexToken(
                    tokenisedLine, ref i,
                    startCondition: c => c == '%',
                    continueCondition: c => curchar == '0' || curchar == '1',
                    tag: SemanticTags.HexNumber,
                    ref plainline, ref linenospaces, ref taggedline))
                    {
                        continue;
                    }*/

                    if (curchar == ':') // include other conditions like implied : or space
                    {
                        taggedline += SemanticTags.StatementSep;
                        closeTag = true;
                    }
                    else if (curchar == ',' || curchar == ';')
                    {
                        taggedline += SemanticTags.ListSep;
                        closeTag = true;
                    }
                    if (curchar == '(')
                    {
                        taggedline += SemanticTags.OpenBracket;
                        closeTag = true;
                    }
                    else if (curchar == ')')
                    {
                        taggedline += SemanticTags.CloseBracket;
                        closeTag = true;
                    }
                    else if (!flgVar && curchar is '!' or '?' or '$')
                    {
                        taggedline += SemanticTags.IndirectionOperator;
                        closeTag = true;
                    }
                    else if (!flgVar && curchar is '#')
                    {
                        taggedline += State.InAsm ? SemanticTags.ImmediateOperator : SemanticTags.IndirectionOperator;
                        closeTag = true;
                    }
                }
                // Operators (+ - * / >> etc)
                if (!rem && !quote)
                {
                    if (LexOperator(tokenisedLine, ref i,
                                    ref plainline, ref linenospaces, ref taggedline))
                    {
                        continue;
                    }
                }
                // FN / PROC names
                if (flgFnOrProc)// (!rem && !quote)
                {
                    LexToken(
                        tokenisedLine, ref i,
                        startCondition: c => char.IsLetter(c) || c == '_',
                        continueCondition: c => char.IsLetterOrDigit(c) || c == '_',
                        tag: "",
                        ref plainline, ref linenospaces, ref taggedline);
                    {
                        flgFnOrProc = false;
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
                        if (curchar == '.')
                        {
                            taggedline += SemanticTags.Label;
                            //don't need a special label flag
                            flgVar = true;
                            // fall through
                        }
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
                #region number walker
                // Numbers
                if ((char.IsAsciiDigit(curchar) || curchar == '.') && !flgVar && !rem && !quote && !flgHex) // && !State.InAsm 
                {
                    taggedline += SemanticTags.Number;

                    bool seenE = false;

                    // Process first character of the number
                    addtoall(curchar, ref plainline, ref linenospaces, ref taggedline);

                    // Now walk forward
                    while (true)
                    {
                        // Look ahead safely
                        if (i + 1 >= tokenisedLine.Length)
                            break;

                        char next = (char)tokenisedLine[i + 1];

                        // Digits always allowed
                        if (char.IsAsciiDigit(next))
                        {
                            curchar = next;
                            i++;
                            addtoall(curchar, ref plainline, ref linenospaces, ref taggedline);
                            continue;
                        }

                        // Decimal point allowed (BBC BASIC allows multiple, but we don't need to)
                        if (next == '.')
                        {
                            curchar = next;
                            i++;
                            addtoall(curchar, ref plainline, ref linenospaces, ref taggedline);
                            continue;
                        }

                        // Exponent marker (only uppercase E)
                        if (next == 'E' && !seenE)
                        {
                            seenE = true;
                            curchar = next;
                            i++;
                            addtoall(curchar, ref plainline, ref linenospaces, ref taggedline);
                            continue;
                        }

                        // Sign allowed only immediately after E
                        if ((next == '+' || next == '-') && seenE)
                        {
                            curchar = next;
                            i++;
                            addtoall(curchar, ref plainline, ref linenospaces, ref taggedline);
                            seenE = false;
                            continue;
                        }

                        // Anything else ends the number
                        break;
                    }

                    taggedline += SemanticTags.Reset;
                    continue;
                }
                #endregion
                // Variables
                if (!flgVar && !rem && (char.IsAsciiLetter(curchar) || curbyte == '_') && !flgFnOrProc && !quote && !flgHex) // variables may start with letter or underline
                {
                    flgVar = true;
                    //if (!flgLabel) 
                        taggedline += SemanticTags.Variable;
                }

                // *** 2. Printable characters - NOW we add to listings ***
                if ((curbyte < 127 && curbyte > 31) || rem || quote) // Anything not a BASIC token
                {
                    if (curbyte > 32 || rem || quote) { linenospaces += curchar; }
                    plainline += curchar;
                    taggedline += curchar;

                    // 3. Things we do AFTER adding the character to the listings

                    if (closeTag) // catch single character tokens like : , ; ( )
                    {
                        taggedline += SemanticTags.Reset;
                        closeTag = false;
                    }
                    if (flgFnOrProc && !char.IsAsciiLetterOrDigit(curchar) && curbyte != '_')
                    {
                        flgFnOrProc = false;
                        taggedline += SemanticTags.Reset;
                    }
                    if (flgVar && !(char.IsAsciiLetterOrDigit(nxtchar) || nxtchar is '%' or '$' or '_'))
                    {
                        flgVar = false;
                        //flgLabel = false;
                        taggedline += SemanticTags.Reset;
                    }

                    if (curchar is ':' or ']' && !rem && !quote)
                        startOfStatement = true;  // a colon outside of quotes or REM is new statement; so is assembler delimiter
                    else if (curchar != ' ')
                        startOfStatement = false; // anything else isn't
                }
                else
                // 4. Now deal with tokens
                {
                    string keyword = getKeywordOrLineNumber(tokenisedLine, curbyte, ref i, ref nxtchar, progInfo, State);

                    if (keyword == "THEN") startOfStatement = true; // THEN signals new statement - ? ADD ELSE ?
                    if (keyword == "FN" || keyword == "PROC") flgFnOrProc = true;

                    if (curbyte == 0x8D)
                        taggedline += SemanticTags.LineNumber + keyword + SemanticTags.Reset; // a GOTO linenumber
                    else
                    {
                        string tag = SemanticTags.Keyword;
                        switch (keyword)
                        {
                            // No If ... Then ... Else - handled in code because depends on whether in multiLineIf or not (unless /breakapart)
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
                            // These cancel 1 indent for the line they're on, and indent following lines
                            case "OTHERWISE":
                            case "WHEN":
                                tag = SemanticTags.InOutKeyword;
                                break;
                            case "TAB(":  // no space after these
                            case "INSTR(":
                            case "POINT(":
                            case "LEFT$(":
                            case "MID$(":
                            case "RIGHT$(":
                            case "STRING$(":
                            case "GET":   // no space after bracket-optional function keywords
                            case "GET$":
                            case "CHR$":
                            case "ASC":
                            case "INKEY":
                            case "VAL":
                            case "LEN":
                            case "SQR":
                            case "ABS":
                            case "SGN":
                            case "EXP":
                            case "LOG":
                            case "SIN":
                            case "COS":
                            case "TAN":
                            case "ATN":
                            case "RND":
                            //case "EOF":  //? illegal without #
                            case "SPC":
                            //case "PTR":
                            //case "EXT":
                            case "EVAL":
                                tag = SemanticTags.BuiltInFn;
                                break;
                        }
                        taggedline += tag + keyword + SemanticTags.Reset;

                        if (keyword == "PROC")
                            taggedline += SemanticTags.ProcName;
                        if (keyword == "FN")
                            taggedline += SemanticTags.FunctionName;

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
            if (rem || flgFnOrProc || flgVar || asmComment || closeTag) taggedline += SemanticTags.Reset; // close hanging tags at end of line

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
                    nxtchar = (ptr == tokenisedLine.Length - 1) ? '\0' : (char)tokenisedLine[ptr + 1];
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
                if (curbyte != 0x8D) // 'line number' marker
                {
                    string keyword = token[curbyte];
                    if (keyword == "TO" && nxtchar == 'P')
                    {
                        keyword = "TOP";
                        nxtchar = (++ptr == tokenisedLine.Length - 1) ? '\0' : (char)tokenisedLine[ptr + 1];
                    }
                    return keyword;
                }

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

            using Stream stream = asm.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
