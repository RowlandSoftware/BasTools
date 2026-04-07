using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text;

namespace BasTools.Core
{
    //***************** ProgInfo *****************
    
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

    //***************** Listing Classes and Records *****************

    // ----------- The New Model -----------
    public record class ProgramLine
    {
        // Stage 0: Raw input
        public int LineNumber { get; set; }
        public byte[] TokenisedLine { get; set; } = Array.Empty<byte>();

        // Stage 1: Detokeniser
        public string NoSpacesLine { get; set; } = "";
        public string PlainDetokenisedLine { get; set; } = "";
        public string TaggedLine { get; set; } = "";

        // Stage 2: Lexer
        //public List<Token> Tokens { get; set; } = new();

        // Stage 3: Formatter
        public string FormattedLineNumber { get; set; }
        public string FormattedPlain { get; set; } = "";
        public string FormattedTagged { get; set; } = "";
        public int IndentLevel { get; set; }
        public bool IsDef {  get; set; }
        public bool IsInDef { get; set; }
        public int DefIndent
        {
            get => IsInDef ? 1 : 0;
        }
    }
    public record Listing(List<ProgramLine> Lines);
    internal record LineRecord(
        int linenumber,
        byte[] lineContent
    );
    //***************** ParserState *****************
    internal class ParserState
    {
        public byte[] Data;
        public int Ptr;
        public bool Z80;
        public int LineCount;
        public bool InAsm;

        public List<string> DirectiveParams = new();
        public ParserState()
        {
            Data = Array.Empty<byte>();
            Z80 = false;
            Ptr = 0;
            LineCount = 0;
            InAsm = false;
        }
    }

    //***************** FormatterState *****************
    internal class FormatterState
    {
        public bool Z80;
        public int LineCount;
        private int _indent;
        public int PendingIndent;
        public bool fMultiLineIf;
        public bool InIfCondition;
        public bool InDefInition;
        public bool IsDef;
        public bool SeenFirstWhen;
        public FormatterState()
        {
            Z80 = false;
            LineCount = 0;
            Indent = 0;
            PendingIndent = 0;
            fMultiLineIf = false;
            InIfCondition = false;
            IsDef = false;
            InDefInition = false;
            SeenFirstWhen = false;
        }
        public FormatterState(FormatterState other)
        {
            Z80 = other.Z80;
            LineCount = other.LineCount;
            Indent = other.Indent;
            PendingIndent = other.PendingIndent;
            fMultiLineIf = other.fMultiLineIf;
            InIfCondition = other.InIfCondition;
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
    public class FormattingOptions
    {
        public bool FlgAddNums;
        public bool FlgIndent;
        public bool FlgEmphDefs;
        public bool Align;
        public bool NoSpaces;
        public bool Bare;
        public bool BreakApart;
        public FormattingOptions()
        {
            FlgAddNums = false;
            FlgIndent = false;
            FlgEmphDefs = false;
            Align = false;
            NoSpaces = false;
            Bare = false;
            BreakApart = false;
        }
    }
}