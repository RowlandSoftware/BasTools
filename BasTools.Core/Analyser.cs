using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BasTools.Core
{
    public static class Analyser
    {
        internal static void Analyse(BasToolsEngine engine, ref bool analyzed)
        {
            bool expectingAssignmentTarget;
            HashSet<string> localVars = new();
            HashSet<string> parameters = new();
            engine.DimLines.Clear();

            // Tracking which procedure or function (inc root) we are "in"
            ProcedureType procedureType = ProcedureType.Root;
            string procedureName = "$"; // root

            foreach (ProgramLine line in engine.CurrentListing.Lines)
            {
                // line level state
                expectingAssignmentTarget = true; // start of line is start of statement...
                bool readOrDim = false;

                List<Token> tokens = BasToolsEngine.WalkTagged(line.TaggedLine).ToList();

                for (int i = 0; i < tokens.Count; i++)
                {
                    SymbolContext context = SymbolContext.TBD; // Global, Local, Parameter, Call, NA, TBD
                    var tok = tokens[i];

                    if (tok.tag == SemanticTags.StatementSep ||
                        (tok.tag == SemanticTags.Keyword && (tok.value is "REPEAT" or "ELSE")))
                    {
                        expectingAssignmentTarget = true;
                        readOrDim = false;
                        continue;
                    }

                    if (tok.tag == SemanticTags.Keyword && tok.value == "READ") // everything after READ is an assignment without =
                    {
                        expectingAssignmentTarget = true;
                        readOrDim = true;
                        continue;
                    }

                    if (tok.tag == SemanticTags.Keyword && (tok.value is "LOCAL" or "DIM")) // variables in the LOCAL list count as assignments (initialised to 0 or "")
                    {
                        bool flgLocal = (tok.value == "LOCAL"); // if false, is DIM

                        if (tok.value == "DIM") // everything after DIM is an assignment without =
                        {
                            expectingAssignmentTarget = true;
                            readOrDim = true;
                        }

                        // read the list following DIM or LOCAL
                        int j = i + 1;
                        while (j < tokens.Count && tokens[j].tag != SemanticTags.StatementSep)
                        {
                            if (tokens[j].tag is SemanticTags.Variable) // we handle arrays below
                            {
                                if (flgLocal)
                                {
                                    localVars.Add(tokens[j].value);                    // build list of LOCAL vars
                                    RecordUse(engine, SemanticTags.Variable, tokens[j].value, line.LineNumber,
                                        SymbolReadOrWrite.Assigned, SymbolContext.Local,
                                        procedureName, procedureType);                  // and record an assignment
                                }
                                else
                                {
                                    // add to DIM list
                                    if (!engine.DimLines.TryGetValue(tokens[j].value, out var list))
                                    {
                                        list = new List<DimInfo>();
                                        engine.DimLines[tokens[j].value] = list;
                                    }
                                    list.Add(new DimInfo(line.LineNumber, IsLocal(tokens[j].value, localVars) == SymbolContext.Local));

                                    RecordUse(engine, SemanticTags.Variable, tokens[j].value, line.LineNumber,
                                        SymbolReadOrWrite.Assigned, IsLocal(tokens[j].value, localVars),
                                        procedureName, procedureType);                  // and record an assignment
                                }
                            }
                            else if (tokens[j].tag == SemanticTags.Array)
                            {
                                string fullName = tokens[j].value + "()";
                                if (flgLocal)
                                {
                                    localVars.Add(fullName);                            // add array to list of LOCAL vars
                                    RecordUse(engine, SemanticTags.Array, fullName, line.LineNumber,
                                        SymbolReadOrWrite.Assigned, SymbolContext.Local,
                                        procedureName, procedureType);                  // and record an assignment
                                }
                                else
                                {
                                    if (!engine.DimLines.TryGetValue(fullName, out var list))
                                    {
                                        list = new List<DimInfo>();
                                        engine.DimLines[fullName] = list;
                                    }
                                    list.Add(new DimInfo(line.LineNumber, IsLocal(fullName, localVars) == SymbolContext.Local));

                                    RecordUse(engine, SemanticTags.Array, fullName, line.LineNumber,
                                        SymbolReadOrWrite.Assigned, IsLocal(fullName, localVars),
                                        procedureName, procedureType);                  // and record an assignment
                                }
                            }
                            j++;
                        }
                        i = j; // Skip past or they get counted as references too
                        continue;
                    }

                    if (tok.tag == SemanticTags.IndentingKeyword && tok.value == "FOR") // without this, FOR i=0 TO 4:NEXT would be an assignment without reference
                    {
                        Token? nextVar = PeekNextNonSpaceToken(tokens, i);
                        if (nextVar != null && (nextVar.tag == SemanticTags.Variable ||
                            nextVar.tag == SemanticTags.Array)) // anything else is illegal as control variable
                        {
                            string suffix = (nextVar.tag == SemanticTags.Array ? "()" : "");
                            RecordUse(engine,nextVar.tag,
                                nextVar.value + suffix,
                                line.LineNumber,
                                SymbolReadOrWrite.Referenced,                       // synthetic reference
                                SymbolContext.Local, procedureName, procedureType
                            );
                        }
                    }

                    if (tok.tag == SemanticTags.Variable || tok.tag == SemanticTags.Array)
                    {
                        string suffix = (tok.tag == SemanticTags.Array ? "()" : "");
                        string fullname = tok.value + suffix;

                        // Derive Context for variable
                        if (procedureType == ProcedureType.Root)
                        {
                            context = SymbolContext.Global;
                        }
                        else if (parameters.Contains(fullname))
                        {
                            context = SymbolContext.Parameter;
                        }
                        else if (localVars.Contains(fullname))
                        {
                            context = SymbolContext.Local;
                        }
                        else
                        {
                            // Variables inside a PROC/FN but not declared LOCAL or parameter are GLOBAL
                            context = SymbolContext.Global;
                        }

                        // Look ahead to see if this is an assignment
                        Token? next = PeekNextNonSpaceToken(tokens, i);
                        // ... and if array skip over the brackets (..)
                        if (tok.tag == SemanticTags.Array && next.tag == SemanticTags.OpenBracket)
                        {
                            int k = i;
                            while (next.tag != SemanticTags.CloseBracket)
                            {
                                next = PeekNextNonSpaceToken(tokens, k++);
                            }
                            next = PeekNextNonSpaceToken(tokens, k);
                        }

                        bool isAssignment = expectingAssignmentTarget &&
                                            next?.tag == SemanticTags.Operator &&
                                            next?.value == "=";
                        if (readOrDim) isAssignment = true;

                        RecordUse(engine,tok.tag, fullname, line.LineNumber,
                            isAssignment ? SymbolReadOrWrite.Assigned : SymbolReadOrWrite.Referenced,
                           context, procedureName, procedureType);

                        expectingAssignmentTarget = false;
                        continue;
                    }

                    if (tok.tag is SemanticTags.ProcName or SemanticTags.FunctionName)
                    {
                        if (line.IsDef)
                        {
                            if (tok.tag == SemanticTags.ProcName)
                                procedureType = ProcedureType.Proc;
                            else
                                procedureType = ProcedureType.Fn;

                            procedureName = tok.value;

                            // We record the DEF as a 'use' to record line number etc
                            RecordUse(engine,tok.tag, tok.value, line.LineNumber, SymbolReadOrWrite.Assigned,
                                SymbolContext.NA, "", procedureType); // here, procedure type = DEFPROC or DEFFN

                            localVars.Clear();
                            parameters.Clear();

                            // Now parse parameters
                            int j = i + 1;
                            int parenDepth = 0;

                            while (j < tokens.Count && tokens[j].tag != SemanticTags.StatementSep)
                            {
                                if (tokens[j].value == "(")
                                {
                                    parenDepth++;
                                    j++;
                                    continue;
                                }

                                if (tokens[j].value == ")")
                                {
                                    parenDepth--;
                                    if (parenDepth == 0)
                                        break;
                                    j++;
                                    continue;
                                }
                                // Parameter at depth 1
                                if (parenDepth == 1)
                                {
                                    if (tokens[j].tag == SemanticTags.Variable)
                                    {
                                        parameters.Add(tokens[j].value);
                                        localVars.Add(tokens[j].value);
                                    }
                                    else if (tokens[j].tag == SemanticTags.Array)
                                    {
                                        string full = tokens[j].value + "()";
                                        parameters.Add(full);
                                        localVars.Add(full);
                                    }
                                }
                                j++;
                            }
                            if (parenDepth != 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                string missed = parenDepth > 0 ? ")" : "(";
                                Console.WriteLine($"Warning: Missing {missed} in parameter list at line {line.LineNumber}");
                                Console.ResetColor();
                            }
                            // scanned parameters, so...
                            i = j;
                            // Now record a synthetic assignment for each
                            foreach (var p in parameters)
                            {
                                // Synthetic assignment at line of DEF
                                RecordUse(engine,SemanticTags.Variable,
                                    p,
                                    line.LineNumber,
                                    SymbolReadOrWrite.Assigned,
                                    SymbolContext.Parameter,
                                    procedureName,
                                    procedureType
                                );
                            }
                        }
                        else // is a PROC or FN *call*
                        {
                            RecordUse(engine,tok.tag, tok.value, line.LineNumber,
                            SymbolReadOrWrite.Referenced,
                            SymbolContext.Call, procedureName, procedureType);  // here, procedure name and type refer to the parent proc
                        }
                        continue;
                    }
                    if (tok.tag is SemanticTags.StringLiteral)
                    {
                        RecordUse(engine, tok.tag, tok.value, line.LineNumber,
                            SymbolReadOrWrite.Assigned,
                            SymbolContext.NA, procedureName, procedureType);

                        expectingAssignmentTarget = false;
                        continue;
                    }
                    if (tok.tag is SemanticTags.Label)
                    {
                        // look at LOCAL and parameters lists (w/o .)
                        SymbolContext labcontext = SymbolContext.Assembler;
                        if (localVars.Contains(tok.value[1..]))
                            labcontext = SymbolContext.Local;
                        if (parameters.Contains(tok.value[1..]))
                            labcontext = SymbolContext.Parameter;

                        RecordUse(engine, tok.tag, tok.value, line.LineNumber,
                        SymbolReadOrWrite.Assigned,                     // .label - all assigned. References tracked in variables
                        labcontext, procedureName, procedureType);      // Context should show if local or parameter, otherwise Assembler

                        expectingAssignmentTarget = false;
                        continue;
                    }

                    if (tok.tag == SemanticTags.Operator && tok.value == "=")
                    {
                        // After '=', everything is a reference
                        expectingAssignmentTarget = false;
                        continue;
                    }
                }
                // Reset only when outside any DEF block
                if (!line.IsInDef && !line.IsDef)
                {
                    procedureType = ProcedureType.Root;
                    procedureName = "$";
                }
            }
            Console.Write($"Analysed {engine.Symbols.Count} unique tokens\n");
            analyzed = true;
            return;
        }
        static void RecordUse(BasToolsEngine engine, string tag, string name, int line,
            SymbolReadOrWrite readwrite, SymbolContext context,
            string currentProcName, ProcedureType procedureType) // , SymbolInfo parentSymbolInfo
        {
            if (tag == SemanticTags.StringLiteral && string.IsNullOrWhiteSpace(name))
                return;

            SymbolKind kind = BasToolsEngine.InferKind(tag, name);

            if (!engine.Symbols.TryGetValue(kind + ":" + name, out var sym)) // If first sight, create SymbolInfo with key "<kind>:<name>"
            {
                sym = new SymbolInfo { Name = name, Kind = kind };
                engine.Symbols.Add(kind + ":" + name, sym);
            }

            if (readwrite == SymbolReadOrWrite.Assigned)
                sym.AssignedCount++;
            else
                sym.ReferencedCount++;

            SymbolUse symbolUse = new SymbolUse
            {
                CalledName = name,
                CalledKind = kind,
                LineNumber = line,
                symbolReadWrite = readwrite,
                symbolContext = context,
                ParentName = currentProcName,
                ParentProcedureType = procedureType
            };
            sym.Uses.Add(symbolUse);
        }
        public static SymbolContext IsLocal(string tokenValue, HashSet<string> localVars)
        {
            return localVars.Contains(tokenValue) ? SymbolContext.Local : SymbolContext.Global;
        }
        public static Token? PeekNextNonSpaceToken(List<Token> tokens, int index)
        {
            for (int i = index + 1; i < tokens.Count; i++)
            {
                var t = tokens[i];

                // Skip null-tag whitespace tokens
                if (t.tag == null) // && string.IsNullOrWhiteSpace(t.value))
                    continue;

                return t;
            }
            return null;
        }
    }
}
