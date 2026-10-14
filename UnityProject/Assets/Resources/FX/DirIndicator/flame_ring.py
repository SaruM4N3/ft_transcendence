#!/usr/bin/env python3
"""
flame-ring — anneau ouvert avec etincelle superieure.

Une seule description geometrique (dict G, en fractions de la taille du
canvas) alimente les deux rendus :
  - pixel art : evaluation au centre de chaque pixel, aucun anti-aliasing,
    palette verrouillee, contour trace en post-passe.
  - SVG : memes lois de taper et de superellipse, echantillonnees.

Themes disponibles dans THEMES. Usage:
    python3 flame_ring.py [outdir] [theme ...]
"""
import math
import os
import sys

from PIL import Image

# ----------------------------------------------------------------- themes ---
# ramp : du plus clair (haut / interieur) au plus sombre (bas / exterieur).
# outline : None = pas de contour.
THEMES = {
    "flame": dict(
        ramp=["#FFF4C9", "#FFD65E", "#FFA92B", "#FF6C1A", "#E33A18", "#B21F2D"],
        outline="#201217",
    ),
    "white": dict(
        ramp=["#FFFFFF", "#F4F5F8", "#E6E8EF", "#D6D9E3", "#C2C6D4", "#AAAFC0"],
        outline=None,
    ),
    "white-outline": dict(
        ramp=["#FFFFFF", "#F4F5F8", "#E6E8EF", "#D6D9E3", "#C2C6D4", "#AAAFC0"],
        outline="#1B1B22",
    ),
    "white-flat": dict(
        ramp=["#FFFFFF"],
        outline=None,
    ),
}

RAMP_RGB = []
OUTLINE_RGB = None


def hx(s):
    s = s.lstrip("#")
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), 255)


def use_theme(name):
    global RAMP_RGB, OUTLINE_RGB, THEME
    THEME = THEMES[name]
    RAMP_RGB = [hx(c) for c in THEME["ramp"]]
    OUTLINE_RGB = hx(THEME["outline"]) if THEME["outline"] else None


# -------------------------------------------------------------- geometrie ---
G = dict(
    cy=0.565,          # centre de l'anneau, decale vers le bas (place au pic)
    r_out=0.400,       # rayon exterieur
    thick=0.115,       # epaisseur de la bande
    gap_half=13.0,     # demi-ouverture en bas (degres)
    taper=44.0,        # zone d'affinement des pointes (degres)
    star_up=0.195,     # branche haute de l'etincelle
    star_down=0.085,   # branche basse
    star_side=0.104,   # branches laterales
    star_q=0.80,       # exposant de la superellipse (<1 = branches concaves)
)


def r_mid(size):
    return size * G["r_out"] - size * G["thick"] / 2.0


def ang_from_bottom(a):
    d = (a - (-math.pi / 2.0) + math.pi) % (2 * math.pi) - math.pi
    return abs(d)


def sample_ring(u, v, size):
    r_out = size * G["r_out"]
    th = size * G["thick"]
    rm = r_out - th / 2.0

    r = math.hypot(u, v)
    b = ang_from_bottom(math.atan2(v, u))
    gap = math.radians(G["gap_half"])
    if b <= gap:
        return None
    f = math.sqrt(min(1.0, (b - gap) / math.radians(G["taper"])))
    if abs(r - rm) > f * th / 2.0:
        return None

    n = len(RAMP_RGB)
    if n == 1:
        return 0
    # teinte : rampe complete du haut vers le bas, decalee d'un cran selon
    # la position dans l'epaisseur (bord interieur clair, exterieur sombre).
    s = 1.0 - b / math.pi                                        # 0 haut -> 1 bas
    p = max(0.0, min(1.0, ((r - rm) / (th / 2.0) + 1.0) / 2.0))  # 0 int -> 1 ext
    i = int(round(s * (n - 1)))
    if p < 0.30:
        i -= 1
    elif p > 0.70:
        i += 1
    return max(0, min(n - 1, i))


