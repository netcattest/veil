import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DOCS = os.path.join(ROOT, "docs")

LOGO_DEFS = """
    <linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#C08CFF"/><stop offset="0.5" stop-color="#7B45D6"/><stop offset="1" stop-color="#2E1666"/>
    </linearGradient>
    <linearGradient id="gloss" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#FFFFFF" stop-opacity="0.42"/><stop offset="0.45" stop-color="#FFFFFF" stop-opacity="0.06"/><stop offset="1" stop-color="#FFFFFF" stop-opacity="0"/>
    </linearGradient>
    <radialGradient id="core" cx="0.5" cy="0.62" r="0.5">
      <stop offset="0" stop-color="#FFFFFF"/><stop offset="1" stop-color="#EBDDFF"/>
    </radialGradient>"""

LOGO_BODY = """
    <rect x="8" y="8" width="240" height="240" rx="58" fill="url(#bg)"/>
    <rect x="8" y="8" width="240" height="240" rx="58" fill="url(#gloss)"/>
    <path d="M128 44 C147 78 198 98 198 150 A70 70 0 0 1 58 150 C58 98 109 78 128 44 Z" fill="none" stroke="#FFFFFF" stroke-opacity="0.55" stroke-width="11" stroke-linejoin="round"/>
    <path d="M128 86 C141 107 170 121 170 153 A42 42 0 0 1 86 153 C86 121 115 107 128 86 Z" fill="none" stroke="#FFFFFF" stroke-opacity="0.85" stroke-width="10" stroke-linejoin="round"/>
    <path d="M128 124 C134 134 148 142 148 157 A20 20 0 0 1 108 157 C108 142 122 134 128 124 Z" fill="url(#core)"/>"""

FONT = "'Segoe UI Variable Display','Segoe UI',-apple-system,BlinkMacSystemFont,'Helvetica Neue',Arial,sans-serif"


def wordmark(text_color, sub_color, tagline):
    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="560" height="160" viewBox="0 0 560 160">
  <defs>{LOGO_DEFS}
  </defs>
  <g transform="translate(16 16) scale(0.5)">{LOGO_BODY}
  </g>
  <text x="172" y="96" font-family="{FONT}" font-size="76" font-weight="600" letter-spacing="-1.5" fill="{text_color}">Veil</text>
  <text x="176" y="128" font-family="{FONT}" font-size="17" font-weight="500" fill="{sub_color}">{tagline}</text>
