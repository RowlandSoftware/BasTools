using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BasTools.Core
{
    public class Tokeniser
    {
        public enum MatchKind
        {
            None,
            FullKeyword,
            Abbreviation
        }
        public static ProgramLine ProgramLineFromText(string text, bool Z80, bool SkipSpaces, TokeniserState State, BasToolsEngine engine)
        {
            ProgramLine ProgLine = new();

            // First pass
            ProgLine.PlainDetokenisedLine = text;
            ProgLine.TokenisedLine = TokeniseLine(text, Z80, SkipSpaces, State, engine, out int linenum);
            ProgLine.LineNumber = linenum;
            WriteTokenisedLine(ProgLine.TokenisedLine);
            // Second pass - detokenise and tag
            ParserState parserState = new();
            ProgInfo progInfo = new(Z80, false, "NA");
            engine.ProcessLineBody(parserState, ProgLine.TokenisedLine, ProgLine, progInfo);

            Console.WriteLine(ProgLine.TaggedLine);

            // Third pass - compact tokenised line, inserting implied THEN as required
            ProgLine.TokenisedLine = NormaliseTokenised(ProgLine, engine);


            return ProgLine;
        }
        public static byte[] TokeniseLine(string text, bool Z80, bool SkipSpaces, TokeniserState State, BasToolsEngine engine, out int lineNumber)
        {
            State.StartOfLine(); // sets initial conditions

            ReadOnlySpan<char> s = text.AsSpan().Trim();
            int i = 0;

            lineNumber = GetLineNum(s, ref i);

            ReadOnlySpan<char> ln = s.Slice(i++).TrimStart();
            Console.WriteLine($"{lineNumber} {ln}");

            List<byte> tokenisedLine = TokeniseLineBody(ln, SkipSpaces, lineNumber, State, engine);

            /****** Make byte array of line number and line length ******/

            /****** combine that with 'tokenisedLine' *******/

            return tokenisedLine.ToArray();
        }
        private static int GetLineNum(ReadOnlySpan<char> s, ref int i)
        {
            /******* Line number ******/

            while (i < s.Length && char.IsDigit(s[i]))
                i++;

            int linenum = (i > 0) ? int.Parse(s[..i]) : 0;

            return linenum;
        }
        private static List<byte> TokeniseLineBody(ReadOnlySpan<char> ln, bool SkipSpaces, int linenum, TokeniserState State, BasToolsEngine engine)
        {
            List<byte> bytes = new();

            /******* Parse, looking for keywords *********/

            int p = 0;

            while (p < ln.Length)
            {
                char ch = ln[p];

                if (SkipSpaces) // don't skip spaces because of implied THENs
                {
                    if (char.IsWhiteSpace(ch))
                    {
                        p++;
                        continue;
                    }
                }

                /***** Start to copy characters / tokens *****/

                // HEX
                if (ch == '&')
                {
                    // Copy the '&' itself
                    bytes.Add((byte)ch);
                    p++;

                    while (p < ln.Length)
                    {
                        char c = ln[p];

                        bool isHexDigit =
                            char.IsAsciiHexDigitUpper(c);

                        if (!isHexDigit)
                            break;

                        bytes.Add((byte)c);
                        p++;
                    }
                    continue; // back to main loop
                }

                // QUOTED STRINGS
                else if (ch == '"')
                {
                    // Copy the opening quote
                    bytes.Add((byte)ch);
                    p++;

                    while (p < ln.Length)
                    {
                        char c = ln[p];

                        bytes.Add((byte)c);
                        p++;

                        if (c == '"')
                        {
                            // Closing quote: end of string literal
                            break;
                        }

                        if (p == ln.Length && c != '"')
                        {
                            // Unterminated string: BASIC stops tokenising the line
                            Console.WriteLine($"Missing closing quote at line {linenum}");
                            break;
                        }
                    }
                    continue; // back to main loop
                }

                // COLON
                else if (ch == ':')
                {
                    // colon: reset StartOfStatement, clear line-number flag
                    State.StartOfStatement = true;
                    State.LineNumberFlag = false;
                    bytes.Add((byte)ch);
                    p++;
                    continue;
                }
                // STAR COMMAND / MULTIPLY
                else if (ch == '*')
                {
                    if (State.StartOfStatement)
                    {
                        // *command: stop tokenising the rest of the line
                        // BASIC literally RTS here — no more scanning
                        // So we copy the '*' and the rest of the line verbatim.

                        bytes.Add((byte)ch);
                        p++;

                        while (p < ln.Length)
                        {
                            bytes.Add((byte)ln[p]);
                            p++;
                        }
                        break;   // end tokenisation for this line
                    }
                    else
                    {
                        // mid-statement: '*' is multiplication operator
                        bytes.Add((byte)ch);
                        p++;
                        continue;
                    }
                }
                // GOTO STYLE LINE NO.
                else if (State.LineNumberFlag)
                {
                    // 1. Skip spaces
                    while (p < ln.Length && ln[p] == ' ')
                        p++;

                    if (p >= ln.Length)
                    {
                        State.LineNumberFlag = false;
                        break;
                    }

                    char c = ln[p];

                    // 2. Comma → emit and continue line-number mode
                    if (c == ',')
                    {
                        bytes.Add((byte)',');
                        p++;
                        continue;
                    }

                    // 3. Digit → parse line number
                    if (char.IsDigit(c))
                    {
                        int start = p;
                        while (p < ln.Length && char.IsDigit(ln[p]))
                            p++;

                        string numText = ln[start..p].ToString();
                        if (ushort.TryParse(numText, out ushort lineNum))
                            EmitLineNumber(lineNum, bytes);
                        else
                            foreach (char cc in numText) bytes.Add((byte)cc);

                        continue;
                    }

                    // 4. Anything else → exit line-number mode
                    State.LineNumberFlag = false;
                    // fall through to normal tokenisation
                }
                else if (char.IsDigit(ch) || ch == '.')
                {

                    // Ordinary number: copy until non-digit or non-dot // TODO Sci Notation
                    bytes.Add((byte)ch);
                    p++;

                    while (p < ln.Length)
                    {
                        char c = ln[p];
                        if (!char.IsDigit(c) && c != '.')
                            break;

                        bytes.Add((byte)c);
                        p++;
                    }
                    State.StartOfStatement = false;
                    State.MiddleOfStatement = true;
                    continue;
                }
                // VARIABLE / PROC / FUNCTION NAME / KEYWORD
                if (char.IsLetter(ch) || ch == '_' || ch == '`') // £
                {
                    int start = p;
                    char c = '\0';

                    // If F‑flag is set, do not tokenise name
                    if (State.FN_PROCname)
                    {
                        bytes.Add((byte)ch);  // copy first letter
                        p++;

                        while (p < ln.Length)
                        {
                            c = ln[p];
                            if (!char.IsLetterOrDigit(c) && c != '_' && c != '`') // £
                                break;
                            bytes.Add((byte)c);
                            p++;
                        }

                        State.FN_PROCname = false;   // clear F-flag
                        State.StartOfStatement = false;
                        State.MiddleOfStatement = true;
                        continue;
                    }
                    else
                    {
                        // First character must be letter or '_' or '`' << ASCII 96 = £
                        p++;

                        while (p < ln.Length)
                        {
                            c = ln[p];

                            if (char.IsLetterOrDigit(c) || c == '_' || c == '`')
                            {
                                p++;
                                continue;
                            }

                            // Dot only allowed if it is abbreviation terminator:
                            if (c == '.')
                            {
                                // Accept the dot as part of the word
                                p++;
                                continue;
                            }
                            // Otherwise dot ends the name run
                            break;
                        }

                        ReadOnlySpan<char> word = ln[start..p];

                        var (kind, advanceBy, tokinfo) = TryKeyword(word, engine); // << We've got the 'word'. Is it a keyword or identifier?

                        if (kind == MatchKind.None) // not a keyword
                        {
                            // plain name
                            EmitName(word, ref bytes);
                            State.StartOfStatement = false;
                            State.MiddleOfStatement = true;
                            continue;
                        }

                        // KEYWORD FOUND
                        // C-flag handling (don't tokenise if followed by variable continuation character, e.g. TIMER)
                        if (tokinfo.fl('C'))
                        {
                            int next = start + advanceBy;

                            if (next < ln.Length)
                            {
                                c = ln[next];
                                if (char.IsLetterOrDigit(c) || c == '_' || c == '`') // £
                                {
                                    if (kind != MatchKind.Abbreviation)
                                    {
                                        EmitName(word, ref bytes);
                                        State.StartOfStatement = false;
                                        State.MiddleOfStatement = true;
                                        continue;
                                    }
                                }
                            }
                        }

                        // KEYWORD

                        p = start + advanceBy;

                        // Emit token
                        byte token1 = tokinfo.Token1;

                        if (tokinfo.fl('P') && State.StartOfStatement) // LHS pseudo-variable
                        {
                            token1 += 0x40;
                        }

                        bytes.Add(token1);

                        if (tokinfo.fl('2'))
                            bytes.Add(tokinfo.Token2);

                        // Apply flag bits (M, S, F, L, R)
                        ApplyFlags(tokinfo, ref State, ref p, ln, bytes);

                        continue;
                    }
                }
                // not a keyword either
                // symbol: copy as-is
                bytes.Add((byte)ch);
                p++;
            }
            return bytes;
        }
        private static (MatchKind kind, int advanceBy, TokenInfo tokinfo) TryKeyword(ReadOnlySpan<char> word, BasToolsEngine engine)
        {
            foreach (var t in engine.toktable)
            {
                TokenInfo tokinfo = t.Value;
                ReadOnlySpan<char> kw = tokinfo.Keyword.AsSpan();

                // First letter must match
                if (word[0] != kw[0])
                    continue;

                // Full keyword match
                if (word.Length >= kw.Length &&
                    word.Slice(0, kw.Length).SequenceEqual(kw))
                {
                    return (MatchKind.FullKeyword, kw.Length, tokinfo);
                }

                // Dot-terminated abbreviation: e.g. "PR." for "PRINT"
                int dot = word.IndexOf('.');
                if (dot > 0)
                {
                    int abbrevLen = dot;
                    if (abbrevLen <= kw.Length &&
                        word.Slice(0, abbrevLen).SequenceEqual(kw[..abbrevLen]))
                    {
                        return (MatchKind.Abbreviation, abbrevLen + 1, tokinfo);
                    }
                }
            }

            return (MatchKind.None, 0, default);
        }
        private static byte[] NormaliseTokenised(ProgramLine ProgLine, BasToolsEngine engine)
        {
            byte[] bytes = ProgLine.TokenisedLine;
            string taggedLine = ProgLine.TaggedLine;
            List<byte> output = new();
            int i = 0;

            foreach (Token tok in BasToolsEngine.WalkTagged(taggedLine))
            {
                switch (tok.tag)
                {
                    case SemanticTags.Keyword:
                    case SemanticTags.InOutKeyword:
                    case SemanticTags.IndentingKeyword:
                    case SemanticTags.BuiltInFn:
                        byte token1 = copyByte(ref i, bytes, output);
                        if (engine.IsDoubleToken(token1))
                            copyByte(ref i, bytes, output);
                        break;
                    case SemanticTags.LineNumber:
                        for (int j = 0; j < 4; j++)
                        {
                            copyByte(ref i, bytes, output);
                        }
                        break;
                    case null:
                        // skip spaces
                        while (i < bytes.Length && bytes[i] == 32) { i++; }
                        break;
                    default:
                        for (int j = 0; j < tok.value.Length; j++)
                        {
                            copyByte(ref i, bytes, output);
                        }
                        break;
                }
            }
            return output.ToArray();
        }
        private static byte copyByte(ref int i, byte[] bytes, List<byte> output)
        {
            if (i >= bytes.Length)
                throw new BasToolsException($"Error in copyByte. i >= bytes.Length: [{i}]");

            byte token1 = bytes[i];
            output.Add(token1);
            i++;
            return token1;
        }
        private static void EmitName(ReadOnlySpan<char> word, ref List<byte> bytes)
        {
            for (int i = 0; i < word.Length; i++)
            {
                bytes.Add((byte)word[i]);
            }
            return;
        }
        private static void ApplyFlags(TokenInfo tokinfo, ref TokeniserState State,
                    ref int p, ReadOnlySpan<char> ln,
                    List<byte> bytes)
        {
            // bit 0 (C-flag) already handled earlier

            // bit 1: M-flag (enter mid-statement)
            if (tokinfo.fl('M'))
            {
                State.StartOfStatement = false;
                State.MiddleOfStatement = true;
                State.LineNumberFlag = false;
            }

            // bit 2: S-flag (enter start-of-statement)
            if (tokinfo.fl('S'))
            {
                State.StartOfStatement = true;
                State.MiddleOfStatement = false;
                State.LineNumberFlag = false;
            }

            // bit 3: F-flag (skip FN/PROC name)
            if (tokinfo.fl('F'))
            {
                State.FN_PROCname = true;
            }

            // bit 4: L-flag (line number armed)
            if (tokinfo.fl('L'))
            {
                State.LineNumberFlag = true;
            }

            // bit 5: R-flag (skip rest of line: REM/DATA)
            if (tokinfo.fl('R'))
            {
                while (p < ln.Length)
                    bytes.Add((byte)ln[p++]);
            }
        }
        static void EmitLineNumber(ushort line, List<byte> bytes)
        {
            byte b0 = 0x8D;
            byte b1 = (byte)((((line & 0x00C0) >> 2) | ((line & 0xC000) >> 12)) ^ 0x54);
            byte b2 = (byte)(((line >> 0) & 0x3F) | 0x40);
            byte b3 = (byte)(((line >> 8) & 0x3F) | 0x40);

            bytes.Add(b0);
            bytes.Add(b1);
            bytes.Add(b2);
            bytes.Add(b3);
        }
        private static void WriteTokenisedLine(byte[] result)
        {
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] < 128)
                {
                    Console.Write((char)result[i]);
                }
                else { Console.Write($"[{result[i]:X2}]"); }
            }
            Console.WriteLine();
        }
    }
}