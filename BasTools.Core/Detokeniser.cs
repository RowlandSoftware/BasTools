using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using static System.Net.Mime.MediaTypeNames;

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
        private readonly HashSet<string> Z80Mnemonics;
        private readonly HashSet<string> Z80Registers;
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

            Z80Mnemonics = LoadMnemonicTable("BasTools.Core.Z80Mnemonics.txt");
            Z80Registers = new(StringComparer.OrdinalIgnoreCase)
            {
                "A","B","C","D","E","H","L","AF","BC","DE","HL",
                "IX","IY","IXH","IXL","IYH","IYL","SP","PC","I","R"
            };

            readTokenTable(token, "BasTools.Core.TokenTable.txt");      // actually a mix of all single-byte tokens
            readTokenTable(Vtoken, "BasTools.Core.VTokenTable.txt");    // double-byte tokens
        }        
        internal bool ProcessRawProgram(string fn, Listing listing, ProgInfo progInfo)
        {
            ParserState State = new();

            // *********** Load File **************

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
            }
            //DBG("Loaded");
            return true;
        }
        IEnumerable<LineRecord> ParseLines(byte[] data, ProgInfo progInfo)
        {
            int ptr = progInfo.Z80 ? 0 : 1;   // skip initial CR if Acorn
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
                ////DBG($">>>>> {lineNumber} ------");
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
        private void processLineBody(ParserState parserState, byte[] tokenisedLine, ProgramLine returnObject, ProgInfo progInfo)
        {
            firstPass(parserState, tokenisedLine, returnObject, progInfo);
            
            secondPass(returnObject);
            
            thirdPass(returnObject);
        }
        private void firstPass(ParserState parserState, byte[] tokenisedLine, ProgramLine returnObject, ProgInfo progInfo)
        {
            string plainline = string.Empty;
            string linenospaces = string.Empty;
            string taggedline = string.Empty;

            bool quote = false;
            bool rem = false;
            bool flgFnOrProc = false;
            bool flgVar = false;
            bool startOfStatement = true;
            bool asmComment = false;
            int prevbyte = 0;
            bool closeTag = false;

            //DBG($"LINE {returnObject.LineNumber} START");

            for (int i = 0; i <= tokenisedLine.Length - 1; i++)
            {
                byte curbyte = tokenisedLine[i];
                char curchar = (char)curbyte;
                char nxtchar = (i == tokenisedLine.Length - 1) ? '\0' : (char)tokenisedLine[i + 1];

                ////DBG($"  i={i}, curchar='{curchar}', rem={rem}, InAsm={parserState.InAsm}, asmComment={asmComment}");

                // 1. Stuff we do BEFORE adding curchar to the listings

                // 1a) Setting flags
                if (startOfStatement && curbyte == '[')
                    parserState.InAsm = true;

                if (startOfStatement && curbyte == ']')
                    parserState.InAsm = false;

                // deal with quotes
                if (curbyte == 34 && !rem)
                {
                    // toggle quote mode
                    if (!quote)
                        taggedline += SemanticTags.StringLiteral + '"';
                    else
                    {
                        taggedline += '"' + SemanticTags.Reset;
                        // closing quote may end an expression; doesn't matter what the string is
                        NoteExprTokenInIf(SemanticTags.StringLiteral, "", parserState);
                        //DBG($"[IF A] Token complete: tag={SemanticTags.StringLiteral}, value='string', ExprComplete={parserState.ExprComplete}");
                    }
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
                if (!rem && !quote)
                {
                    // hex
                    if (LexToken(
                    tokenisedLine, ref i,
                    startCondition: c => c == '&',
                    continueCondition: char.IsAsciiHexDigit,
                    tag: SemanticTags.HexNumber,
                    ref plainline, ref linenospaces, ref taggedline,
                    parserState))
                    {
                        continue;
                    }
                    // Binary (Basic V)
                    if (!flgVar)
                    {
                        if (LexToken(
                        tokenisedLine, ref i,
                        startCondition: c => c == '%',
                        continueCondition: c => c == '0' || c == '1',
                        tag: SemanticTags.BinaryNumber,
                        ref plainline, ref linenospaces, ref taggedline,
                        parserState))
                        {
                            continue;
                        }
                    }

                    if (curchar is ':' || (curchar == ' ' && parserState.InIfCondition && parserState.ExprComplete)) // implied THEN
                    {
                        //DBG($"Curchar: \"{curchar}\" ");
                        taggedline += SemanticTags.StatementSep;
                        closeTag = true;
                        parserState.InIfCondition = false;
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
                        if (parserState.InIfCondition)
                        {
                            parserState.IfParenDepth++;
                            //DBG($"[IF B] Paren depth++ ? {parserState.IfParenDepth} at i={i}");
                        }

                    }
                    else if (curchar == ')')
                    {
                        taggedline += SemanticTags.CloseBracket;
                        closeTag = true;

                        if (parserState.InIfCondition)
                        {
                            if (parserState.IfParenDepth > 0)
                                parserState.IfParenDepth--;

                            // now at depth 0: this can complete the expression
                            if (parserState.IfParenDepth == 0)
                            {
                                var (t, v) = getTagAndValueFromTaggedLine(taggedline); // see what last token  was
                                NoteExprTokenInIf(SemanticTags.CloseBracket, v, parserState);
                                //DBG($"[IF C] Token complete: tag={SemanticTags.CloseBracket}, value={v}, ExprComplete={parserState.ExprComplete}");
                            }
                        }
                    }
                    else if (!flgVar && curchar is '!' or '?' or '$' or '|') // Bar is BasV 5-byte FP indirection operator
                    {
                        taggedline += SemanticTags.IndirectionOperator;
                        closeTag = true;
                    }
                    else if (!flgVar && curchar is '#')
                    {
                        taggedline += parserState.InAsm ? SemanticTags.ImmediateOperator : SemanticTags.IndirectionOperator; // e.g. EOF#, PRINT#
                        closeTag = true;
                    }
                    // Operators (+ - * / >> etc)
                    if (LexOperator(tokenisedLine, ref i,
                    ref plainline, ref linenospaces, ref taggedline,
                    parserState))
                    {
                        continue;
                    }
                    // FN / PROC names
                    if (flgFnOrProc)// (!rem && !quote)
                    {
                        LexToken(
                        tokenisedLine, ref i,
                        startCondition: c => char.IsLetter(c) || c == '_',
                        continueCondition: c => char.IsLetterOrDigit(c) || c == '_',
                        tag: "", // because could be FN or PROC, already added
                        ref plainline, ref linenospaces, ref taggedline,
                        parserState);
                        {
                            flgFnOrProc = false;
                            var (tag, v) = getTagAndValueFromTaggedLine(taggedline);
                            NoteExprTokenInIf(tag, v, parserState);
                            //DBG($"[IF D] Token complete: tag={tag}, value={v}, ExprComplete={parserState.ExprComplete}");
                            continue;
                        }
                    }
                }
                #region Assembler
                //if (parserState.InAsm) Console.WriteLine("In asm");
                if (parserState.InAsm && !quote)
                {
                    if (!asmComment && curchar is '\\' or ';' or '\'')
                    {
                        asmComment = true;
                        rem = true;
                        startOfStatement = false;
                        checkForUnclosedTag(ref taggedline);        // ; will have been tagged but not closed, so remove
                        closeTag = false;
                        taggedline += SemanticTags.AssemblerComment;
                        ////DBG($"Start of comment: {taggedline}");
                    }
                    else if (asmComment && curchar == ':')
                    {
                        asmComment = false;
                        rem = false;
                        startOfStatement = true;
                        taggedline += SemanticTags.Reset;
                        ////DBG($"End of comment: {taggedline}");
                        // fall through so ':' still acts as statement separator
                    }
                    //if (asmComment) //DBG($"Char falling through: {curchar}");
                    if (!asmComment)
                    {
                        if (startOfStatement && curchar == '.')
                        {
                            taggedline += SemanticTags.Label;
                            //don't need a special label flag
                            flgVar = true;
                            // fall through
                        }
                        else
                        {
                            string possibleMnemonic = readMnemonic(tokenisedLine, i);
                            
                            if (possibleMnemonic != string.Empty)
                            {
                                bool isMnemonic;
                                if (progInfo.BasicV)
                                {
                                    isMnemonic = ArmMnemonics.Contains(possibleMnemonic.ToUpperInvariant());
                                }
                                else if (progInfo.Z80)
                                {
                                    isMnemonic = Z80Mnemonics.Contains(possibleMnemonic.ToUpperInvariant());
                                }
                                else
                                {
                                    isMnemonic = Mnemonics6502.Contains(possibleMnemonic.ToUpperInvariant()) || Regex.IsMatch(possibleMnemonic, "EQU[BDSW]", RegexOptions.IgnoreCase);
                                }
                                if (isMnemonic)
                                {
                                    taggedline += SemanticTags.Mnemonic + possibleMnemonic.ToUpper() + SemanticTags.Reset;
                                    plainline += possibleMnemonic;
                                    linenospaces += possibleMnemonic;

                                    i += possibleMnemonic.Length - 1;
                                    curbyte = tokenisedLine[i];
                                    curchar = (char)curbyte;
                                    nxtchar = (i == tokenisedLine.Length - 1) ? '\0' : (char)tokenisedLine[i + 1];
                                    prevbyte = (byte)plainline[^1];
                                    continue;
                                }
                                if (progInfo.BasicV)
                                {
                                    // ARM registers: Detect tokens like R0, R12, SP, LR, PC
                                    char uchar = char.ToUpperInvariant(curchar);
                                    char unext = char.ToUpperInvariant(nxtchar);
                                    if (uchar is 'R' && char.IsDigit(nxtchar) || (curchar is 'S' or 'L' or 'P' && unext is 'P' or 'R' or 'C'))
                                    {
                                        // R0–R15, SP, LR, PC
                                        string reg = readRegister(tokenisedLine, i);   // similar to readMnemonic
                                        if (ArmRegisters.Contains(reg.ToUpperInvariant()))
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
                                else if (progInfo.Z80)
                                {
                                    // Z80 registers: A, B, C, D, E, H, L,
                                    // AF, BC, DE, HL,
                                    // IX, IY, IXH, IXL, IYH, IYL,
                                    // SP, PC, I, R

                                    char uchar = char.ToUpperInvariant(curchar);

                                    // Fast‑path: first character must be A–S
                                    if ("ABCDEHILPRS".Contains(uchar))
                                    {
                                        string reg = readRegister(tokenisedLine, i).ToUpperInvariant();

                                        if (Z80Registers.Contains(reg))
                                        {
                                            // Tag it as a register
                                            taggedline += SemanticTags.Register + reg + SemanticTags.Reset;
                                            plainline += reg;
                                            linenospaces += reg;

                                            // Advance past the whole register
                                            i += reg.Length - 1;
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
                                if ((char.IsAsciiDigit(curchar) || curchar == '.') && !flgVar && !rem && !quote) // don't tag numbers as variables
                                { }
                                else
                                {
                                    if (!flgVar && !isMnemonic)// It's a variable, then (flgVar set if label)
                                    {
                                        taggedline += SemanticTags.Variable + possibleMnemonic + SemanticTags.Reset;
                                        plainline += possibleMnemonic;
                                        linenospaces += possibleMnemonic;

                                        i += possibleMnemonic.Length - 1;
                                        curbyte = tokenisedLine[i];
                                        curchar = (char)curbyte;
                                        nxtchar = (i == tokenisedLine.Length - 1) ? '\0' : (char)tokenisedLine[i + 1];
                                        prevbyte = (byte)plainline[^1];
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                } //end of assembler section
                #endregion
                #region number walker
                // Numbers
                if ((char.IsAsciiDigit(curchar) || curchar == '.') && !flgVar && !rem && !quote) // && !parserState.InAsm  && !flgHex
                {
                    taggedline += SemanticTags.Number;

                    bool seenE = false;

                    // Process first character of the number
                    addtoall(curchar, ref plainline, ref linenospaces, ref taggedline);
                    string num = curchar.ToString();

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
                            num += curchar;
                            i++;
                            addtoall(curchar, ref plainline, ref linenospaces, ref taggedline);
                            continue;
                        }

                        // Decimal point allowed (BBC BASIC allows multiple, but we don't need to)
                        if (next == '.')
                        {
                            curchar = next;
                            num += '.';
                            i++;
                            addtoall(curchar, ref plainline, ref linenospaces, ref taggedline);
                            continue;
                        }

                        // Exponent marker (only uppercase E)
                        if (next == 'E' && !seenE)
                        {
                            seenE = true;
                            curchar = next;
                            num += 'E';
                            i++;
                            addtoall(curchar, ref plainline, ref linenospaces, ref taggedline);
                            continue;
                        }

                        // Sign allowed only immediately after E
                        if ((next == '+' || next == '-') && seenE)
                        {
                            curchar = next;
                            num += curchar;
                            i++;
                            addtoall(curchar, ref plainline, ref linenospaces, ref taggedline);
                            seenE = false;
                            continue;
                        }

                        // Anything else ends the number
                        break;
                    }

                    taggedline += SemanticTags.Reset;
                    NoteExprTokenInIf(SemanticTags.Number, num, parserState);
                    //DBG($"[IF E] Token complete: tag={SemanticTags.Number}, value={num}, ExprComplete={parserState.ExprComplete}");
                    continue;
                }
                #endregion
                // Variables - may start with letter or underline
                if (!flgVar && !rem && (char.IsAsciiLetter(curchar) || curbyte == '_') && !flgFnOrProc && !quote) // && !flgHex
                {
                    flgVar = true;
                    taggedline += SemanticTags.Variable;
                }

                // *** 2. Printable characters - NOW we add to listings ***
                if ((curbyte < 127 && curbyte > 31) || rem || quote) // Anything not a BASIC token
                {
                    ////DBG($"Catching {curchar}");
                    if (curbyte > 32 || rem || quote) { linenospaces += curchar; }
                    plainline += curchar;
                    taggedline += curchar;

                    //.if (asmComment) //DBG(taggedline);

                    // 3. Things we do AFTER adding the character to the listings

                    if (closeTag) // close after single character tokens like : , ; ( ) have been added
                    {
                        taggedline += SemanticTags.Reset;
                        closeTag = false;
                    }
                    // end of variables
                    if (flgVar)
                    {
                        if (curchar is '%' or '$') // have added the last character of variable
                        {
                            flgVar = false;
                            taggedline += SemanticTags.Reset;
                            //flgLabel = false;
                            var (t, v) = getTagAndValueFromTaggedLine(taggedline);
                            NoteExprTokenInIf(SemanticTags.Variable, v, parserState);
                            //DBG($"[IF F] Token complete: tag={SemanticTags.Variable}, value={v}, ExprComplete={parserState.ExprComplete}");
                        }
                        else if (!char.IsAsciiLetterOrDigit(nxtchar) & nxtchar is not '_' and not '%' and not '$') // char coming up is not legal in variable names
                        {
                            flgVar = false;
                            taggedline += SemanticTags.Reset;
                            //flgLabel = false;
                            var (t, v) = getTagAndValueFromTaggedLine(taggedline);
                            NoteExprTokenInIf(SemanticTags.Variable, v, parserState);
                            //DBG($"[IF G] Token complete: tag={t}, value={v}, ExprComplete={parserState.ExprComplete}");
                        }
                    }
                    // check for end of statement
                    if (curchar is ':' or ']' && !rem && !quote)
                        startOfStatement = true;  // a colon outside of quotes or REM is new statement; so is assembler delimiter
                    else if (curchar != ' ')
                        startOfStatement = false; // anything else isn't
                }
                else
                // 4. Now deal with tokens
                {
                    string keyword = getKeywordOrLineNumber(tokenisedLine, curbyte, ref i, ref nxtchar, progInfo, parserState);

                    // Implied THEN tracking
                    if (keyword == "IF")
                    {
                        parserState.InIfCondition = true;
                        parserState.IfParenDepth = 0;
                        parserState.ExprComplete = false;
                        //DBG($"[IF H] Start IF condition at i={i}");
                    }
                    if (keyword == "THEN" || keyword == "ELSE")
                    {
                        parserState.InIfCondition = false;
                        startOfStatement = true;
                        //DBG($"[IF I] End IF condition at i={i} via {keyword}");
                    }

                    if (keyword == "FN" || keyword == "PROC") flgFnOrProc = true;

                    if (curbyte == 0x8D)
                        taggedline += SemanticTags.LineNumber + keyword + SemanticTags.Reset; // a GOTO linenumber
                    else
                    {
                        string tag = SemanticTags.Keyword;
                        switch (keyword)
                        {
                            // No If ... Then ... Else - handled in code because depends on whether in multiLineIf or not (unless /splitlines)
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
                            case "EOF":  //? illegal without #
                            case "SPC":
                            case "PTR":
                            case "EXT":
                            case "EVAL":
                            case "OPENIN":
                            case "OPENOUT":
                            case "OPENUP":
                                tag = SemanticTags.BuiltInFn;
                                break;
                        }
                        taggedline += tag + keyword + SemanticTags.Reset;

                        if (tag == SemanticTags.BuiltInFn && keyword.EndsWith('(')) // catch tokens like STRING$(
                        {
                            if (parserState.InIfCondition) parserState.IfParenDepth++;
                        }
                        if (tag == SemanticTags.Keyword && keyword is not "IF" and not "THEN" and not "ELSE")
                        {
                            NoteExprTokenInIf(tag, keyword, parserState);
                            //DBG($"[IF J] Token complete: tag: {tag}, value: {keyword}, ExprComplete={parserState.ExprComplete}");
                        }

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

                        if (keyword == "REM")
                        {
                            rem = true;
                            startOfStatement = false;
                            taggedline += SemanticTags.RemText;
                        }

                        plainline += keyword;
                    }

                    linenospaces += keyword;
                }
                prevbyte = curbyte;
            }
            if (rem || flgFnOrProc || flgVar || asmComment || closeTag) taggedline += SemanticTags.Reset; // close hanging tags at end of line
            
            //if (parserState.InAsm) //DBG($"InAsm {parserState.InAsm} - {taggedline}");

            returnObject.PlainDetokenisedLine = plainline;
            returnObject.TaggedLine = taggedline;
            returnObject.NoSpacesLine = linenospaces;
            returnObject.InAsm = parserState.InAsm;
            returnObject.IsArm = (progInfo.BasicV && !progInfo.Z80);
            returnObject.IsZ80 = (progInfo.Z80);

            return;
        }
        private void secondPass(ProgramLine returnObject)
        {
            // - Tracking expression completeness to insert StatementSep where neither space nor :
            //   for 'implied THEN' and line statement splitting

            var tokens = tokenListFromTaggedLine(returnObject.TaggedLine);
            var sb = new StringBuilder();

            bool inIf = false;
            int parenDepth = 0;

            for (int i = 0; i < tokens.Count; i++)
            {
                var t = tokens[i];

                // Always emit the current token
                sb.Append(t.tag);
                sb.Append(t.value);
                if (t.tag != null)
                    sb.Append(SemanticTags.Reset);

                // Track IF/THEN/ELSE
                if (t.tag == SemanticTags.Keyword && t.value == "IF")
                {
                    inIf = true;
                    parenDepth = 0;
                    continue;
                }

                if (t.tag == SemanticTags.Keyword && (t.value == "THEN" || t.value == "ELSE"))
                {
                    inIf = false;
                    continue;
                }

                // Track parentheses
                if (t.tag == SemanticTags.OpenBracket || (t.tag == SemanticTags.BuiltInFn && t.value.EndsWith('(')))
                    parenDepth++;
                if (t.tag == SemanticTags.CloseBracket)
                    parenDepth--;

                // Only consider implied THEN if inside IF and not inside parentheses
                if (!inIf || parenDepth != 0)
                    continue;

                // Expression is complete if this token is a terminal
                bool exprComplete =
                t.tag == SemanticTags.Variable ||
                t.tag == SemanticTags.Number ||
                t.tag == SemanticTags.StringLiteral ||
                t.tag == SemanticTags.CloseBracket ||
                (t.tag == SemanticTags.Keyword &&
                t.value != "AND" &&
                t.value != "OR" &&
                t.value != "EOR" &&
                t.value != "NOT" &&
                t.value != "FN");

                if (!exprComplete)
                    continue;

                var (nextIndex, next) = NextSignificantToken(tokens, i);

                if (nextIndex == -1)
                    continue;   // no implied THEN at end of line

                // Continuation tokens
                bool continuation =
                next.tag == SemanticTags.Operator ||
                next.tag == SemanticTags.OpenBracket ||
                next.tag == SemanticTags.Variable ||
                next.tag == SemanticTags.Number ||
                next.tag == SemanticTags.StringLiteral ||
                next.tag == SemanticTags.BuiltInFn;

                if (next.tag == SemanticTags.Keyword && (next.value == "AND" || next.value == "OR" || next.value == "EOR"))
                {
                    continuation = true;
                }

                // NEW RULE: A variable does NOT continue the expression if the current token is terminal
                bool currentIsTerminal =
                t.tag == SemanticTags.Variable ||
                t.tag == SemanticTags.Number ||
                t.tag == SemanticTags.StringLiteral ||
                t.tag == SemanticTags.CloseBracket ||
                t.tag == SemanticTags.FunctionName ||
                (t.tag == SemanticTags.Keyword &&
                t.value != "AND" &&
                t.value != "OR" &&
                t.value != "EOR" &&
                t.value != "NOT" &&
                t.value != "FN");

                if (currentIsTerminal && next.tag == SemanticTags.Variable)
                {
                    continuation = false;
                }

                // Chained IF
                if (next.tag == SemanticTags.Keyword && next.value == "IF")
                    continuation = true;

                if (continuation)
                    continue;

                // Explicit separators already present
                // If the next token is an original separator (space or colon) or THEN, do NOT insert implied THEN
                if (
                (next.tag == SemanticTags.StatementSep &&
                (next.value == " " || next.value == ":")) ||
                (next.tag == SemanticTags.Keyword && next.value == "THEN")
                )
                    continue;

                // Insert implied THEN
                sb.Append($"{SemanticTags.StatementSep}{SemanticTags.Reset}");

                // Skip literal whitespace after an inserted null separator
                int j = nextIndex;
                while (j < tokens.Count && tokens[j].tag == null && string.IsNullOrWhiteSpace(tokens[j].value))
                {
                    j++;
                }
                // Continue processing from the first non-whitespace token
                i = j - 1;

                inIf = false;
            }

            returnObject.TaggedLine = sb.ToString();
        }
        private void thirdPass(ProgramLine returnObject)
        {
            // - Tracking boolean expressions to detect = as Comparison Operator for
            //   assignment/reference detection in BasAnalysis.
            //   (Could do this in firstPass but this keeps the logic together)

            var tokens = tokenListFromTaggedLine(returnObject.TaggedLine);
            var sb = new StringBuilder();

            bool insideBoolean = false;
            int parenDepth = 0;
            bool inArgList = false;

            for (int i = 0; i < tokens.Count; i++)
            {
                var tok = tokens[i];

                // Enter boolean context
                if ((tok.tag == SemanticTags.Keyword && tok.value == "IF") ||
                    (tok.tag == SemanticTags.IndentingKeyword && tok.value == "WHILE") || 
                    (tok.tag == SemanticTags.OutdentingKeyword && tok.value == "UNTIL"))
                    insideBoolean = true;

                // Leave boolean context at THEN
                if (tok.tag == SemanticTags.Keyword && tok.value == "THEN")
                    insideBoolean = false;

                // New statement always ends boolean context
                if (tok.tag == SemanticTags.StatementSep) // whether : space or null
                    insideBoolean = false;

                // Track parentheses
                if (tok.tag == SemanticTags.OpenBracket)
                {
                    parenDepth++;
                    if (inArgList) insideBoolean = true;   // FN/PROC arguments
                }

                if (tok.tag == SemanticTags.CloseBracket)
                {
                    parenDepth--;
                    if (parenDepth == 0) inArgList = false;
                }

                // Detect FN/PROC argument lists
                if (tok.tag == SemanticTags.FunctionName || tok.tag == SemanticTags.ProcName)
                    inArgList = true;

                // PRINT expressions are boolean contexts too
                if (tok.tag == SemanticTags.Keyword && tok.value == "PRINT")
                    insideBoolean = true;

                // Now we've done all the checking and flags...
                if (tok.tag == SemanticTags.Operator && tok.value == "=") // it could be a mis-classified =
                {
                    // What's the rule??
                    if (insideBoolean) //  || parenDepth != 0 || inArgList
                        tok.tag = SemanticTags.IsEqualTo_Operator; // change operator tag
                }
                // Always emit the current token
                sb.Append(tok.tag);
                sb.Append(tok.value);
                if (tok.tag != null)
                    sb.Append(SemanticTags.Reset);

            } // end foreach

            returnObject.TaggedLine = sb.ToString();
        }
        // Returns (index, token) where token is (value, tag)
        // If none found, returns (-1, (null, null))
        private (int index, (string value, string tag) token)
        NextSignificantToken(List<(string Value, string Tag)> tokens, int start)
        {
            for (int j = start + 1; j < tokens.Count; j++)
            {
                var tok = tokens[j];

                // Skip null-tag tokens
                if (tok.Tag == null)
                    continue;

                return (j, tok);
            }

            return (-1, (null, null));
        }
        private string getKeywordOrLineNumber(byte[] tokenisedLine, byte curbyte, ref int ptr, ref char nxtchar, ProgInfo progInfo, ParserState s)
        {
            try
            {
                if (!s.Z80 && (curbyte > 197 && curbyte < 201))
                {
                    progInfo.BasicV = true;
                    int token = curbyte * 256 + tokenisedLine[++ptr];
                    nxtchar = (ptr == tokenisedLine.Length - 1) ? '\0' : (char)tokenisedLine[ptr + 1];
                    return Vtoken[token];
                }
                if (s.Z80) // RT Russell tokens that could not be included in token table (though overlap)
                {
                    switch (curbyte)
                    {
                        case 0xC6: return "SUM";        // 198
                        case 0xC7: return "WHILE";      // 199
                        case 0xC8: return "CASE";       // 200 LOAD
                        case 0xC9: return "WHEN";       // 201 WHEN
                        case 0xCA: return "OF";         // 202 OF
                        case 0xCB: return "ENDCASE";    // 203 ENDCASE
                        case 0xCC: return "OTHERWISE";  // 204 ELSE (multiline)
                        case 0xCD: return "ENDIF";      // 205 ENDIF
                        case 0xCE: return "ENDWHILE";   // 206 ENDWHILE
                    }
                    /*The token for PUT has changed from & CE (206) in version 3 to & 0E in version 5.
                    If this token is present in existing programs it will list as ENDWHILE rather
                    than PUT, and the programs will need to be modified to restore functionality.
                    [WHATSNEW.TDT in BBCZ80V5 co-pro.zip\B\0\ from http://rtrussell.co.uk/bbcbasic/z80basic.html]
                    Note: 0E is BY in Windows versions of R.T. Russell BASIC. BY is encoded as 'B' 'Y'
                    in Arm Basic according to https://mdfs.net/Docs/Comp/BBCBasic/Tokens, and BY is 0F
                    */
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
        //
        // Utility procedures
        //
        static void NoteExprTokenInIf(string tag, string keyword, ParserState parserState)
        {
            //DBG($"Checking {tag} {keyword}, InIfCondition = {parserState.InIfCondition}, starting paren depth {parserState.IfParenDepth}");
            if (!parserState.InIfCondition) return;

            if (parserState.IfParenDepth > 0)
            {
            parserState.ExprComplete = false;
            return;
            }

            // Keywords that continue an expression
            if (tag == SemanticTags.Keyword && keyword is "AND" or "OR" or "EOR")
            {
            parserState.ExprComplete = false;
            return;
            }

            // Operators / built-in functions / indirection etc. should also reset
            if (tag == SemanticTags.Operator || tag == SemanticTags.BuiltInFn || tag == SemanticTags.IndirectionOperator) // is the last condition accurate?
            {
            parserState.ExprComplete = false;
            return;
            }

            // Variables, numbers, string literals, closing ) at depth 0 ? potentially complete
            parserState.ExprComplete = true;
        }
        private (string tag, string value) getTagAndValueFromTaggedLine(string taggedline)
        {
            if (string.IsNullOrEmpty(taggedline))
                return ("", "");

            // Pattern: {=TAG}VALUE{/} at end of string
            Regex r = new Regex(@"(\{=[^}]*\})([^{}]*)(\{/\})$", RegexOptions.CultureInvariant);

            Match m = r.Match(taggedline);
            if (!m.Success)
                return ("", "");

            string tag = m.Groups[1].Value;
            string value = m.Groups[2].Value;

            return (tag, value);
        }
        private void checkForUnclosedTag(ref string taggedline)
        {
            int x = taggedline.LastIndexOf("{=");
            if (x == -1) return;
            int y = taggedline.IndexOf('}', x);
            if (y == -1)
            {
                throw new BasToolsException("Malformed tag in '" + taggedline + "'");
            }
            string tag = taggedline.Substring(x, y-x+1);
            if (taggedline.EndsWith(tag))
            {
                taggedline = taggedline.Substring(0, x);
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
        static void DBG(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}