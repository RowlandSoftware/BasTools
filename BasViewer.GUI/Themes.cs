using BasTools.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace BasViewer.GUI
{
    internal class Themes
    {
        const string LemonChiffon = "#fbf8cc";
        const string PowderPetal = "#fde4fc";
        const string CottonRose = "#ffcfd2";
        const string PinkOrchid = "#f1c0e8";
        const string Periwinkle = "#cfbaf0";
        const string BabyBlueIce = "#a3c4f3";
        const string LightSkyBlue = "#90dbf4";
        const string ElectricAqua = "#8eecf5";
        const string SoftCyan = "#98f5e1";
        const string Celadon = "#b9fbc0";

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
                ["then"] = "color:RoyalBlue; font-weight: bold;",
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
                ["foreground"] = "Black",
                ["linenumber_bg"] = "#dddddd",
                ["linenumber_fg"] = "DarkSlateGray",

                ["keyword"] = "color:Blue; font-weight: bold;",
                ["indentingkeyword"] = "color:Blue; font-weight: bold;",
                ["outdentingkeyword"] = "color:Blue; font-weight: bold;",
                ["inout_keyword"] = "color:Blue; font-weight: bold;",
                ["then"] = "color:RoyalBlue; font-weight: bold;",
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
                ["then"] = "color:White; font-weight: bold;",
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
                ["then"] = "color:White; font-weight: bold;",
                ["builtinfn"] = "color:White; font-weight: bold;",
                ["string"] = "color:SlateGray; font-style:italic;",
                ["number"] = "color:DarkGray;",
                ["hexnumber"] = "color:DarkGray;",
                ["binarynumber"] = "color:DarkGray;",
                ["var"] = "color:Silver;",
                ["array"] = "color:LightSlateGray;",
                ["staticint"] = "color:DimGray;",
                ["remtext"] = "color:Silver; font-style:italic;",
                ["assemcomment"] = "color:Silver; font-style:italic;",
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

            ["Green Screen"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["font"] = "Consolas,Cascadia Code,Menlo,Monospace",
                ["background"] = "#001800",
                ["foreground"] = "PaleGreen",
                ["linenumber_bg"] = "#124012",
                ["linenumber_fg"] = "PaleGreen",

                ["keyword"] = "color:ForestGreen; font-weight: bold;",
                ["indentingkeyword"] = "color:ForestGreen; font-weight: bold;",
                ["outdentingkeyword"] = "color:ForestGreen; font-weight: bold;",
                ["inout_keyword"] = "color:ForestGreen; font-weight: bold;",
                ["then"] = "color:ForestGreen; font-weight: bold;",
                ["builtinfn"] = "color:ForestGreen; font-weight: bold;",
                ["string"] = "color:SpringGreen; font-style:italic;",
                ["number"] = "color:#DBFCCF;",
                ["hexnumber"] = "color:DBFCCF;",
                ["binarynumber"] = "color:DBFCCF;",
                ["var"] = "color:GreenYellow;",
                ["array"] = "color:YellowGreen;",
                ["staticint"] = "color:DimGray;",
                ["remtext"] = "color:GreenYellow; font-style:italic;",
                ["assemcomment"] = "color:GreenYellow; font-style:italic;",
                ["starcommand"] = "color:PaleGreen;",
                ["embeddeddata"] = "color:PaleGreen;",
                ["proc"] = "color:LimeGreen;",
                ["fn"] = "color:LimeGreen;",
                ["label"] = "color:GreenYellow;",
                ["register"] = "color:DarkSeaGreen;",
                ["mnemonic"] = "color:ForestGreen;",
                ["linenumber"] = "color:DarkGreen",
                ["operator"] = "color:DBFCCF;",
                ["="] = "color:ForestGreen",
                ["indirectionoperator"] = "color:DBFCCF;",
                ["immediateoperator"] = "color:ForestGreen;",
                ["statementsep"] = "color:ForestGreen;",
                ["listsep"] = "color:ForestGreen;",
                ["openbracket"] = "color:PaleGreen;",
                ["closebracket"] = "color:PaleGreen;",
            },

            ["Pastel"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["font"] = "Consolas,Cascadia Code,Menlo,Monospace",
                ["background"] = "Indigo", //"#390099",
                ["foreground"] = PinkOrchid,
                ["linenumber_bg"] = PowderPetal,
                ["linenumber_fg"] = "Indigo",

                ["keyword"] = "font-weight: bold;",
                ["indentingkeyword"] = "font-weight: bold;",
                ["outdentingkeyword"] = "font-weight: bold;",
                ["inout_keyword"] = "font-weight: bold;",
                ["then"] = "font-weight: bold;",
                ["builtinfn"] = "font-weight: bold;",
                ["string"] = "color:LimeGreen;",
                ["number"] = "color:b9fbc0;",
                ["hexnumber"] = "color:#b9fbc0;",
                ["binarynumber"] = "color:#b9fbc0;",
                ["var"] = "color:#a3c4f3;",
                ["array"] = "color:98f5e1;",
                ["staticint"] = "color:98f5e1;",
                ["remtext"] = "color:#fbf8cc; font-style:italic; font-weight: bold;",
                ["assemcomment"] = "color:#fbf8cc; font-style:italic; font-weight: bold;",
                ["starcommand"] = "color:Gold;",
                ["embeddeddata"] = "color:Orange;",
                ["proc"] = "color:#8eecf5;",
                ["fn"] = "color:#8eecf5;",
                ["label"] = "color:MediumOrchid;",
                ["register"] = "color:SeaGreen;",
                ["mnemonic"] = "color:DodgerBlue;",
                ["linenumber"] = "color:b9fbc0",
                ["operator"] = "color:Red;",
                ["="] = "color:red",
                ["indirectionoperator"] = "color:#fbf8cc;",
                ["immediateoperator"] = "color:#fbf8cc;",
                ["statementsep"] = "color:Orange;",
                ["listsep"] = "color:Blue;",
                ["openbracket"] = "color:#ffff3f;",
                ["closebracket"] = "color:#ffff3f;",
            },

            ["Neon"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["font"] = "Consolas,Cascadia Code,Menlo,Monospace",
                ["background"] = "Black",
                ["foreground"] = "DodgerBlue",
                ["linenumber_bg"] = "DarkSlateGray",
                ["linenumber_fg"] = "Red",

                ["keyword"] = "font-weight: bold;",
                ["indentingkeyword"] = "font-weight: bold;",
                ["outdentingkeyword"] = "font-weight: bold;",
                ["inout_keyword"] = "font-weight: bold;",
                ["then"] = "font-weight: bold;",
                ["builtinfn"] = "font-weight: bold;",
                ["string"] = "color:LimeGreen; font-weight: bold;",
                ["number"] = "color:white;",
                ["hexnumber"] = "color:white;",
                ["binarynumber"] = "color:white;",
                ["var"] = "color:#FF1ED6;",
                ["array"] = "color:Violet;",
                ["staticint"] = "color:MediumPurple;",
                ["remtext"] = "color:Yellow; font-style:italic;",
                ["assemcomment"] = "color:Yellow; font-style:italic;",
                ["starcommand"] = "color:Gold;",
                ["embeddeddata"] = "color:Orange;",
                ["proc"] = "color:Turquoise;",
                ["fn"] = "color:Turquoise;",
                ["label"] = "color:MediumOrchid;",
                ["register"] = "color:GreenYellow;",
                ["mnemonic"] = "color:DodgerBlue;",
                ["linenumber"] = "color:Crimson",
                ["operator"] = "color:Red;",
                ["="] = "color:red",
                ["indirectionoperator"] = "color:white;",
                ["immediateoperator"] = "color:White;",
                ["statementsep"] = "color:Orange;",
                ["listsep"] = "color:Blue;",
                ["openbracket"] = "color:Yellow;",
                ["closebracket"] = "color:Yellow;",
            },

            ["Visual Studio"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["font"] = "Cascadia Mono,Cascadia Code,Consolas,Menlo,Monospace",
                ["background"] = "#111",
                ["foreground"] = "#eee",
                ["linenumber_bg"] = "#111",
                ["linenumber_fg"] = "#8a8a8a",

                ["keyword"] = "color:#d896b7;",
                ["indentingkeyword"] = "color:#d896b7;",
                ["outdentingkeyword"] = "color:#d896b7;",
                ["inout_keyword"] = "color:#d896b7;",
                ["then"] = "color:#d896b7;",
                ["builtinfn"] = "color:#d896b7;",
                ["string"] = "color:#d69d85;",
                ["number"] = "color:#b5ce93;",
                ["hexnumber"] = "color:#b5ce93;",
                ["binarynumber"] = "color:#b5ce93;",
                ["var"] = "color:#9cdcff;",
                ["array"] = "color:9cdcff;font-weight: bold;",
                ["staticint"] = "color:#9cdcff;font-weight:bold;",
                ["remtext"] = "color:#57a64a;",
                ["assemcomment"] = "color:#57a64a;",
                ["starcommand"] = "color:white;",
                ["embeddeddata"] = "color:#3c6ca0;",
                ["proc"] = "color:#dcdcaa;",
                ["fn"] = "color:#dcdcaa;",
                ["label"] = "color:#da70d6;",
                ["register"] = "color:GreenYellow;",
                ["mnemonic"] = "color:#18a0ff;",
                ["linenumber"] = "color:gray",
                ["operator"] = "color:#b4b4b4;",
                ["="] = "color:#b4b4b4",
                ["indirectionoperator"] = "color:#4ec9b0;",
                ["immediateoperator"] = "color:d69d85;",
                ["statementsep"] = "color:white;",
                ["listsep"] = "color:white;",
                ["openbracket"] = "color:#ffd700;",
                ["closebracket"] = "color:#ffd700;",
            },

            ["Typewriter"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["font"] = "ITC American Typewriter Std, Courier New,Menlo,Monospace",
                ["background"] = "BlanchedAlmond",
                ["foreground"] = "DarkSlateGray",
                ["linenumber_bg"] = "MediumGray",
                ["linenumber_fg"] = "Gray",

                ["keyword"] = "font-weight: bold;",
                ["indentingkeyword"] = "font-weight: bold;",
                ["outdentingkeyword"] = "font-weight: bold;",
                ["inout_keyword"] = "font-weight: bold;",
                ["then"] = "font-weight: bold;",
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
                ["embeddeddata"] = "font-weight:light;",
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
                width: 5ch;
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
                .search-hit {{
                    background-color: yellow;
                    color: black;
                    border-radius: 2px;
                }}
                .search-current {{
                    background-color: orange;
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
        internal static string GetScript()
        {
            string script1 = Environment.NewLine + @"<script>
                function toggleFold(name) {
                    const rows = document.querySelectorAll('.' + name);
                    const arrow = document.getElementById('arrow_' + name);
                    const isClosed = (arrow.textContent === ""▶"");
                    rows.forEach(r => { r.style.display = isClosed ? """" : ""none""; });
                    arrow.textContent = isClosed ? ""▼"" : ""▶"";
                };
            ";

            string script2 = Environment.NewLine + @"
            window.search = window.search || {};

window.search.clear = function () {
    // Remove current-match class
    document.querySelectorAll("".search-current"").forEach(span => {
        span.classList.remove(""search-current"");
    });

    // Remove search-hit spans safely
    document.querySelectorAll("".search-hit"").forEach(span => {
        const parent = span.parentNode;
        if (!parent) return;

        const text = document.createTextNode(span.textContent);

        // Insert the text node before the span
        parent.insertBefore(text, span);

        // Remove the span
        parent.removeChild(span);

        // Merge adjacent text nodes
        if (text.previousSibling && text.previousSibling.nodeType === Node.TEXT_NODE) {
            text.previousSibling.textContent += text.textContent;
            parent.removeChild(text);
        }
    });
};

window.search.highlightAll = function (term){
    if (!term) return;

    const regex = new RegExp(term.replace(/[.*+?^${}()|[\]\\]/g, ""\\$&""), ""gi"");

    const walker = document.createTreeWalker(
        document.body,
        NodeFilter.SHOW_TEXT,
        null,
        false
    );

    const textNodes = [];
    let node;
    while (node = walker.nextNode()) {
        if (node.parentNode &&
            node.parentNode.classList &&
            node.parentNode.classList.contains(""search-hit""))
            continue;

        textNodes.push(node);
    }

    for (const node of textNodes) {
        const text = node.nodeValue;

        regex.lastIndex = 0;

        let match;
        let lastIndex = 0;
        const frag = document.createDocumentFragment();

        while ((match = regex.exec(text)) !== null) {
            const before = text.slice(lastIndex, match.index);
            if (before)
                frag.appendChild(document.createTextNode(before));

            const span = document.createElement(""span"");
            span.className = ""search-hit"";
            span.textContent = match[0];
            frag.appendChild(span);

            lastIndex = match.index + match[0].length;
        }

        if (lastIndex === 0)
            continue;

        const after = text.slice(lastIndex);
        if (after)
            frag.appendChild(document.createTextNode(after));

        node.parentNode.replaceChild(frag, node);
    }
};

window.search.applyMatches = function (matches, currentIndex)
{
    window.search.clear();

    matches.forEach((m, i) =>
    {
        const lineTd = document.getElementById(""line_"" + m.LineId);
        if (!lineTd) return;

        const line = lineTd.parentElement;
        if (!line) return;

        const codeCell = line.querySelector(""td.code"");
        if (!codeCell) return;

        const spans = Array.from(codeCell.querySelectorAll(""span""));
        const targetSpan = spans[m.TokenIndex];
        if (!targetSpan) return;

        const fullText = targetSpan.textContent;
        const start = m.Offset;
        const end = m.Offset + m.Length;

        // Whole-span match? (your old behaviour)
        if (start === 0 && end === fullText.length)
        {
            const hit = document.createElement(""span"");
            hit.className = ""search-hit"";
            hit.dataset.matchIndex = i;
            hit.textContent = fullText;

            targetSpan.replaceWith(hit);
            return;
        }

        // Substring match inside the span
        const beforeText = fullText.slice(0, start);
        const hitText    = fullText.slice(start, end);
        const afterText  = fullText.slice(end);

        const frag = document.createDocumentFragment();

        if (beforeText.length > 0)
        {
            const beforeSpan = document.createElement(""span"");
            beforeSpan.textContent = beforeText;
            frag.appendChild(beforeSpan);
        }

        const hitSpan = document.createElement(""span"");
        hitSpan.className = ""search-hit"";
        hitSpan.dataset.matchIndex = i;
        hitSpan.textContent = hitText;
        frag.appendChild(hitSpan);

        if (afterText.length > 0)
        {
            const afterSpan = document.createElement(""span"");
            afterSpan.textContent = afterText;
            frag.appendChild(afterSpan);
        }

        targetSpan.replaceWith(frag);
    });

    window.search.scrollTo(currentIndex);
};

window.search.scrollTo = function (index) {
    const hits = document.querySelectorAll("".search-hit"");
    if (hits.length === 0) return;

    hits.forEach(h => h.classList.remove(""search-current""));

    // Prefer the span whose data-match-index matches
    let target = null;
    hits.forEach(h => {
        if (h.dataset.matchIndex == index) {
            target = h;
        }
    });

    // Fallback: treat index as position in hits
    if (!target) {
        target = hits[index];
    }
    if (!target) return;

    target.classList.add(""search-current"");
    target.scrollIntoView({ behavior: ""smooth"", block: ""center"" });
};
" + Environment.NewLine;
            
            return script1 + script2 + "</script>";
        }
    }
}
