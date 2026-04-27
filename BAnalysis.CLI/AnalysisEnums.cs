using System;
using System.Collections.Generic;
using System.Text;

namespace BasAnalysis.CLI
{
    public enum SymbolKind
    {
        StaticInt,
        IntVar,
        RealVar,
        StringVar,
        RealArray,
        IntArray,
        StringArray,
        LiteralString,
        Fn,
        Proc,
        Label,
        FuckKnows
    }
    public enum SymbolReadOrWrite
    {
        Assigned,
        Referenced
    }
    public enum SymbolContext
    {
        Global,     // global var
        Local,      // LOCAL var
        Parameter,  // Part of formal parameters (so in effect local)
        Call,       // Call to FN/PROC
        NA,         // Not applicable, currently a DEF
        TBD         // placeholder
    }
    public enum ProcedureType
    {
        Proc,
        Fn,
        Root
    }
    public class SymbolUse
    {
        public int LineNumber { get; init; }
        public SymbolContext symbolContext { get; init; }       // Global, Local, Parameter, Call etc
        public SymbolReadOrWrite symbolReadWrite { get; init; } // Assigned / Referenced
        public string? ParentName { get; set; }                 // null = global
        public ProcedureType? ParentProcedureType { get; set; } // .Root = global
    }
    public class SymbolInfo
    {
        public string Name { get; init; } = "";
        public SymbolKind Kind { get; init; }   // IntVar, RealVar, StringVar, Array, PROC, FN, LocalVar, etc.

        public int AssignedCount { get; set; }
        public int ReferencedCount { get; set; }

        public List<SymbolUse> Uses { get; } = new();   // line numbers, contexts, parent
    }
}