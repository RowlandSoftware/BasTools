using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using BasTools.Core;

namespace Text2Basic.CLI
{
    //***************** CommandSwitches *****************
    public class CommandSwitches
    {
        // switches for detokenisation
        internal bool basicV;
        internal bool Z80;
        internal bool noNumbers;
        public CommandSwitches()
        {
            basicV = false;
            Z80 = false;
            noNumbers = false;
        }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                help();
                return; // Environment.Exit(0);
            }

            BasToolsEngine engine = new BasToolsEngine();

            /******** Test harness only ****************/

            if (args[0].ToLower() == "/test")
            {
                Console.WriteLine("\nEnter empty line to finish.");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Enter line to tokenise");
                while (true)
                {
                    TokeniserState State = new();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(">");
                    Console.ForegroundColor = ConsoleColor.White;

                    string userinput = Console.ReadLine();
                    if (userinput != null)
                    {
                        if (string.IsNullOrWhiteSpace(userinput)) { break; }

                        // Normalise Windows £ (U+00A3) to Acorn £ / backtick (ASCII 96)
                        userinput = userinput.Replace('£', '`');

                        int dummy = 0;
                        ProgramLine result = Tokeniser.ProgramLineFromText(userinput, false, false, State, engine, ref dummy);
                        WriteTokenisedLine(result.TokenisedLine);
                    }
                }
                return;
            }

            /********** MAIN PROGRAM ************/

            CommandSwitches switches = new();
            string inputfile = string.Empty;
            string outputfile = string.Empty;
            bool save = false;

            //******** readCommandSwitches ********
            readCommandSwitches(args, switches, ref inputfile, ref outputfile, ref save);

            // Show message
            Console.Error.WriteLine("Processing, please wait...");

            try
            {
                string[] lines = ReadLines(inputfile);
                TokeniserState State = new();
                int FakeLineNum = 0;

                foreach (string textline in lines)
                {
                    ProgramLine result = Tokeniser.ProgramLineFromText(textline, false, false, State, engine, ref FakeLineNum);
                    //Console.Write($"{result.LineNumber} + "); WriteTokenisedLine(result.TokenisedLine);
                    //Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        private static readonly string[] Newlines = { "\r\n", "\n", "\r" };

        public static string[] ReadLines(string path)
        {
            string text = File.ReadAllText(path);
            return text.Split(Newlines, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        static void readCommandSwitches(string[] args, CommandSwitches switches, ref string inputfile, ref string outputfile, ref bool save)
        {
            foreach (string arg in args)
            {
                bool recognised = false;
                if (arg.StartsWith('/') && arg.Length > 1)
                {
                    string arg2 = arg.Substring(1).ToUpperInvariant(); // remove the /
                    string arg1 = string.Empty;
                    string arg3 = string.Empty;
                    int x = arg2.IndexOf('=');                // split at '=' or ':' if present
                    x = x >= 0 ? x : arg2.IndexOf(':');
                    if (x >= 0)
                    {
                        arg1 = arg2.Substring(0, x);
                        arg3 = arg2.Substring(x + 1);

                        if ("FILE".StartsWith(arg1)) { inputfile = arg3; recognised = true; }
                        if ("SAVE".StartsWith(arg1)) { outputfile = arg3; recognised = true; save = true; }
                    }
                    bool flgNegative = arg2.StartsWith('-');
                    if (flgNegative)
                        arg2 = arg2.Substring(1);

                    if (arg2 == "V") { switches.basicV = !flgNegative; recognised = true; }
                    if ("Z80".StartsWith(arg2)) { switches.Z80 = !flgNegative; recognised = true; }
                    if (arg2 == "?" || "HELP".StartsWith(arg2)) { help(); Environment.Exit(0); }
                    if ("NONUMBERS".StartsWith(arg2)) { switches.noNumbers = !flgNegative; recognised = true; }

                    if (!recognised)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine("Option " + arg.ToLowerInvariant() + " not recognised");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
                else
                {
                    if (inputfile.Length == 0) // This is where we pick up the filename if not already found
                        inputfile = arg;
                    else
                        if (!save)
                        {
                            outputfile = arg;
                            save = true;
                        }
                }
            }
            // no filename found:
            if (inputfile.Length == 0)
            {
                Console.Error.WriteLine("Error: No input filename found");
                help();
                Environment.Exit(0);
            }
            if (outputfile.Length == 0)
            {
                save = false;
            }
            //else Console.WriteLine($"={outputfile}");
        }

        /************** Utilities ***************/

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
        static void help()
        {
            string vs = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion ?? "1.1.0"; // ?? = null coalescing operator. //requires ref to System.Windows.Forms

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\nText2Basic vs {vs} for BasTools (C) Andrew Rowland 2022-26");
            Console.WriteLine("Converts text file to tokenised BBC BASIC program file");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n    Text2Basic [/file=]filename [[/save=]filename] [/V] [/Z80] [/nonumbers] [/list] [/blist] [/test]");
            Console.WriteLine("    Text2Basic [/? | /h]  Display help\n");
            Console.WriteLine("      [/file=]filename");
            Console.WriteLine("                   BASIC program in plain text format to be tokenised.");
            Console.WriteLine("                   Filename to follow '=' without spaces. Quote if contains spaces.");
            Console.WriteLine("                   '/file=' may be omitted if filename is first item");
            Console.WriteLine("      [/save=]filename");
            Console.WriteLine("                   Specifies filename of tokenised BASIC program.");
            Console.WriteLine("                   '/save=' may be omitted if filename is second item");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("    OPTIONS");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("    /V               Specifies that BASIC V keywords and assembler may be included");
            Console.WriteLine("    /Z80             Output file should be saved in Z80 format");
            Console.WriteLine("    /nonumbers       Do not number program lines (Z80 only)");
            Console.WriteLine("    /list            Display program after tokenising");
            Console.WriteLine("    /blist           Display program with PrettyPrint");
            Console.WriteLine("    /test            Invoke single line test harness for debug");

            Console.WriteLine("\nOptions may be specified in any order and can be abbreviated.");
            Console.WriteLine("Parameters containing spaces must be enclosed by double quotes.");
            Console.WriteLine("\nFor further help, see ReadMe.");
        }
    }
}
