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

    static readonly Dictionary<string, (bool before, bool after)> SpacingRules =
    new()
    {
        { SemanticTags.Operator, (true, true) },
        { SemanticTags.Keyword,  (true, true) },
        { SemanticTags.Comma,    (false, true) },
        { SemanticTags.Colon,    (false, true) },
        { SemanticTags.Identifier, (false, false) },
        // etc.
    };
        //var(before, after) = BasSpacingRules.Rules.GetValueOrDefault(tag, (false, false));

    //***************** SemanticTags *****************
    public enum SemanticTypes
    {
        Keyword,
        IndentingKeyword,
        OutdentingKeyword,
        InOutKeyword,
        StringLiteral,
        Number,
        HexNumber,
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
        Reset
    }
    public static class SemanticTags
    {
        // These are the literal tags you insert into the output
        public static string Keyword => "{=keyword}";
        public static string IndentingKeyword => "{=indentingkeyword}";
        public static string OutdentingKeyword => "{=outdentingkeyword}";
        public static string InOutKeyword => "{=inout_keyword}";
        public static string StringLiteral => "{=string}";
        public static string Number => "{=number}";
        public static string HexNumber => "{=hexnumber}";
        public static string Variable => "{=var}";
        public static string StaticInteger => "{=staticint}";
        public static string RemText => "{=remtext}";
        public static string AssemblerComment => "{=assemcomment}";
        public static string StarCommand => "{=starcommand}";
        public static string EmbeddedData => "{=embeddeddata}";
        public static string Proc => "{=proc}";
        public static string Function => "{=fn}";
        public static string Label => "{=label}";
        public static string Register => "{=register}";
        public static string Mnemonic => "{=mnemonic}";
        public static string LineNumber => "{=linenumber}";
        public static string Operator => "{=operator}";
        public static string Reset => "{/}";
    }

    //***************** Listing Classes and Records *****************

    // ----------- The New Model -----------
   /* public record class ProgramLine
    {
        // Stage 0: Raw input
        public int LineNumber { get; set; }
        public byte[] TokenisedLine { get; set; } = Array.Empty<byte>();

        // Stage 1: Detokeniser
        public string NoSpacesLine { get; set; } = "";
        public string PlainDetokenisedLine { get; set; } = "";
        public string TaggedLine { get; set; } = "";

        // Stage 2: Lexer
        public List<Token> Tokens { get; set; } = new();

        // Stage 3: Annotator
        public List<AnnotatedToken> AnnotatedTokens { get; set; } = new();

        // Stage 4: Formatter
        public string FormattedPlain { get; set; } = "";
        public string FormattedTagged { get; set; } = "";
        public int IndentLevel { get; set; }
    }
    public record Listing(List<ProgramLine> Lines);*/

    // --------- Old Models TO GO ---------
    public record Listing(
    List<ProcessedLine> ProgramLines //, List<Token> Tokens
    );
    public record class ProcessedLine
    {
        public int LineNumber { get; set; }
        public byte[] TokenisedLine { get; set; } = Array.Empty<byte>();
        public string NoSpacesLine { get; set; } = "";
        public string PlainDetokenisedLine { get; set; } = "";
        public string TaggedLine { get; set; } = "";
    }
    
    public record FormattedListing(
    List<FormattedLine> FormattedLines
    );
    public record class FormattedLine
    {
        public int LineNumber { get; set; }
        public string FormattedLineNumber { get; set; }
        public int IndentLevel { get; set; }
        public string PlainLineOrSegment { get; set; } = "";
        public string TaggedLineLineOrSegment { get; set; } = "";
    }
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
        public FormatterState()
        {
            Z80 = false;
            LineCount = 0;
            Indent = 0;
            PendingIndent = 0;
            fMultiLineIf = false;
        }
        public FormatterState(FormatterState other)
        {
            Z80 = other.Z80;
            LineCount = other.LineCount;
            Indent = other.Indent;
            PendingIndent = other.PendingIndent;
            fMultiLineIf |= other.fMultiLineIf;
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