</svg>
"""


T = {
    "pt": {
        "title": "Como o Veil funciona",
        "subtitle": "Um único executável · tudo roda localmente · nenhuma telemetria",
        "veil": "Veil.exe",
        "veil_sub": "C# · WPF · .NET Framework 4.8",
        "veil_items": [("Interface de vidro", "WPF + acrílico do Windows 11"), ("Recursos embutidos", "tor.zip · 252 bandeiras · países"),
                       ("Bandeja do sistema", "menu próprio com status ao vivo"), ("Configurações", "%LOCALAPPDATA%\\Veil")],
        "launch": "1 · extrai o Tor, gera o torrc e inicia",
        "launch2": "tor.exe dentro de um Job Object",
        "stdout": "log + progresso do bootstrap",
        "control": "ControlPort",
        "control_sub": "127.0.0.1:auto · cookie",
        "control_note": "2 · dados ao vivo do circuito",
        "tor_sub": "Tor 0.4.9.14 · Expert Bundle 15.0.26",
        "tor_items": ["SocksPort 127.0.0.1:9050", "GeoIP local (sem consulta externa)", "ExitNodes {país} · StrictNodes"],
        "pt_title": "Pluggable transports",
        "apps": "Seus apps",
        "apps_sub": "navegador, curl, qualquer app",
        "socks": "SOCKS5",
        "net": "Rede Tor",
        "guard": "Guarda / Ponte",
        "middle": "Relay do meio",
        "exit": "Saída",
        "internet": "Internet",
        "internet_sub": "os sites veem o IP da saída",
        "moat": "bridges.torproject.org",
        "moat_sub": "API Moat (HTTPS)",
        "moat_note": "única consulta externa · só ao clicar em Buscar",
        "legend_moat": "Moat · só ao clicar em Buscar",
        "footer": "Bandeiras, nomes de países e GeoIP vêm embutidos. A contagem de saídas por país é calculada a partir do consenso local do Tor.",
    },
    "en": {
        "title": "How Veil works",
        "subtitle": "A single executable · everything runs locally · zero telemetry",
        "veil": "Veil.exe",
        "veil_sub": "C# · WPF · .NET Framework 4.8",
        "veil_items": [("Glass interface", "WPF + Windows 11 acrylic"), ("Embedded resources", "tor.zip · 252 flags · countries"),
                       ("System tray", "custom menu with live status"), ("Settings", "%LOCALAPPDATA%\\Veil")],
        "launch": "1 · extracts Tor, writes torrc, starts it",
        "launch2": "tor.exe inside a Job Object",
        "stdout": "log + bootstrap progress",
        "control": "ControlPort",
        "control_sub": "127.0.0.1:auto · cookie",
        "control_note": "2 · live circuit data",
        "tor_sub": "Tor 0.4.9.14 · Expert Bundle 15.0.26",
        "tor_items": ["SocksPort 127.0.0.1:9050", "Local GeoIP (no external lookups)", "ExitNodes {country} · StrictNodes"],
        "pt_title": "Pluggable transports",
        "apps": "Your apps",
        "apps_sub": "browser, curl, any app",
        "socks": "SOCKS5",
        "net": "Tor network",
        "guard": "Guard / Bridge",
        "middle": "Middle relay",
        "exit": "Exit",
        "internet": "Internet",
        "internet_sub": "websites see the exit's IP",
        "moat": "bridges.torproject.org",
        "moat_sub": "Moat API (HTTPS)",
        "moat_note": "the only external request · only when you click Fetch",
        "legend_moat": "Moat · only when you click Fetch",
        "footer": "Flags, country names and GeoIP are embedded. Exit counts per country are computed from Tor's local consensus.",
    },
}

COMMANDS = ["GETINFO circuit-status", "GETINFO stream-status", "GETINFO ns/id/<fingerprint>", "GETINFO ip-to-country/<ip>",
            "GETINFO traffic/read · written", "GETINFO status/bootstrap-phase", "GETINFO ns/all", "SETCONF ExitNodes={cc}", "SIGNAL NEWNYM"]


def esc(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def card(x, y, w, h, title, sub=None, accent="#9B5DE5"):
    out = [f'<g><rect x="{x}" y="{y}" width="{w}" height="{h}" rx="22" fill="url(#glass)" stroke="url(#rim)" stroke-width="1.2" filter="url(#shadow)"/>',
           f'<rect x="{x + 18}" y="{y + 20}" width="4" height="{34 if sub else 20}" rx="2" fill="{accent}"/>',
           f'<text x="{x + 32}" y="{y + 36}" class="h">{esc(title)}</text>']
    if sub:
        out.append(f'<text x="{x + 32}" y="{y + 54}" class="s">{esc(sub)}</text>')
    out.append('</g>')
    return "\n".join(out)


def arrow(x1, y1, x2, y2, color="#C9A2F7", dashed=False):
    dash = ' stroke-dasharray="7 6"' if dashed else ""
    return f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{color}" stroke-width="2.4" stroke-linecap="round"{dash} marker-end="url(#arrow{color[1:]})"/>'


def flow_dots(x1, y1, x2, y2, color, delay=0):
    path = f"M{x1},{y1} L{x2},{y2}"
    return (f'<circle r="4" fill="{color}"><animateMotion dur="2.4s" begin="{delay}s" repeatCount="indefinite" path="{path}"/>'
            f'<animate attributeName="opacity" values="0;1;1;0" dur="2.4s" begin="{delay}s" repeatCount="indefinite"/></circle>')


def architecture(lang):
    t = T[lang]
    W, H = 1240, 880
    o = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">',
         f"""<defs>
  <linearGradient id="page" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#1B1428"/><stop offset="0.6" stop-color="#121119"/><stop offset="1" stop-color="#0C0C11"/></linearGradient>
  <radialGradient id="blobA"><stop offset="0" stop-color="#7D4698" stop-opacity="0.55"/><stop offset="1" stop-color="#7D4698" stop-opacity="0"/></radialGradient>
  <radialGradient id="blobB"><stop offset="0" stop-color="#2D63E0" stop-opacity="0.35"/><stop offset="1" stop-color="#2D63E0" stop-opacity="0"/></radialGradient>
  <radialGradient id="blobC"><stop offset="0" stop-color="#C2398F" stop-opacity="0.28"/><stop offset="1" stop-color="#C2398F" stop-opacity="0"/></radialGradient>
  <linearGradient id="glass" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#FFFFFF" stop-opacity="0.13"/><stop offset="0.5" stop-color="#FFFFFF" stop-opacity="0.05"/><stop offset="1" stop-color="#FFFFFF" stop-opacity="0.08"/></linearGradient>
  <linearGradient id="rim" x1="0" y1="0" x2="0.7" y2="1"><stop offset="0" stop-color="#FFFFFF" stop-opacity="0.55"/><stop offset="0.4" stop-color="#FFFFFF" stop-opacity="0.08"/><stop offset="1" stop-color="#FFFFFF" stop-opacity="0.25"/></linearGradient>
  <linearGradient id="chain" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#9B5DE5"/><stop offset="0.5" stop-color="#64D2FF"/><stop offset="1" stop-color="#30D158"/></linearGradient>
  <filter id="shadow" x="-20%" y="-20%" width="140%" height="150%"><feDropShadow dx="0" dy="10" stdDeviation="14" flood-color="#000" flood-opacity="0.45"/></filter>
  <filter id="glow" x="-100%" y="-100%" width="300%" height="300%"><feGaussianBlur stdDeviation="6"/></filter>
  <marker id="arrowC9A2F7" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto"><path d="M0,0 L10,5 L0,10 z" fill="#C9A2F7"/></marker>
  <marker id="arrow7EE2A0" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto"><path d="M0,0 L10,5 L0,10 z" fill="#7EE2A0"/></marker>
  <marker id="arrowFF9F0A" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto"><path d="M0,0 L10,5 L0,10 z" fill="#FF9F0A"/></marker>
  <marker id="arrow64D2FF" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto"><path d="M0,0 L10,5 L0,10 z" fill="#64D2FF"/></marker>
  {LOGO_DEFS}
