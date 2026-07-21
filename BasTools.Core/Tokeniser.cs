using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BasTools.Core
{
    public class Tokeniser
    {
        public static byte[] TokeniseLine(string text, BasToolsEngine engine)
        {
            TokeniserState State = new TokeniserState(); // sets initial conditions
            List<byte> bytes = new();

            ReadOnlySpan<char> s = text.AsSpan().Trim();
            int i = 0;

            while (i < s.Length && char.IsDigit(s[i]))
                i++;

            int linenum = (i > 0) ? int.Parse(s[..i]) : 0;

            // parse, looking for keywords
            ReadOnlySpan<char> ln = s.Slice(i++).TrimStart();
            Console.WriteLine($"{linenum} {ln}");

            int p = 0;

            while (p < ln.Length)
            {
                bool matched = false;

                foreach (var t in engine.toktable)
                {
                    TokenInfo tokinfo = t.Value;
                    ReadOnlySpan<char> kw = tokinfo.Keyword.AsSpan();

                    if (p + kw.Length > ln.Length)
                        continue;

                    ReadOnlySpan<char> slice = ln.Slice(p, kw.Length);
                    if (DoCompare(slice, kw, out int advanceby))
                    {
                        bytes.Add(tokinfo.Token1);

                        if (tokinfo.Flags.HasFlag(TokenFlags.TwoByte))
                            bytes.Add(tokinfo.Token2);

                        p += advanceby;
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    bytes.Add((byte)ln[p]);
                    p++;
                }
            }

            // make byte array of line number and line length

            // combine that with 'bytes'

            return bytes.ToArray();
        }
        private static bool DoCompare(ReadOnlySpan<char> slice, ReadOnlySpan<char> kw, out int advanceby)
        {
            if (slice.SequenceEqual(kw))
            {
                advanceby = kw.Length;
                return true;
            }

            int x = slice.IndexOf('.');
            if (x > 0)
            {
                slice = slice.Slice(0, x);
                ReadOnlySpan<char> kp = kw.Slice(0, x);
                advanceby = x+1;
                return (slice.SequenceEqual(kp));
            }
            else
            {
                advanceby = 0;
                return false;
            }
        }
    }
}