def sample_star(u, v, size):
    dy = v - r_mid(size)
    sx = size * G["star_side"]
    sy = size * (G["star_up"] if dy >= 0 else G["star_down"])
    q = G["star_q"]
    d = (abs(u) / sx) ** q + (abs(dy) / sy) ** q
    if d > 1.0:
        return None
    return max(0, min(len(RAMP_RGB) - 1, int(round(d * 2.4))))


# ------------------------------------------------------------ rendu pixel ---
def _layer(size, fn, outline):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    px = img.load()
    cx, cy = size / 2.0, size * G["cy"]
    hit = [[False] * size for _ in range(size)]

    for y in range(size):
        for x in range(size):
            i = fn((x + 0.5) - cx, cy - (y + 0.5), size)
            if i is not None:
                hit[y][x] = True
                px[x, y] = RAMP_RGB[i]

    # nettoyage : pas de pixel orphelin (typique des petites tailles)
    for y in range(size):
        for x in range(size):
            if not hit[y][x]:
                continue
            n = sum(
                hit[y + dy][x + dx]
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                if 0 <= x + dx < size and 0 <= y + dy < size
            )
            if n == 0:
                hit[y][x] = False
                px[x, y] = (0, 0, 0, 0)

    if outline and OUTLINE_RGB:
        for y in range(size):
            for x in range(size):
                if hit[y][x]:
                    continue
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < size and 0 <= ny < size and hit[ny][nx]:
                        px[x, y] = OUTLINE_RGB
                        break
    return img, hit


