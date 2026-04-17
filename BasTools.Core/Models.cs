using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text;

namespace BasTools.Core
{
    //***************** ProgInfo *****************
    // Just a little collection of information. Also used to pass some information
    
    public class ProgInfo
    {
        public bool Z80;
        public bool BasicV;
        public int NumberOfLines;
        public int LengthInBytes;
        public string Filename;
        public string BasicDialect =>
            Z80 ? "Z80 Basic"
                : BasicV ? "Acorn Basic V"
                    : "Acorn Basic I–IV";
        public ProgInfo(bool z80, bool basicV, string filename)
        {
            Z80 = z80;
            BasicV = basicV;
            NumberOfLines = 0;
            LengthInBytes = 0;
            Filename = filename;
        }
    }

    //***************** SemanticTags *****************
    public enum SemanticTypes // not used
    {
        Keyword,
        IndentingKeyword,
        OutdentingKeyword,
        InOutKeyword,
        BuiltInFn,
        StringLiteral,
        Number,
        HexNumber,
        BinaryNumber,
        Variable,
        StaticInteger,
        RemText,
        AssemblerComment,
        StarCommand,
        EmbeddedData,
        Proc,
        Function,
        Label,
        Register,
        Mnemonic,
        LineNumber,
        Operator,
        IndirectionOperator,
        ImmediateOperator,
        StatementSep,
        ListSep,
        OpenBracket,
        CloseBracket,
        Reset
    }
    public static class SemanticTags
    {
        // These are the literal tags you insert into the output
        public const string Keyword = "{=keyword}";
        public const string IndentingKeyword = "{=indentingkeyword}";
        public const string OutdentingKeyword = "{=outdentingkeyword}";
        public const string InOutKeyword = "{=inout_keyword}";
        public const string BuiltInFn = "{=builtinfn}";
        public const string StringLiteral = "{=string}";
        public const string Number = "{=number}";
        public const string HexNumber = "{=hexnumber}";
        public const string BinaryNumber = "{=binarynumber}";
        public const string Variable = "{=var}";
        public const string StaticInteger = "{=staticint}";
        public const string RemText = "{=remtext}";
        public const string AssemblerComment = "{=assemcomment}";
        public const string StarCommand = "{=starcommand}";
        public const string EmbeddedData = "{=embeddeddata}";
        public const string ProcName = "{=proc}";
        public const string FunctionName = "{=fn}";
        public const string Label = "{=label}";
        public const string Register = "{=register}";
        public const string Mnemonic = "{=mnemonic}";
        public const string LineNumber = "{=linenumber}";
        public const string Operator = "{=operator}";
        public const string IndirectionOperator = "{=indirectionoperator}";
        public const string ImmediateOperator = "{=immediateoperator}";        
        public const string StatementSep = "{=statementsep}";
        public const string ListSep = "{=listsep}";
        public const string OpenBracket = "{=openbracket}";
        public const string CloseBracket = "{=closebracket}";
        public const string Reset = "{/}";
    }
    //
    //***************** Listing Classes and Records *****************
    //
    public record class ProgramLine
    {
        // Stage 0: Raw input
        public int LineNumber { get; set; }
        public byte[] TokenisedLine { get; set; } = Array.Empty<byte>();

        // Stage 1: Detokeniser
        public string NoSpacesLine { get; set; } = "";
        public string PlainDetokenisedLine { get; set; } = "";
        public string TaggedLine { get; set; } = "";

        // Stage 3: Formatter
        public string FormattedLineNumber { get; set; }
        public string FormattedPlain { get; set; } = "";
        public string FormattedTagged { get; set; } = "";

        // Flags and indent level
        public int IndentLevel { get; set; }
        public bool IsDef {  get; set; }
        public bool IsInDef { get; set; }
        public int DefIndent
        {
            get => IsInDef ? 1 : 0;
        }
        // Properties for Assembler
        public bool InAsm { get; set; } = false;
        public bool IsArm { get; set; } = false;
        // Properties needed for SplitLines
        public FormatterState fstate;
        public ProgramLine (ProgramLine other)
        {
            LineNumber = other.LineNumber;
            IndentLevel = other.IndentLevel;
            IsDef = other.IsDef;
            IsInDef = other.IsInDef;
            InAsm = other.InAsm;
            IsArm = other.IsArm;
        }
    }
    public record Listing(List<ProgramLine> Lines);
    internal record LineRecord(
        int linenumber,
        byte[] lineContent
    );
    public record Token(
        string tag,
        string value,
        bool isLast
    );

    //***************** ParserState used by Detokeniser *****************
    internal class ParserState
    {
        public byte[] Data;
        public int Ptr;
        public bool Z80;
        public int LineCount;
        public bool InAsm;
        public bool InIfCondition;  // Evaluating the IF <condition>
        public bool InIf;           // All of line following IF (but before ELSE)
        public int IfParenDepth;
        public bool ExprComplete;

        public List<string> DirectiveParams = new();
        public ParserState()
        {
            Data = Array.Empty<byte>();
            Z80 = false;
            Ptr = 0;
            LineCount = 0;
            InAsm = false;
            InIfCondition = false;
            InIf = false;
            IfParenDepth = 0;
            ExprComplete = false;
        }
    }

    //***************** FormatterState *****************
    public class FormatterState
    {
        //public bool Z80;
        public int LineCount;
        private int _indent;
        public int PendingIndent;
        public int MultiLineIfDepth;
        public bool InIfCondition;  // Evaluating the IF <condition>
        public bool InIf;           // All of line following IF (but before ELSE)
        public bool LoopInIf;
        public int LoopsOnThisLine;
        public bool InDefInition;
        public bool IsDef;
        public bool SeenFirstWhen;
        public FormatterState()
        {
            LineCount = 0;
            Indent = 0;
            PendingIndent = 0;
            MultiLineIfDepth = 0;
            InIfCondition = false;
            InIf = false;
            LoopInIf = false;
            LoopsOnThisLine = 0;
            IsDef = false;
            InDefInition = false;
            SeenFirstWhen = false;
        }
        public FormatterState(FormatterState other) : this()
        {
            LineCount = other.LineCount;
            Indent = other.Indent + other.PendingIndent;
            MultiLineIfDepth = other.MultiLineIfDepth;
            InIfCondition = other.InIfCondition;
            InIf = other.InIf;
            LoopInIf = other.LoopInIf;
            IsDef = other.IsDef;
            InDefInition = other.InDefInition;
            SeenFirstWhen = other.SeenFirstWhen;
        }
        public int Indent
        {
            get => _indent;
            set => _indent = value < 0 ? 0 : value;
        }
    }

    //***************** FormattingOptions *****************
    // Just a subset of switches. See CommandSwitches.copyToFormatOptions()
    public class FormattingOptions
    {
        public bool FlgAddNums;
        public bool FlgIndent;
        public bool FlgEmphDefs;
        public bool Align;
        public bool NoFormat;
        public bool Bare;
        public bool SplitLines;
        public bool AssemblerColumns;
        public int ExtraColumnWidth;
        public FormattingOptions()
        {
            FlgAddNums = false;
            FlgIndent = false;
            FlgEmphDefs = false;
            Align = false;
            NoFormat = false;
            Bare = false;
            SplitLines = false;
            AssemblerColumns = false;
            ExtraColumnWidth = 10;
        }
    }
}