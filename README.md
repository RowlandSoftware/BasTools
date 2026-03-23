The tools will eventually include BBC Basic analysis and editing. The most complete at the moment is BasList, a command line utility to list tokenised BASIC programs from the BBC Micro, RiscOS machines and R.T. Russell's Z80 programs. Cross-platform (Windows, Max, Linux).

# BasList vs 1.1.0

Lists a BBC BASIC program file in Acorn or R.T. Russell (Z80) format

## SYNTAX

BasList [/file=]filename ([from line] [to line]) | [line,line]) [Options] ([IF ...] | [IFX ...] | [LIST ...])

BasList [/file=]filename [/V] [/addnumbers] [/auto] [/align] [/indent] [/nonumbers] [/nospaces] [/bare] [/pause] [/prettyprint] [/dark | /light]

BasList /? or /h - help


  /V	Allow BASIC V keywords. See Compatibility below.
  
  /addnumbers	Show line numbers (starting at 10, in tens) when none in the program (Z80 only)
  
  /align	Right-align line numbers
	
  Without /align (line numbers displayed with one following space):
  
>		10 REM An example program
>
>		100 MODE 7
>
>		1000 DEFPROCmain
	
With /align (line numbers ranged right and one following space):
		   
>       10 REM An example program
>
>      100 MODE 7
>
>     1000 DEFPROCmain
  
  /indent	Indent listing of loops (unless /nospaces specified).
	
  The equivalent of LISTO7 in BASIC IV
  
  /nonumbers	Omits line numbers
  
  /nospaces	Omits spaces after line numbers. (Also cancels /indent.)
  
>		10REM An example program
>    
>		100MODE 7
>    
>		1000DEFPROCmain
  
  /bare	Omits additional messages
  
  /prettyprint	Adds additional spaces and syntax colouring
  
  /dark	Dark mode - black background (default)
  
  /light	Light mode - white background

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