</defs>
<style>
  text {{ font-family: {FONT}; fill: #F5F5F7; }}
  .t {{ font-size: 34px; font-weight: 600; letter-spacing: -0.5px; }}
  .st {{ font-size: 15px; fill: #A9A6B4; }}
  .h {{ font-size: 18px; font-weight: 600; }}
  .s {{ font-size: 12.5px; fill: #A9A6B4; }}
  .i {{ font-size: 14px; font-weight: 600; }}
  .is {{ font-size: 12px; fill: #9C99A8; }}
  .m {{ font-family: 'Cascadia Mono', Consolas, 'SFMono-Regular', Menlo, monospace; font-size: 12px; fill: #D9C8F5; }}
  .lbl {{ font-size: 12.5px; font-weight: 600; fill: #DDBFFF; }}
  .lbls {{ font-size: 11.5px; fill: #A9A6B4; }}
  .f {{ font-size: 13px; fill: #8E8B98; }}
</style>""",
         f'<rect width="{W}" height="{H}" rx="28" fill="url(#page)"/>',
         '<circle cx="160" cy="120" r="340" fill="url(#blobA)"/><circle cx="1080" cy="520" r="360" fill="url(#blobB)"/><circle cx="420" cy="860" r="320" fill="url(#blobC)"/>',
         f'<g transform="translate(48 40) scale(0.21)">{LOGO_BODY}</g>',
         f'<text x="112" y="76" class="t">{esc(t["title"])}</text>',
         f'<text x="114" y="100" class="st">{esc(t["subtitle"])}</text>']

    vx, vy, vw, vh = 48, 140, 300, 560
    o.append(card(vx, vy, vw, vh, t["veil"], t["veil_sub"]))
    icons = ["M4,5 H20 V15 H4 Z M8,20 H16 M12,15 V20", "M4,7 L12,3 L20,7 V17 L12,21 L4,17 Z M4,7 L12,11 L20,7 M12,11 V21",
             "M3,17 H21 M6,17 V9 A6,6 0 0 1 18,9 V17 M10,20 H14", "M12,8 A4,4 0 1 0 12.01,8 Z M12,2 V5 M12,19 V22 M2,12 H5 M19,12 H22"]
    colors = ["#3478F6", "#8E5BD9", "#30B158", "#FF9F0A"]
    for i, (a, b) in enumerate(t["veil_items"]):
        y = vy + 92 + i * 112
        o.append(f'<rect x="{vx + 18}" y="{y}" width="{vw - 36}" height="92" rx="16" fill="#FFFFFF" fill-opacity="0.05" stroke="#FFFFFF" stroke-opacity="0.08"/>')
        o.append(f'<rect x="{vx + 34}" y="{y + 26}" width="40" height="40" rx="10" fill="{colors[i]}"/>')
        o.append(f'<g transform="translate({vx + 42} {y + 34}) scale(1)"><path d="{icons[i]}" fill="none" stroke="#FFF" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></g>')
        o.append(f'<text x="{vx + 90}" y="{y + 42}" class="i">{esc(a)}</text>')
        o.append(f'<text x="{vx + 90}" y="{y + 62}" class="is">{esc(b)}</text>')

    mx, mw = 392, 270
    o.append(card(mx, 140, mw, 118, t["moat"], t["moat_sub"], "#FF9F0A"))
    o.append(f'<text x="{mx + 32}" y="{140 + 88}" class="lbls">{esc(t["moat_note"])}</text>')
    o.append(arrow(vx + vw + 6, 200, mx - 8, 200, "#FF9F0A", True))

    cy = 300
    o.append(card(mx, cy, mw, 400, t["control"], t["control_sub"], "#64D2FF"))
    for i, c in enumerate(COMMANDS):
        yy = cy + 92 + i * 33
        o.append(f'<rect x="{mx + 18}" y="{yy - 18}" width="{mw - 36}" height="26" rx="8" fill="#FFFFFF" fill-opacity="0.05"/>')
        o.append(f'<text x="{mx + 30}" y="{yy}" class="m">{esc(c)}</text>')
    o.append(arrow(vx + vw + 6, 500, mx - 8, 500, "#64D2FF"))
    o.append(arrow(mx - 8, 540, vx + vw + 6, 540, "#64D2FF"))
    o.append(flow_dots(mx - 8, 540, vx + vw + 6, 540, "#64D2FF"))

    tx, tw = 706, 270
    o.append(card(tx, 140, tw, 260, "tor.exe", t["tor_sub"], "#30D158"))
    for i, item in enumerate(t["tor_items"]):
        yy = 140 + 100 + i * 46
        o.append(f'<circle cx="{tx + 36}" cy="{yy - 4}" r="5" fill="#30D158"/>')
        o.append(f'<text x="{tx + 52}" y="{yy}" class="i" font-size="13">{esc(item)}</text>')
    o.append(f'<path d="M{vx + vw} 166 C 500 122, 620 122, {tx - 10} 170" fill="none" stroke="#C9A2F7" stroke-width="2.4" stroke-linecap="round" marker-end="url(#arrowC9A2F7)"/>')
    o.append(f'<text x="{452}" y="114" class="lbl">{esc(t["launch"])}</text>')
    o.append(f'<text x="{470}" y="{280}" class="lbls" opacity="0">.</text>')
    o.append(arrow(tx - 8, 380, mx + mw + 8, 380, "#64D2FF"))
    o.append(arrow(mx + mw + 8, 420, tx - 8, 420, "#64D2FF"))
    o.append(flow_dots(tx - 8, 380, mx + mw + 8, 380, "#64D2FF", 0.8))

    o.append(card(tx, 430, tw, 150, t["pt_title"], "lyrebird · conjure", "#8E5BD9"))
    chips = ["obfs4", "webtunnel", "snowflake", "meek"]
    for row, names in enumerate([chips[:2], chips[2:]]):
        cx = tx + 22
        for c in names:
            w = 18 + len(c) * 7.4
            y0 = 430 + 74 + row * 34
            o.append(f'<rect x="{cx}" y="{y0}" width="{w}" height="26" rx="13" fill="#9B5DE5" fill-opacity="0.22" stroke="#C9A2F7" stroke-opacity="0.4"/>')
            o.append(f'<text x="{cx + 9}" y="{y0 + 18}" class="m" font-size="12">{c}</text>')
            cx += w + 8
    o.append(arrow(tx + tw / 2, 404, tx + tw / 2, 426, "#C9A2F7"))

    o.append(card(tx, 610, tw, 90, t["apps"], t["apps_sub"], "#0A84FF"))
    o.append(arrow(tx + tw / 2, 606, tx + tw / 2, 586, "#7EE2A0"))
    o.append(f'<text x="{tx + tw / 2 + 10}" y="600" class="lbl" fill="#9BF5B4">{esc(t["socks"])}</text>')

    nx, nw = 1012, 190
    o.append(card(nx, 140, nw, 560, t["net"], None, "#9B5DE5"))
    nodes = [(t["guard"], "#9B5DE5", 250), (t["middle"], "#64D2FF", 380), (t["exit"], "#30D158", 510), (t["internet"], "#8E8E93", 640)]
    lx = nx + 46
    o.append(f'<rect x="{lx - 2}" y="250" width="4" height="390" rx="2" fill="url(#chain)" opacity="0.75"/>')
    for name, color, y in nodes:
        o.append(f'<circle cx="{lx}" cy="{y}" r="18" fill="{color}" opacity="0.35" filter="url(#glow)"/>')
        o.append(f'<circle cx="{lx}" cy="{y}" r="9" fill="{color}" stroke="#FFF" stroke-opacity="0.7" stroke-width="2"/>')
        o.append(f'<text x="{lx + 26}" y="{y + 5}" class="i">{esc(name)}</text>')
    o.append(f'<text x="{lx + 26}" y="{664}" class="is">{esc(t["internet_sub"])}</text>')
    for i, (a, b) in enumerate([(250, 380), (380, 510), (510, 640)]):
        o.append(flow_dots(lx, a + 14, lx, b - 14, nodes[i][1], i * 0.5))
    o.append(f'<path d="M{tx + tw + 4} 230 C {tx + tw + 20} 240, {nx - 20} 240, {lx - 22} 250" fill="none" stroke="#C9A2F7" stroke-width="2.4" stroke-linecap="round" marker-end="url(#arrowC9A2F7)"/>')

    o.append(f'<rect x="48" y="736" width="{W - 96}" height="96" rx="20" fill="#FFFFFF" fill-opacity="0.04" stroke="#FFFFFF" stroke-opacity="0.08"/>')
    legend = [("#C9A2F7", t["launch2"]), ("#64D2FF", "ControlPort · " + t["control_note"].split("· ")[-1]), ("#7EE2A0", "SOCKS5"), ("#FF9F0A", t["legend_moat"])]
    lx0 = 76
    for color, text in legend:
        o.append(f'<line x1="{lx0}" y1="770" x2="{lx0 + 26}" y2="770" stroke="{color}" stroke-width="3" stroke-linecap="round"{" stroke-dasharray=&quot;6 5&quot;" if color == "#FF9F0A" else ""}/>')
        o.append(f'<text x="{lx0 + 36}" y="775" class="s">{esc(text)}</text>')
        lx0 += 36 + len(text) * 6.4 + 40
    o.append(f'<text x="76" y="808" class="f">{esc(t["footer"])}</text>')
    o.append('</svg>')
    return "\n".join(o).replace('stroke-dasharray=&quot;6 5&quot;', 'stroke-dasharray="6 5"')


def main():
    os.makedirs(DOCS, exist_ok=True)
    files = {
        "logo.svg": open(os.path.join(ROOT, "assets", "logo.svg"), encoding="utf-8").read(),
        "wordmark-dark.svg": wordmark("#F5F5F7", "#A9A6B4", "Tor, bonito e simples · by NetCat Test"),
        "wordmark-light.svg": wordmark("#1C1B22", "#6E6B78", "Tor, bonito e simples · by NetCat Test"),
        "wordmark-dark-en.svg": wordmark("#F5F5F7", "#A9A6B4", "Tor, beautifully simple · by NetCat Test"),
        "wordmark-light-en.svg": wordmark("#1C1B22", "#6E6B78", "Tor, beautifully simple · by NetCat Test"),
        "architecture-pt.svg": architecture("pt"),
        "architecture-en.svg": architecture("en"),
    }
    for name, content in files.items():
        with open(os.path.join(DOCS, name), "w", encoding="utf-8", newline="\n") as f:
            f.write(content)
        print(name)


if __name__ == "__main__":
    main()
