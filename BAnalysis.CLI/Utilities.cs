using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace BAnalysis.CLI
{
    internal static class Utilities
    {
        public static string[] SplitArgList(string input)
        {
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

                // Space outside quotes → split
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
            Console.WriteLine("Detailed analysis of a BBC BASIC program file\n");
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void help(string[] args)
        {
            banner();

            if (args.Length <= 1)
            {
                Console.WriteLine("    BasList <filename>");
                Console.WriteLine("\n    COMMANDS\n");
                Console.WriteLine("    {0,-10}{1,-10}{2,-10}{3,-10}{4,-10}{5,-10}", "help", "load", "analyze", "list", "lvar", "lvars");
                Console.WriteLine("    {0,-10}{1,-10}{2,-10}{3,-10}{4,-10}{5,-10}", "lfn", "lproc", "tree", "preview", "exit", "quit");
                Console.WriteLine("\nEnter help <command> for further help\n");
            }
            else
            {
                switch (args[1].ToLower())
                {
                    case "load":
                        Console.WriteLine("load <file spec> - If file spec contains spaces, enclose in double quotes");
                        break;
                    case "analyse":
                    case "analyze":
                    case "exit":
                        Console.WriteLine($"{args[1]} - Leave BasAnalysis");
                        break;
                    default:
                        Console.WriteLine($"'{args[1]}' not recognised");
                        break;
                }
            }
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