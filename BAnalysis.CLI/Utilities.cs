using BasTools.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace BasAnalysis.CLI
{
    internal static class Utilities
    {
        public static string[] SplitArgList(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return Array.Empty<string>();
            }
            List<string> parts = new();
            StringBuilder current = new();
            bool inQuotes = false;
            char quoteChar = '\0';

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                // Start or end of a quoted section
                if ((c == '"' || c == '\''))
                {
                    if (!inQuotes)
                    {
                        inQuotes = true;
                        quoteChar = c;
                    }
                    else if (c == quoteChar)
                    {
                        inQuotes = false;
                    }

                    current.Append(c);
                    continue;
                }

                // Space outside quotes ? split
                if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        parts.Add(current.ToString());
                        current.Clear();
                    }
                    continue;
                }

                // Normal character
                current.Append(c);
            }

            // Add final token
            if (current.Length > 0)
                parts.Add(current.ToString());

            return parts.ToArray();
        }

        public static void banner()
        {
            string vs = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion ?? "1.1.0"; // ?? = null coalescing operator. //requires ref to System.Windows.Forms

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\nBasAnalysis vs {vs} (C) Andrew Rowland 2022-26");
            Console.WriteLine("Detailed analysis of a BBC BASIC program\n");
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void help(string[] args)
        {
            if (args.Length <= 1)
            {
                banner();
                Console.WriteLine("    BasList <filename>");
                Console.WriteLine("\n    COMMANDS\n");
                Console.WriteLine("    {0,-10}{1,-10}{2,-10}{3,-10}{4,-10}{5,-10}", "help", "load", "analyze", "list", "lvar", "lvars");
                Console.WriteLine("    {0,-10}{1,-10}{2,-10}{3,-10}{4,-10}{5,-10}", "lfn", "lproc", "tree", "preview", "cls", "clear");
                Console.WriteLine("    {0,-10}{1,-10}{2,-10}", "exit", "quit", "x");
                Console.WriteLine("\nEnter help <command> for further help\n");
            }
            else
            {
                Console.WriteLine("");
                switch (args[1].ToLower(CultureInfo.InvariantCulture))
                {
                    case "load":
                        Console.WriteLine("load <file spec> - If file spec contains spaces, enclose in double quotes");
                        break;
                    case "list":
                        Console.WriteLine("list        - Display entire program");
                        Console.WriteLine("list nn nn  - Display program lines (from to)");
                        Console.WriteLine("list <name> - Display PROC or FN (list)");
                        break;
                    case "preview":
                        Console.WriteLine("preview     - Display first 20 lines of program");
                        break;
                    case "analyse":
                    case "analyze":
                        Console.WriteLine("analyze     - (or analyse) Use after 'load' and before other options");
                        break;
                    case "lvars":
                        Console.WriteLine("lvars       - Display analysis of variables, procedures and strings");
                        break;
                    case "lvar":
                        Console.WriteLine("lvar <variable>   - Display detailed analysis of named variable");
                        break;
                    case "lfn":
                        Console.WriteLine("lfn <FN name>     - Display detailed analysis of named function");
                        break;
                    case "lproc":
                        Console.WriteLine("lproc <PROC name> - Display detailed analysis of named procedure");
                        break;
                    case "listif":
                        Console.WriteLine("listif <text>     - Display lines that contain <text> (list)");
                        break;
                    case "cls":
                    case "clear":
                        Console.WriteLine("cls | clear - Clear screen");
                        break;
                    case "exit":
                    case "quit":
                    case "x":
                    case "end":
                        Console.WriteLine($"{args[1]} - Leave BasAnalysis");
                        break;
                    default:
                        Console.WriteLine($"'{args[1]}' not recognised");
                        break;
                }
            }
        }

        // Print line and check for pause
        // Returns: true - continue, false - stop listing
        internal static bool printLine(ProgramLine progLine, ref int linesprinted)
        {
            string line = progLine.FormattedPlain;
            string printedLine = progLine.LineNumber.ToString().PadLeft(5) + ' ' + new string(' ', progLine.IndentLevel * 2) + line;
            Console.WriteLine(printedLine);

            int windowWidth = Console.WindowWidth;
            int rows = (printedLine.Length + (windowWidth - 1)) / windowWidth;

            linesprinted += rows;

            // Deal with pausing
            switch (Utilities.CheckForPause(ref linesprinted))
            {
                case ConsoleKey.Spacebar: linesprinted = 0; break;
                case ConsoleKey.Enter: linesprinted--; break;
                case ConsoleKey.Escape: return false;
            }
            return true;
        }
        public static ConsoleKey CheckForPause(ref int linesprinted)
        {
            if (linesprinted == Console.WindowHeight - 4)
            {
                string prompt = " -- Enter - next line | Space - Continue | Esc - End --";
                if (Console.WindowWidth <= prompt.Length)
                {
                    prompt = "[Enter | Space | Esc]";
                }
                Console.Write(prompt);
                // Read until a valid key is pressed
                ConsoleKey key;
                while (true)
                {
                    var info = Console.ReadKey(intercept: true);
                    key = info.Key;

                    if (key == ConsoleKey.Spacebar ||
                    key == ConsoleKey.Enter ||
                    key == ConsoleKey.Escape)
                    {
                        break; // valid key
                    }
                }
                // Clear the prompt once
                ClearCurrentConsoleLine();
                return key;
            }
            return ConsoleKey.None;
        }
        private static void ClearCurrentConsoleLine()
        {
            if (Console.IsOutputRedirected) return;
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            for (int i = 0; i < Console.WindowWidth; i++)
                Console.Write(" ");
            Console.SetCursorPosition(0, currentLineCursor);
        }
    }
}