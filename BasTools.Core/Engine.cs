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
    using System.Linq.Expressions;
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
    //
    //***************** The Engine *****************
    //
    public partial class BasToolsEngine
    {
        public Listing loadAndFormatFile(string filename, FormattingOptions formatOptions, ProgInfo progInfo)
        {
            Listing listing = new(new List<ProgramLine>());

            try
            {
                ProcessRawProgram(filename, listing, progInfo);
                {
                    try
                    {
                        FormatProgram(listing, formatOptions, progInfo.BasicV);
                        {
                            return listing;
                        }
                    }
                    catch (Exception e1)
                    {
                        throw new BasToolsException("Error while formatting the program", e1);
                    }
                }
            }
            catch (Exception e2)
            {
                throw new BasToolsException($"Program '{filename}' could not be processed", e2);
            }
        }
        //public BasToolsEngine()
        public static IEnumerable<(string value, string tag, bool isLast)> WalkTagged(string line)
        {
            if (line == null) yield break;
            int i = 0;

            // First, collect all items into a temporary list
            var items = new List<(string value, string tag)>();

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
        static void DumpResourceNames()
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (var name in asm.GetManifestResourceNames())
                Console.WriteLine(name);
        }
    }
}