def render(size, outline=True, star=True):
    img, _ = _layer(size, sample_ring, outline)
    if not star:
        return img
    st, st_hit = _layer(size, sample_star, outline)

    # sans contour, l'etincelle se fondrait dans l'anneau : on evide l'anneau
    # sur un pixel autour d'elle pour garder la silhouette lisible sur
    # n'importe quel fond. En dessous de 48px le vide mangerait l'anneau,
    # on laisse alors l'etincelle fusionner en pic.
    if not OUTLINE_RGB and size >= 48:
        px = img.load()
        for y in range(size):
            for x in range(size):
                if st_hit[y][x]:
                    continue
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1),
                               (1, 1), (1, -1), (-1, 1), (-1, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < size and 0 <= ny < size and st_hit[ny][nx]:
                        px[x, y] = (0, 0, 0, 0)
                        break

    img.alpha_composite(st)
    return img


def up(img, factor):
    return img.resize((img.width * factor, img.height * factor), Image.NEAREST)


# -------------------------------------------------------------- rendu svg ---
def P(a_deg, r, cx, cy):
    a = math.radians(a_deg)
    return (cx + r * math.cos(a), cy - r * math.sin(a))


def f2(p):
    return f"{p[0]:.2f} {p[1]:.2f}"


def half_thick(a_deg, size):
    """demi-epaisseur de la bande a un angle donne — meme loi que le pixel."""
    b = ang_from_bottom(math.radians(a_deg))
    gap = math.radians(G["gap_half"])
    if b <= gap:
        return 0.0
    f = math.sqrt(min(1.0, (b - gap) / math.radians(G["taper"])))
    return f * size * G["thick"] / 2.0


def frange(a, b, step):
    n = max(1, int(round(abs(b - a) / step)))
    return [a + (b - a) * i / n for i in range(n + 1)]


def svg(size=64):
    ramp = THEME["ramp"]
    outline = THEME["outline"]
    cx, cy = size / 2.0, size * G["cy"]
    r_out = size * G["r_out"]
    r_in = r_out - size * G["thick"]
    rm = (r_out + r_in) / 2.0
    a0, a1, tp = -90 + G["gap_half"], 270 - G["gap_half"], G["taper"]

    def pt(a, r):
        return P(a, r, cx, cy)

    # --- anneau : pointes echantillonnees, arcs exacts sur la partie pleine
    d = ["M " + f2(pt(a0, rm))]
    for a in frange(a0, a0 + tp, 2.0)[1:]:
        d.append("L " + f2(pt(a, rm + half_thick(a, size))))
    d.append(f"A {r_out:.2f} {r_out:.2f} 0 1 0 " + f2(pt(a1 - tp, r_out)))
    for a in frange(a1 - tp, a1, 2.0)[1:]:
        d.append("L " + f2(pt(a, rm + half_thick(a, size))))
    for a in frange(a1, a1 - tp, 2.0)[1:]:
        d.append("L " + f2(pt(a, rm - half_thick(a, size))))
    d.append(f"A {r_in:.2f} {r_in:.2f} 0 1 1 " + f2(pt(a0 + tp, r_in)))
    for a in frange(a0 + tp, a0, 2.0)[1:]:
        d.append("L " + f2(pt(a, rm - half_thick(a, size))))
    ring = " ".join(d) + " Z"

    # --- etincelle : superellipse d'exposant q, meme equation que le pixel
    sx, q, scy = size * G["star_side"], G["star_q"], cy - rm
    sp = []
    for i in range(96):
        t = 2 * math.pi * i / 96
        ct, st = math.cos(t), math.sin(t)
        sy = size * (G["star_up"] if st >= 0 else G["star_down"])
        u = sx * math.copysign(abs(ct) ** (2.0 / q), ct)
        v = sy * math.copysign(abs(st) ** (2.0 / q), st)
        sp.append(("M " if i == 0 else "L ") + f"{cx + u:.2f} {scy - v:.2f}")
    star = " ".join(sp) + " Z"

    if len(ramp) == 1:
        defs = ""
        fill_ring = fill_star = ramp[0]
    else:
        stops = ["0.00", "0.20", "0.44", "0.66", "0.85", "1.00"][: len(ramp)]
        lin = "".join(
            f'\n      <stop offset="{o}" stop-color="{c}"/>' for o, c in zip(stops, ramp)
        )
        defs = f"""
    <linearGradient id="fr-body" x1="0" y1="0" x2="0" y2="1">{lin}
    </linearGradient>
    <radialGradient id="fr-spark" cx="0.5" cy="0.62" r="0.55">
      <stop offset="0.00" stop-color="{ramp[0]}"/>
      <stop offset="0.55" stop-color="{ramp[min(1, len(ramp) - 1)]}"/>
      <stop offset="1.00" stop-color="{ramp[min(2, len(ramp) - 1)]}"/>
    </radialGradient>"""
        fill_ring, fill_star = "url(#fr-body)", "url(#fr-spark)"

    cut = ""
    ring_attrs = ""
    if not outline:
        cw = size * 0.030
        cut = f"""
    <mask id="fr-cut" maskUnits="userSpaceOnUse" x="0" y="0" width="{size}" height="{size}">
      <rect x="0" y="0" width="{size}" height="{size}" fill="#fff"/>
      <path d="{star}" fill="#000" stroke="#000" stroke-width="{cw:.2f}" stroke-linejoin="round"/>
    </mask>"""
        ring_attrs = ' mask="url(#fr-cut)"'

    if outline:
        g_open = (
            f'<g stroke="{outline}" stroke-width="{size * 0.024:.2f}" '
            f'stroke-linejoin="round" stroke-linecap="round">'
        )
    else:
        g_open = "<g>"

    return f"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {size} {size}" width="{size}" height="{size}">
  <defs>{defs}{cut}
  </defs>
  {g_open}
    <path d="{ring}" fill="{fill_ring}"{ring_attrs}/>
    <path d="{star}" fill="{fill_star}"/>
  </g>
</svg>
"""


# ------------------------------------------------------------------- main ---
def build(outdir, theme):
    use_theme(theme)
    out = os.path.join(outdir, theme)
    os.makedirs(out, exist_ok=True)

    m = {}
    for s, ol, st in ((16, False, True), (32, True, True), (64, True, True)):
        m[s] = render(s, outline=ol, star=st)
        m[s].save(os.path.join(out, f"flame-ring-{theme}-{s}.png"))
    for factor in (2, 4, 8):
        up(m[64], factor).save(
            os.path.join(out, f"flame-ring-{theme}-{64 * factor}.png")
        )
    with open(os.path.join(out, f"flame-ring-{theme}.svg"), "w") as f:
        f.write(svg(64))
    return m


def main():
    outdir = sys.argv[1] if len(sys.argv) > 1 else "."
    themes = sys.argv[2:] or list(THEMES)
    for t in themes:
        build(outdir, t)
        print("ok ->", os.path.join(outdir, t))


if __name__ == "__main__":
    main()
