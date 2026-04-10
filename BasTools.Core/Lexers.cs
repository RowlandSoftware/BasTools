using System;
using System.Collections.Generic;
using System.Text;

namespace BasTools.Core
{
    public partial class BasToolsEngine
    {
        //***************** Lexers *****************
        //
        static readonly string[] MultiOps = { "+=", "-=", ">>", ">>>", "<<", "<=", ">=", "<>" };
        static bool TryGetMultiOperator(byte[] line, int index, out string op)
        {
            foreach (var m in MultiOps)
            {
                int len = m.Length;

                // Bounds check
                if (index + len > line.Length)
                    continue;

                bool match = true;

                for (int j = 0; j < len; j++)
                {
                    if ((char)line[index + j] != m[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    op = m;
                    return true;
                }
            }
            op = null!;
            return false;
        }
        /// <summary>
        /// Attempts to lex an operator starting at index i.
        /// Returns true if an operator was consumed, and advances i accordingly.
        /// </summary>
        static bool LexOperator(
            byte[] line,
            ref int i,
            ref string plainline,
            ref string linenospaces,
            ref string taggedline,
            ParserState parserState)
        {
            char c = (char)line[i];

            if (!IsOperatorChar(c))
                return false;

            taggedline += SemanticTags.Operator;
            string op = string.Empty;

            // Try multi-operator first
            if (TryGetMultiOperator(line, i, out var multi))
            {
                foreach (char ch in multi)
                {
                    op += ch;
                    addtoall(ch, ref plainline, ref linenospaces, ref taggedline);
                }
                i += multi.Length - 1; // advance past operator
            }
            else
            {
                // Single-character operator
                addtoall(c, ref plainline, ref linenospaces, ref taggedline);
                op = c.ToString();
            }

            taggedline += SemanticTags.Reset;
            NoteExprTokenInIf(SemanticTags.Operator, op, parserState);

            return true;
        }
        static bool LexToken(
            byte[] line,
            ref int i,
            Func<char, bool> startCondition,
            Func<char, bool> continueCondition,
            string tag,
            ref string plainline,
            ref string linenospaces,
            ref string taggedline,
            ParserState parserState)
        {
            char c = (char)line[i];

            // Check start condition
            if (!startCondition(c))
                return false;

            // Begin tag
            string keyword = string.Empty;
            taggedline += tag;

            // Consume first character
            addtoall(c, ref plainline, ref linenospaces, ref taggedline);

            // Consume continuation characters
            int pos = i + 1;
            while (pos < line.Length)
            {
                char next = (char)line[pos];
                keyword += (char)line[pos];

                if (!continueCondition(next))
                    break;

                addtoall(next, ref plainline, ref linenospaces, ref taggedline);
                pos++;
            }

            // Advance index
            i = pos - 1;

            // End tag
            taggedline += SemanticTags.Reset;
            if (tag != "") NoteExprTokenInIf(tag, keyword, parserState);

            return true;
        }
        static string readRegister(byte[] tokenisedLine, int index)
        {
            int i = index;
            while (i < tokenisedLine.Length - 1 && char.IsAsciiLetterOrDigit((char)tokenisedLine[i]))
                i++;

            return Encoding.ASCII.GetString(tokenisedLine, index, i - index);
        }
        static bool IsOperatorChar(char c) => c is '+' or '-' or '/' or '*' or '=' or '<' or '>' or '^';
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
        static string readMnemonic(byte[] tokenisedLine, int ptr)
        {
            string result = string.Empty;

            while (ptr <= tokenisedLine.Length - 1 && (char.IsAsciiLetterOrDigit((char)tokenisedLine[ptr]) || ((char)tokenisedLine[ptr] is '%' or '$' or '_'))) // if we capture MORE than a mnemonic, it is a variable, e.g. lda123, opt%
            {
                result += (char)tokenisedLine[ptr++];
            }
            return result;
        }
    }
}