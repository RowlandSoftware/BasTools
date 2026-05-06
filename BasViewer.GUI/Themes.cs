using BasTools.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace BasViewer.GUI
{
    internal class Themes
    {
        private static readonly Dictionary<string, Dictionary<string, string>> _themes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Dark"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["font"] = "Consolas,Cascadia Code,Menlo,Monospace",
                ["background"] = "#111",
                ["foreground"] = "#eee",
                ["linenumber_bg"] = "DarkSlateGray",
                ["linenumber_fg"] = "Silver",

                ["keyword"] = "color:RoyalBlue; font-weight: bold;",
                ["indentingkeyword"] = "color:RoyalBlue; font-weight: bold;",
                ["outdentingkeyword"] = "color:RoyalBlue; font-weight: bold;",
                ["inout_keyword"] = "color:RoyalBlue; font-weight: bold;",
                ["builtinfn"] = "color:RoyalBlue; font-weight: bold;",
                ["string"] = "color:MediumSeaGreen;",
                ["number"] = "color:white;",
                ["hexnumber"] = "color:white;",
                ["binarynumber"] = "color:white;",
                ["var"] = "color:MediumOrchid;",
                ["array"] = "color:BlueViolet;",
                ["staticint"] = "color:MediumPurple;",
                ["remtext"] = "color:#FFF777; font-style:italic;",
                ["assemcomment"] = "color:#FFF777; font-style:italic;",
                ["starcommand"] = "color:white;",
                ["embeddeddata"] = "color:white;",
                ["proc"] = "color:Turquoise;",
                ["fn"] = "color:Turquoise;",
                ["label"] = "color:MediumOrchid;",
                ["register"] = "color:GreenYellow;",
                ["mnemonic"] = "color:RoyalBlue;",
                ["linenumber"] = "color:gray",
                ["operator"] = "color:Red;",
                ["="] = "color:red",
                ["indirectionoperator"] = "color:white;",
                ["immediateoperator"] = "color:Red;",
                ["statementsep"] = "color:Orange;",
                ["listsep"] = "color:white;",
                ["openbracket"] = "color:white;",
                ["closebracket"] = "color:white;",
            },

            ["Light"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["font"] = "Consolas,Cascadia Code,Menlo,Monospace",
                ["background"] = "#ffffff",
                ["foreground"] = "Snow",
                ["linenumber_bg"] = "#dddddd",
                ["linenumber_fg"] = "DarkSlateGray",

                ["keyword"] = "color:Blue; font-weight: bold;",
                ["indentingkeyword"] = "color:Blue; font-weight: bold;",
                ["outdentingkeyword"] = "color:Blue; font-weight: bold;",
                ["inout_keyword"] = "color:Blue; font-weight: bold;",
                ["builtinfn"] = "color:Blue; font-weight: bold;",
                ["string"] = "color:Green;",
                ["number"] = "color:Black;",
                ["hexnumber"] = "color:Black;",
                ["binarynumber"] = "color:Black;",
                ["var"] = "color:MediumOrchid;",
                ["array"] = "color:BlueViolet;",
                ["staticint"] = "color:MediumPurple;",
                ["remtext"] = "color:Gold; font-style:italic;",
                ["assemcomment"] = "color:Gold; font-style:italic;",
                ["starcommand"] = "color:Black;",
                ["embeddeddata"] = "color:Black;",
                ["proc"] = "color:Steelblue;",
                ["fn"] = "color:Steelblue;",
                ["label"] = "color:MediumOrchid;",
                ["register"] = "color:Red;",
                ["mnemonic"] = "color:Blue;",
                ["linenumber"] = "color:gray",
                ["operator"] = "color:Red;",
                ["="] = "color:red",
                ["indirectionoperator"] = "color:Black;",
                ["immediateoperator"] = "color:Black;",
                ["statementsep"] = "color:Orange;",
                ["listsep"] = "color:Black;",
                ["openbracket"] = "color:Black;",
                ["closebracket"] = "color:Black;",
            },

            ["Retro"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["font"] = "Consolas,Cascadia Code,Menlo,Monospace",
                ["background"] = "#0000FF",
                ["foreground"] = "#FFFFFF",
                ["linenumber_bg"] = "Black",
                ["linenumber_fg"] = "White",

                ["keyword"] = "color:White; font-weight: bold;",
                ["indentingkeyword"] = "color:White; font-weight: bold;",
                ["outdentingkeyword"] = "color:White; font-weight: bold;",
                ["inout_keyword"] = "color:White; font-weight: bold;",
                ["builtinfn"] = "color:White; font-weight: bold;",
                ["string"] = "color:LimeGreen; font-weight: bold;",
                ["number"] = "color:white;",
                ["hexnumber"] = "color:white;",
                ["binarynumber"] = "color:white;",
                ["var"] = "color:Magenta;",
                ["array"] = "color:Violet;",
                ["staticint"] = "color:MediumPurple;",
                ["remtext"] = "color:#FFF777; font-style:italic;",
                ["assemcomment"] = "color:#FFF777; font-style:italic;",
                ["starcommand"] = "color:white;",
                ["embeddeddata"] = "color:white;",
                ["proc"] = "color:Turquoise;",
                ["fn"] = "color:Turquoise;",
                ["label"] = "color:MediumOrchid;",
                ["register"] = "color:GreenYellow;",
                ["mnemonic"] = "color:White;",
                ["linenumber"] = "color:gray",
                ["operator"] = "color:Red;",
                ["="] = "color:red",
                ["indirectionoperator"] = "color:white;",
                ["immediateoperator"] = "color:White;",
                ["statementsep"] = "color:Orange;",
                ["listsep"] = "color:white;",
                ["openbracket"] = "color:white;",
                ["closebracket"] = "color:white;",
            },

            ["Mono"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["font"] = "Consolas,Cascadia Code,Menlo,Monospace",
                ["background"] = "Black",
                ["foreground"] = "White",
                ["linenumber_bg"] = "MediumGray",
                ["linenumber_fg"] = "Gray",

                ["keyword"] = "color:White; font-weight: bold;",
                ["indentingkeyword"] = "color:White; font-weight: bold;",
                ["outdentingkeyword"] = "color:White; font-weight: bold;",
                ["inout_keyword"] = "color:White; font-weight: bold;",
                ["builtinfn"] = "color:White; font-weight: bold;",
                ["string"] = "color:SlateGray; font-style:italic;",
                ["number"] = "color:DarkGray;",
                ["hexnumber"] = "color:DarkGray;",
                ["binarynumber"] = "color:DarkGray;",
                ["var"] = "color:Silver;",
                ["array"] = "color:LightSlateGray;",
                ["staticint"] = "color:DimGray;",
                ["remtext"] = "color:Silver; font-style:italic;",
                ["assemcomment"] = "colorSilver; font-style:italic;",
                ["starcommand"] = "color:white;",
                ["embeddeddata"] = "color:white;",
                ["proc"] = "color:Gray;",
                ["fn"] = "color:Gray;",
                ["label"] = "color:Silver;",
                ["register"] = "color:Gray;",
                ["mnemonic"] = "color:White;",
                ["linenumber"] = "color:gray",
                ["operator"] = "color:LightGray;",
                ["="] = "color:LightGray",
                ["indirectionoperator"] = "color:white;",
                ["immediateoperator"] = "color:White;",
                ["statementsep"] = "color:White;",
                ["listsep"] = "color:white;",
                ["openbracket"] = "color:white;",
                ["closebracket"] = "color:white;",
            },

            ["Typewriter"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["font"] = "American Typewriter Std Med,Courier New,Menlo,Monospace",
                ["background"] = "BlanchedAlmond",
                ["foreground"] = "DarkSlateGray",
                ["linenumber_bg"] = "MediumGray",
                ["linenumber_fg"] = "Gray",

                ["keyword"] = "font-weight: bold;",
                ["indentingkeyword"] = "font-weight: bold;",
                ["outdentingkeyword"] = "font-weight: bold;",
                ["inout_keyword"] = "font-weight: bold;",
                ["builtinfn"] = "font-weight: bold;",
                ["string"] = "font-family:Comic Sans MS;",
                ["number"] = "color:DimGray;",
                ["hexnumber"] = "color:DimGray;",
                ["binarynumber"] = "color:DimGray;",
                ["var"] = "text-decoration: underline;",
                ["array"] = "font-weight:light; text-decoration: underline;",
                ["staticint"] = "color:DimGray;",
                ["remtext"] = "font-style:italic;",
                ["assemcomment"] = "font-style:italic;",
                ["starcommand"] = "font-variant:all-small-caps;",
                ["embeddeddata"] = "color:Gold;",
                ["proc"] = "color:Gray;",
                ["fn"] = "color:Gray;",
                ["label"] = "background-color:DarkSlateGray; color:White;",
                ["register"] = "color:DimGray;",
                ["mnemonic"] = "color:DarkSlateGray;",
                ["linenumber"] = "color:gray",
                ["operator"] = "color:DarkSlateGray;",
                ["="] = "color:DarkSlateGray",
                ["indirectionoperator"] = "color:DarkSlateGray;",
                ["immediateoperator"] = "color:DarkSlateGray;",
                ["statementsep"] = "color:DarkSlateGray;",
                ["listsep"] = "color:DarkSlateGray;",
                ["openbracket"] = "color:DarkSlateGray;",
                ["closebracket"] = "color:DarkSlateGray;",
            }
        };
        public static string GetCss(string theme)
        {
            if (!_themes.TryGetValue(theme, out var map))
                map = _themes["Dark"]; // fallback

            var sb = new StringBuilder(Environment.NewLine + "<style>");

            sb.Append($@"
                body {{
                font-family: {map["font"]};
                font-size: 14px;
                background: {map["background"]};
                color: {map["foreground"]};
                }}

                table {{
                border-collapse: collapse;
                width: 100%;
                }}

                td.line-number {{
                color: {map["linenumber_fg"]};
                background-color: {map["linenumber_bg"]};
                padding-right: 4px;
                text-align: right;
                vertical-align: top;
                }}

                td:last-child {{
                white-space: pre-wrap;
                word-break: break-word;
                }}
                /* Folding margin */
                td.fold-marker {{
                    width: 14px;
                    text-align: center;
                    vertical-align: top;
                    cursor: pointer;
                    user-select: none;
                    background-color: {map["linenumber_bg"]};
					color: {map["linenumber_fg"]};
                    font-family: Consolas;
                }}

                /* Body rows (inside PROC/FN) */
                tr.fold-body {{
                    transition: all 0.15s ease;
                }}                
            ");

                // Semantic tag classes (loop!)
            foreach (var kvp in map)
            {
                string tag = kvp.Key;

                // Skip global keys
                if (tag is "background" or "foreground" or "linenumber_bg" or "linenumber_fg")
                    continue;

                string css = kvp.Value;

                sb.Append($".{tag} {{{css}}}" + Environment.NewLine);
            }

            sb.Append("</style>");

            return sb.ToString();
        }
    }
}
