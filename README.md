BasTools is designed to modernise and preserve BBC BASIC development workflows. 
It provides accurate pretty‑printing, token analysis, and structural inspection for 6502 and ARM and R.T. Russell BBC BASIC sources, using a deterministic parsing engine shared across all tools.

The tools will eventually include full analysis and editing. The most complete at the moment is BasList, a command line utility to list tokenised BASIC programs from the BBC Micro, RiscOS machines and R.T. Russell's Z80 programs, with optional comprehensive pretty-printing. Cross-platform (Windows, Mac, Linux).

# BasList vs 2.0.0 for BasTools 1.0.0

Lists a BBC BASIC program file in Acorn or R.T. Russell (Z80) format with full formatting options and syntax colouring

## SYNTAX

BasList [/file=]filename ([from line] [to line]) | [line,line]) [Options] ([IF ...] | [IFX ...] | [LIST ...])

BasList [/file=]filename [/V] [/notBasicV] [/addnumbers] [/] [/align] [/indent] [/nonumbers] [/noformat] [/splitlines] [/columns] [(/cls | /clear)] [/bare] [/pause] [/prettyprint] [/dark | /light]

BasList /? or /h - help


## EXAMPLES

>BasList Program ,200	- List up to line 200
>
>BasList Program 1000,	- List from line 1000
>
>BasList Program 200,1000	- List from line 200 to 1000
>
>BasList Program 200 1000	- List from line 200 to 1000
>
>BasList Program IF PRINTTAB	- List only lines containing the specified text
>
>BasList Program IFX	- IF, but respecting spaces and case
>
>BasList Program LIST PROCinp	- Displays PROCinp definition
>
>BasList Program LIST FNadd	- Displays FNadd definition
>
>	Note that there may be multiple PROCs and FNs specified, e.g. LIST PROCinp FNadd

# Features

- Accurate detokenisation of BBC BASIC (6502, ARM and Z80)

- Code indentation options (loops and structures, procedures and functions)

- Split long lines with full identation capabilities

- Syntax colouring option

- Assembly handling - align in columns option

- Shared parsing engine across all tools

- Installer for Windows (Inno Setup)

# Contributing

Contributions, bug reports, and suggestions are welcome. 
Please open an issue, submit a pull request or contact through the website.

# Website

https://www.rowlandsoftware.com/BBC/BasList/help.php?id=998
