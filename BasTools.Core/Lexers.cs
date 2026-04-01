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
            ref string taggedline)
        {
            char c = (char)line[i];

            if (!IsOperatorChar(c))
                return false;

            taggedline += SemanticTags.Operator;

            // Try multi-operator first
            if (TryGetMultiOperator(line, i, out var multi))
            {
                foreach (char ch in multi)
                    addtoall(ch, ref plainline, ref linenospaces, ref taggedline);

                i += multi.Length - 1; // advance past operator
            }
            else
            {
                // Single-character operator
                addtoall(c, ref plainline, ref linenospaces, ref taggedline);
            }

            taggedline += SemanticTags.Reset;
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
            ref string taggedline)
        {
            char c = (char)line[i];

            // Check start condition
            if (!startCondition(c))
                return false;

            // Begin tag
            taggedline += tag;

            // Consume first character
            addtoall(c, ref plainline, ref linenospaces, ref taggedline);

            // Consume continuation characters
            int pos = i + 1;
            while (pos < line.Length)
            {
                char next = (char)line[pos];

                if (!continueCondition(next))
                    break;

                addtoall(next, ref plainline, ref linenospaces, ref taggedline);
                pos++;
            }

            // Advance index
            i = pos - 1;

            // End tag
            taggedline += SemanticTags.Reset;

            return true;
        }
    }
}