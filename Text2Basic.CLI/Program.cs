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
            ProgInfo progInfo = new ProgInfo();

            /******** Test harness only ****************/
            #region test harness
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
                        ProgramLine result = engine.ProgramLineFromText(userinput, false, false, State, ref dummy);
                        WriteTokenisedLine(result.TokenisedLine);
                    }
                }
                return;
            }
            #endregion
            /********** MAIN PROGRAM ************/

            TokeniserCommandSwitches switches = new();

            //******** readCommandSwitches ********
            readCommandSwitches(args, switches);

            // Show message
            Console.Error.WriteLine("Processing, please wait...");

            engine.LoadAndTokeniseFile(switches, progInfo);
            Console.Error.WriteLine($"{engine.CurrentListing.Lines.Count} lines processed");

            if (switches.list || switches.blist)
            {
                BasAnalysis.CLI.Utilities.List(engine, 0, 0xFEFF, 20, switches.blist); // TODO
            }
            if (switches.save)
            {
                savefile(switches, engine);
            }
        }
        static void readCommandSwitches(string[] args, TokeniserCommandSwitches switches)
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

                        if ("FILE".StartsWith(arg1)) { switches.inputfile = arg3; recognised = true; }
                        if ("SAVE".StartsWith(arg1)) { switches.outputfile = arg3; recognised = true; switches.save = true; }
                    }

                    if (arg2 == "V") { switches.basicV = true; recognised = true; }
                    if ("Z80".StartsWith(arg2)) { switches.Z80 = true; recognised = true; }
                    if ("NONUMBERS".StartsWith(arg2)) { switches.noNumbers = true; recognised = true; }
                    if ("LIST".StartsWith(arg2)) { switches.list = true; recognised = true; }
                    if ("BLIST".StartsWith(arg2)) { switches.blist = true; recognised = true; }
                    if (arg2 == "?" || "HELP".StartsWith(arg2)) { help(); Environment.Exit(0); }

                    if (!recognised)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine("Option " + arg.ToLowerInvariant() + " not recognised");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
                else
                {
                    if (switches.inputfile.Length == 0) // This is where we pick up the filename if not already found
                        switches.inputfile = arg;
                    else
                        if (!switches.save)
                        {
                            switches.outputfile = arg;
                            switches.save = true;
                        }
                }
            }
            // no filename found:
            if (switches.inputfile.Length == 0)
            {
                Console.Error.WriteLine("Error: No input filename found");
                help();
                Environment.Exit(0);
            }
            if (switches.outputfile.Length == 0)
            {
                switches.save = false;
            }
            //else Console.WriteLine($"={outputfile}");
        }
        static void savefile(TokeniserCommandSwitches switches, BasToolsEngine engine)
        {
            if (string.IsNullOrEmpty(switches.outputfile))
            {
                Console.Error.WriteLine("Filename for save is missing");
                return;
            }
            if (engine.CurrentListing.Lines.Count == 0)
            {
                Console.Error.WriteLine("No program lines to save");
                return;
            }

            using var bw = new BinaryWriter(File.OpenWrite(switches.outputfile));

            if (!switches.Z80)
            {
                bw.Write('\r');
            }

            foreach (ProgramLine line in engine.CurrentListing.Lines)
            {
                int linenum = line.LineNumber;
                byte ln_hi = (byte)(linenum >> 8);   // top 8 bits
                byte ln_lo = (byte)(linenum & 0xFF); // bottom 8 bits
                byte[] linebody = line.TokenisedLine;
                if (linebody.Length > 251)
                {
                    Console.WriteLine($"Line {linenum} too long by {251 - linebody.Length} bytes.");
                    continue;
                }
                byte linelen = (byte)(linebody.Length + 4);

                if (switches.Z80)
                {
                    bw.Write(linelen);
                    bw.Write(ln_lo);
                    bw.Write(ln_hi);
                }
                else
                {
                    bw.Write(ln_hi);
                    bw.Write(ln_lo);
                    bw.Write(linelen);
                }
                bw.Write(linebody);
                bw.Write('\r');
            }
            // End of program marker
            if (switches.Z80)
            {
                bw.Write('\0');
                bw.Write(0xFF);
                bw.Write(0xFF);
            }
            else
            {
                bw.Write(0xFF);
            }
            bw.Close();
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
