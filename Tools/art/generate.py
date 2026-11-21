"""
Generador de arte pixel-art de Flappy 2077.

Todo el arte nuevo sale de aquí, con semilla fija, para que sea reproducible y siga la
paleta de los edificios originales (morado #9b21d9, azul #1129a6, cian #40f4d2, coral #ff5c5c,
rosa #ff32a1). Se dibuja a resolución nativa (1 px = 1 píxel de arte) y Unity lo importa con
filtro Point y 20 px por unidad (ver Assets/Editor/PixelArtImporter.cs).

Uso:
    python3 -m venv Tools/art/.venv && Tools/art/.venv/bin/pip install pillow==11.3.0
    Tools/art/.venv/bin/python Tools/art/generate.py
"""
import math
import os
import random
import struct
import wave

from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Pixel")
AUDIO_OUT = os.path.join(ROOT, "Assets", "Resources", "Audio")

# ---------------------------------------------------------------- paleta


def hx(s, a=255):
    s = s.lstrip("#")
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), a)


P = {
    "void": hx("05010f"),
    "night": hx("0d0326"),
    "deep": hx("1a0645"),
    "indigo": hx("2a0f6b"),
    "royal": hx("1129a6"),
    "violet": hx("442da4"),
    "purple": hx("7a37e6"),
    "magenta": hx("9b21d9"),
    "plum": hx("59007a"),
    "wine": hx("ab1d8c"),
    "pink": hx("ff32a1"),
    "rose": hx("ff79ce"),
    "lilac": hx("ea83e5"),
    "cyan": hx("40f4d2"),
    "mint": hx("55e0be"),
    "sky": hx("428cff"),
    "coral": hx("ff5c5c"),
    "peach": hx("ffb09a"),
    "amber": hx("ffd36b"),
    "white": hx("f4ecff"),
    "shadow": hx("0a0420"),
}
CLEAR = (0, 0, 0, 0)

# Matriz de Bayer 4x4 para degradados con tramado (look retro).
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def lerp(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(4))


def save(img, name):
    os.makedirs(OUT, exist_ok=True)
    img.save(os.path.join(OUT, name + ".png"))


def rect(img, x0, y0, x1, y1, c):
    """Rectángulo relleno con coordenadas inclusivas, recortado al lienzo."""
    w, h = img.size
    px = img.load()
    for y in range(max(0, y0), min(h, y1 + 1)):
        for x in range(max(0, x0), min(w, x1 + 1)):
            px[x, y] = c


def put(img, x, y, c):
    w, h = img.size
    if 0 <= x < w and 0 <= y < h:
        img.load()[x, y] = c


def dither_gradient(img, stops, y0=0, y1=None):
    """Degradado vertical por tramos con tramado Bayer. stops: [(t, color), ...]."""
    w, h = img.size
    y1 = h - 1 if y1 is None else y1
    px = img.load()
    bands = 6  # pasos visibles por tramo: pixel-art, no degradado suave
    for y in range(y0, y1 + 1):
        t = (y - y0) / max(1, (y1 - y0))
        for i in range(len(stops) - 1):
            t0, c0 = stops[i]
            t1, c1 = stops[i + 1]
            if t0 <= t <= t1:
                lt = (t - t0) / max(1e-6, t1 - t0)
                break
        for x in range(w):
            q = lt * bands + (BAYER[y % 4][x % 4] / 16.0 - 0.5)
            q = min(bands, max(0, math.floor(q + 0.5)))
            px[x, y] = lerp(c0, c1, q / bands)


# ---------------------------------------------------------------- fondo


def sky():
    """Cielo 480x200: negro arriba, bruma violeta-magenta en el horizonte (como el logo)."""
    w, h = 480, 200
    img = Image.new("RGBA", (w, h))
    dither_gradient(
        img,
        [
            (0.0, P["void"]),
            (0.45, P["night"]),
            (0.72, P["deep"]),
            (0.9, hx("3b0b78")),
            (1.0, hx("6a12a8")),
        ],
    )
    rnd = random.Random(2077)
    for _ in range(170):
        x, y = rnd.randrange(w), rnd.randrange(int(h * 0.75))
        c = rnd.choice([P["white"], P["white"], P["lilac"], P["cyan"], hx("b9a6ff")])
        fade = 1 - y / (h * 0.75)
        put(img, x, y, (c[0], c[1], c[2], int(120 + 135 * fade)))
    for _ in range(14):  # estrellas grandes en cruz
        x, y = rnd.randrange(2, w - 2), rnd.randrange(2, int(h * 0.5))
        put(img, x, y, P["white"])
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            put(img, x + dx, y + dy, (200, 190, 255, 140))
    save(img, "bg_sky")


def moon():
    """Luna sintética de 72 px: degradado coral→rosa→violeta con franjas horizontales."""
    s = 72
    img = Image.new("RGBA", (s, s), CLEAR)
    px = img.load()
    r = s / 2 - 0.5
    for y in range(s):
        t = y / (s - 1)
        if t < 0.5:
            col = lerp(P["amber"], P["coral"], t / 0.5)
        else:
            col = lerp(P["coral"], P["pink"], (t - 0.5) / 0.5)
        for x in range(s):
            if (x - r) ** 2 + (y - r) ** 2 <= r * r:
                q = BAYER[y % 4][x % 4] / 16.0
                c = col if q < 0.85 else lerp(col, P["white"], 0.25)
                px[x, y] = c
    # Franjas que se ensanchan hacia abajo (estética retro-futurista).
    y = int(s * 0.52)
    gap = 1
    while y < s:
        for yy in range(y, min(s, y + gap)):
            for x in range(s):
                px[x, yy] = CLEAR
        y += gap + max(2, 6 - gap)
        gap += 1
    save(img, "bg_moon")


def skyline(name, w, h, seed, body, edge, lights, density, min_h, max_h, antennas=True):
    """Silueta de ciudad repetible en horizontal (los bordes izquierdo y derecho empalman)."""
    rnd = random.Random(seed)
    img = Image.new("RGBA", (w, h), CLEAR)
    x = 0
    blocks = []
    while x < w:
        bw = rnd.randint(14, 34)
        bh = rnd.randint(min_h, max_h)
        blocks.append((x, bw, bh))
        x += bw - rnd.randint(0, 4)
    # Cierra el empalme: el último bloque termina exactamente en w.
    for bx, bw, bh in blocks:
        x0, x1 = bx, min(w - 1, bx + bw - 1)
        top = h - bh
        rect(img, x0, top, x1, h - 1, body)
        style = rnd.random()
        if style < 0.3:  # remate escalonado
            rect(img, x0 + 3, top - 4, x1 - 3, top - 1, body)
            rect(img, x0 + 6, top - 7, x1 - 6, top - 5, body)
        elif style < 0.5:  # azotea inclinada
            for i in range(min(10, (x1 - x0) // 2)):
                rect(img, x0 + i, top - (10 - i) // 2, x0 + i, top - 1, body)
        # bordes con luz de neón tenue
        rect(img, x0, top, x0, h - 1, edge)
        rect(img, x0, top, x1, top, edge)
        # ventanas
        for wy in range(top + 3, h - 2, 4):
            for wx in range(x0 + 2, x1 - 1, 3):
                if rnd.random() < density:
                    c = rnd.choice(lights)
                    put(img, wx, wy, c)
                    if rnd.random() < 0.5:
                        put(img, wx + 1, wy, c)
        if antennas and rnd.random() < 0.35:
            ax = rnd.randint(x0 + 2, max(x0 + 2, x1 - 2))
            ah = rnd.randint(5, 14)
            rect(img, ax, top - ah, ax, top - 1, edge)
            put(img, ax, top - ah - 1, P["coral"])
    # Empalme: copia la columna 0..3 al final para que repita sin costura.
    px = img.load()
    for y in range(h):
        for k in range(4):
            px[w - 4 + k, y] = px[k, y]
    save(img, name)


def background():
    sky()
    moon()
    dim = lambda c, a: (c[0], c[1], c[2], a)
    skyline(
        "bg_far", 480, 110, 11,
        body=hx("1b0a4a"), edge=hx("2c1574"),
        lights=[dim(P["violet"], 255), dim(P["purple"], 200), dim(P["sky"], 140)],
        density=0.18, min_h=30, max_h=95, antennas=True,
    )
    skyline(
        "bg_mid", 480, 130, 23,
        body=hx("26105f"), edge=hx("4a22a8"),
        lights=[P["cyan"], P["rose"], P["amber"], P["mint"], P["coral"], dim(P["sky"], 220)],
        density=0.28, min_h=24, max_h=80, antennas=True,
    )


# ---------------------------------------------------------------- tráfico


def traffic():
    """Autos voladores lejanos (8x4) en tres colores, con faro y luz trasera."""
    bodies = [("traffic_a", P["violet"], P["rose"]), ("traffic_b", P["royal"], P["cyan"]), ("traffic_c", P["plum"], P["amber"])]
    for name, body, glow in bodies:
        img = Image.new("RGBA", (10, 4), CLEAR)
        rect(img, 2, 1, 8, 2, body)
        rect(img, 4, 0, 6, 0, body)
        put(img, 9, 1, P["white"])   # faro
        put(img, 1, 1, P["coral"])  # luz trasera
        put(img, 0, 1, (255, 92, 92, 120))
        rect(img, 3, 3, 7, 3, (glow[0], glow[1], glow[2], 170))  # brillo inferior
        save(img, name)


# ---------------------------------------------------------------- edificios nuevos


def window_grid(img, x0, y0, x1, y1, rnd, colors, p=0.55, step=(3, 3), size=(2, 1)):
    for y in range(y0, y1 - size[1] + 2, step[1]):
        for x in range(x0, x1 - size[0] + 2, step[0]):
            if rnd.random() < p:
                rect(img, x, y, x + size[0] - 1, y + size[1] - 1, rnd.choice(colors))
            else:
                rect(img, x, y, x + size[0] - 1, y + size[1] - 1, P["plum"])


def outline(img, color):
    """Contorno de 1 px alrededor de los píxeles opacos (como los sprites originales)."""
    w, h = img.size
    src = img.copy().load()
    px = img.load()
    for y in range(h):
        for x in range(w):
            if src[x, y][3] == 0:
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h and src[nx, ny][3] > 0:
                        px[x, y] = color
                        break


WIN = None  # se fija en buildings()


def tower_neon(seed):
    """Torre alta con letrero vertical de neón."""
    rnd = random.Random(seed)
    w, h = 30, 190
    img = Image.new("RGBA", (w, h), CLEAR)
    rect(img, 4, 14, 25, h - 1, P["magenta"])
    rect(img, 4, 14, 5, h - 1, P["royal"])
    rect(img, 24, 14, 25, h - 1, P["violet"])
    rect(img, 7, 6, 22, 13, P["royal"])  # remate
    rect(img, 14, 0, 15, 5, P["royal"])
    put(img, 14, 0, P["coral"]); put(img, 15, 0, P["coral"])
    window_grid(img, 8, 18, 21, h - 14, rnd, WIN, p=0.5)
    # letrero vertical
    rect(img, 0, 30, 4, 110, P["shadow"])
    rect(img, 1, 31, 3, 109, P["wine"])
    for y in range(33, 108, 6):
        rect(img, 1, y, 3, y + 3, rnd.choice([P["pink"], P["cyan"], P["rose"]]))
    # puerta y base
    rect(img, 4, h - 10, 25, h - 1, P["royal"])
    rect(img, 12, h - 8, 17, h - 1, P["cyan"])
    rect(img, 13, h - 7, 16, h - 1, P["mint"])
    outline(img, P["shadow"])
    return img


def tower_holo(seed):
    """Rascacielos con anillos holográficos cian."""
    rnd = random.Random(seed)
    w, h = 40, 200
    img = Image.new("RGBA", (w, h), CLEAR)
    rect(img, 9, 30, 30, h - 1, P["royal"])
    rect(img, 11, 30, 28, h - 1, P["violet"])
    rect(img, 15, 12, 24, 29, P["royal"])
    rect(img, 19, 0, 20, 11, P["royal"])
    put(img, 19, 0, P["cyan"]); put(img, 20, 0, P["cyan"])
    window_grid(img, 13, 34, 27, h - 6, rnd, [P["cyan"], P["mint"], P["sky"], P["lilac"]], p=0.6, step=(3, 2), size=(2, 1))
    for ry in (48, 92, 136):  # anillos
        rect(img, 3, ry, 36, ry + 2, (64, 244, 210, 170))
        rect(img, 5, ry + 1, 34, ry + 1, P["cyan"])
        rect(img, 3, ry + 3, 36, ry + 3, (64, 244, 210, 70))
    outline(img, P["shadow"])
    return img


def tower_capsule(seed):
    """Bloque de cápsulas apiladas (vivienda) con luces cálidas."""
    rnd = random.Random(seed)
    w, h = 44, 170
    img = Image.new("RGBA", (w, h), CLEAR)
    rect(img, 6, 10, 37, h - 1, P["plum"])
    for y in range(12, h - 8, 9):
        for x in (8, 22):
            c = rnd.choice([P["magenta"], P["wine"], P["violet"]])
            rect(img, x, y, x + 13, y + 7, c)
            rect(img, x + 2, y + 2, x + 11, y + 5, P["shadow"])
            if rnd.random() < 0.7:
                lc = rnd.choice([P["amber"], P["peach"], P["rose"], P["cyan"]])
                rect(img, x + 2 + rnd.randint(0, 5), y + 3, x + 6 + rnd.randint(0, 5), y + 4, lc)
    # tuberías y escalera
    rect(img, 3, 20, 4, h - 1, P["violet"])
    rect(img, 39, 10, 40, h - 1, P["violet"])
    for y in range(20, h, 5):
        put(img, 2, y, P["violet"]); put(img, 5, y, P["violet"])
    rect(img, 8, 2, 20, 9, P["violet"])  # depósito
    rect(img, 10, 4, 18, 5, P["cyan"])
    outline(img, P["shadow"])
    return img


def platform_hanging(seed):
    """Estructura colgante (obstáculo superior): plataforma con cables, focos y letrero."""
    rnd = random.Random(seed)
    w, h = 46, 190
    img = Image.new("RGBA", (w, h), CLEAR)
    # columna que baja desde arriba
    rect(img, 18, 0, 27, h - 30, P["royal"])
    rect(img, 20, 0, 25, h - 30, P["violet"])
    for y in range(4, h - 34, 6):
        rect(img, 20, y, 25, y, P["indigo"])
        put(img, 22, y + 2, rnd.choice([P["cyan"], P["coral"], P["rose"]]))
    # módulo inferior
    rect(img, 4, h - 32, 41, h - 10, P["magenta"])
    rect(img, 4, h - 32, 41, h - 30, P["royal"])
    window_grid(img, 7, h - 27, 38, h - 14, rnd, WIN, p=0.6)
    rect(img, 8, h - 9, 37, h - 6, P["royal"])
    # focos que iluminan hacia abajo
    for x in (10, 22, 34):
        rect(img, x, h - 5, x + 2, h - 4, P["amber"])
        rect(img, x - 1, h - 3, x + 3, h - 1, (255, 211, 107, 90))
    # letrero lateral, sujeto a la columna con dos soportes
    rect(img, 28, h - 66, 30, h - 65, P["indigo"])
    rect(img, 28, h - 46, 30, h - 45, P["indigo"])
    rect(img, 30, h - 70, 44, h - 40, P["shadow"])
    rect(img, 31, h - 69, 43, h - 41, P["wine"])
    for y in range(h - 66, h - 43, 5):
        rect(img, 33, y, 41, y + 2, rnd.choice([P["pink"], P["cyan"]]))
    outline(img, P["shadow"])
    return img


def platform_bridge(seed):
    """Puente aéreo con tubos de tránsito (obstáculo superior ancho)."""
    rnd = random.Random(seed)
    w, h = 56, 170
    img = Image.new("RGBA", (w, h), CLEAR)
    rect(img, 6, 0, 13, h - 26, P["royal"])
    rect(img, 42, 0, 49, h - 26, P["royal"])
    rect(img, 8, 0, 11, h - 26, P["violet"])
    rect(img, 44, 0, 47, h - 26, P["violet"])
    for y in range(0, h - 30, 10):  # tirantes cruzados
        for i in range(10):
            put(img, 14 + i * 2, y + i, P["indigo"])
            put(img, 41 - i * 2, y + i, P["indigo"])
    rect(img, 0, h - 26, 55, h - 8, P["magenta"])
    rect(img, 0, h - 26, 55, h - 24, P["royal"])
    rect(img, 2, h - 21, 53, h - 15, P["shadow"])
    for x in range(3, 52, 7):  # cápsulas de tránsito iluminadas
        if rnd.random() < 0.7:
            rect(img, x, h - 20, x + 4, h - 16, rnd.choice([P["cyan"], P["mint"], P["rose"]]))
    rect(img, 0, h - 7, 55, h - 5, P["royal"])
    for x in range(4, 54, 6):
        put(img, x, h - 4, P["coral"])
    outline(img, P["shadow"])
    return img


def tower_pagoda(seed):
    """Torre escalonada tipo pagoda con aleros de neón."""
    rnd = random.Random(seed)
    w, h = 44, 200
    img = Image.new("RGBA", (w, h), CLEAR)
    y = h - 1
    widths = [36, 32, 28, 24, 20, 16]
    for i, bw in enumerate(widths):
        tier_h = 26 if i < 4 else 20
        x0 = (w - bw) // 2
        rect(img, x0, y - tier_h + 1, x0 + bw - 1, y, P["magenta"] if i % 2 == 0 else P["violet"])
        window_grid(img, x0 + 3, y - tier_h + 6, x0 + bw - 4, y - 3, rnd, WIN, p=0.5)
        # alero con borde de neón
        rect(img, x0 - 4, y - tier_h - 1, x0 + bw + 3, y - tier_h + 1, P["royal"])
        rect(img, x0 - 4, y - tier_h + 2, x0 + bw + 3, y - tier_h + 2, P["pink"] if i % 2 == 0 else P["cyan"])
        put(img, x0 - 4, y - tier_h + 3, P["coral"])
        put(img, x0 + bw + 3, y - tier_h + 3, P["coral"])
        y -= tier_h + 3
    rect(img, w // 2 - 1, max(0, y - 18), w // 2, y, P["royal"])
    put(img, w // 2 - 1, max(0, y - 19), P["coral"]); put(img, w // 2, max(0, y - 19), P["coral"])
    outline(img, P["shadow"])
    return img


def tower_corp(seed):
    """Torre corporativa de vidrio con panel de logo luminoso."""
    rnd = random.Random(seed)
    w, h = 38, 210
    img = Image.new("RGBA", (w, h), CLEAR)
    rect(img, 4, 20, 33, h - 1, P["royal"])
    # vidrio: franjas diagonales tenues
    for y in range(20, h):
        for x in range(5, 33):
            if (x + y) % 9 == 0:
                put(img, x, y, P["sky"])
            elif (x + y) % 9 == 1:
                put(img, x, y, (66, 140, 255, 130))
    for y in range(26, h - 6, 5):
        rect(img, 5, y, 32, y, P["indigo"])
        for x in range(6, 32, 2):
            if rnd.random() < 0.18:
                put(img, x, y + 2, rnd.choice([P["cyan"], P["white"], P["amber"]]))
    # corona inclinada
    for i in range(14):
        rect(img, 4 + i, 20 - i, 33, 20 - i, P["royal"])
    rect(img, 30, 0, 31, 7, P["royal"])
    put(img, 30, 0, P["coral"]); put(img, 31, 0, P["coral"])
    # panel de logo
    rect(img, 9, 40, 28, 58, P["shadow"])
    rect(img, 10, 41, 27, 57, P["wine"])
    for (x, y) in ((14, 44), (13, 45), (12, 46), (13, 47), (14, 48), (15, 49), (16, 50), (17, 51), (18, 52),
                   (19, 51), (20, 50), (21, 49), (22, 48), (23, 47), (24, 46), (23, 45), (22, 44)):
        rect(img, x, y, x + 1, y + 1, P["cyan"])
    rect(img, 14, 54, 23, 55, P["pink"])
    outline(img, P["shadow"])
    return img


def tower_shops(seed):
    """Bloque estrecho de tiendas apiladas con toldos y letreros laterales."""
    rnd = random.Random(seed)
    w, h = 40, 180
    img = Image.new("RGBA", (w, h), CLEAR)
    y = h - 1
    awnings = [P["pink"], P["cyan"], P["amber"], P["rose"], P["mint"]]
    i = 0
    while y > 24:
        fh = rnd.randint(18, 26)
        body = rnd.choice([P["plum"], P["violet"], P["magenta"], P["wine"]])
        rect(img, 6, y - fh + 1, 33, y, body)
        rect(img, 9, y - fh + 7, 30, y - 3, P["shadow"])
        for x in range(10, 30, 4):
            rect(img, x, y - fh + 8, x + 2, y - 4, rnd.choice([P["amber"], P["peach"], P["cyan"], P["rose"]]) if rnd.random() < 0.6 else P["indigo"])
        # toldo a rayas
        col = awnings[i % len(awnings)]
        for x in range(4, 36):
            rect(img, x, y - fh + 2, x, y - fh + 4, col if (x // 2) % 2 == 0 else P["white"])
        # letrero lateral alterno
        if i % 2 == 0:
            rect(img, 34, y - fh + 6, 39, y - 4, P["shadow"])
            for yy in range(y - fh + 7, y - 5, 3):
                rect(img, 35, yy, 38, yy + 1, col)
        else:
            rect(img, 0, y - fh + 6, 5, y - 4, P["shadow"])
            for yy in range(y - fh + 7, y - 5, 3):
                rect(img, 1, yy, 4, yy + 1, col)
        y -= fh
        i += 1
    rect(img, 8, y - 8, 31, y, P["violet"])
    rect(img, 12, y - 14, 15, y - 9, P["indigo"])  # tanque de agua
    outline(img, P["shadow"])
    return img


def tower_antenna(seed):
    """Torre de comunicaciones: estructura de celosía, antena parabólica y luces rojas."""
    rnd = random.Random(seed)
    w, h = 36, 200
    img = Image.new("RGBA", (w, h), CLEAR)
    rect(img, 8, 70, 27, h - 1, P["indigo"])
    rect(img, 10, 70, 25, h - 1, P["violet"])
    window_grid(img, 12, 76, 23, h - 6, rnd, [P["cyan"], P["sky"], P["mint"]], p=0.45, step=(3, 4))
    # celosía
    for y in range(8, 70):
        half = 2 + (y - 8) // 9
        put(img, 17 - half, y, P["royal"]); put(img, 18 + half, y, P["royal"])
        if y % 6 == 0:
            rect(img, 17 - half, y, 18 + half, y, P["royal"])
            put(img, 17, y + 2, P["royal"]); put(img, 18, y + 2, P["royal"])
    rect(img, 17, 0, 18, 8, P["royal"])
    for y in (0, 22, 44):
        put(img, 17, y, P["coral"]); put(img, 18, y, P["coral"])
    # parabólica
    for i in range(9):
        rect(img, 24 + i // 2, 46 + i, 26 + i // 2, 46 + i, P["lilac"])
    rect(img, 22, 50, 25, 51, P["royal"])
    outline(img, P["shadow"])
    return img


def top_billboard(seed):
    """Pantalla gigante colgando de dos cables (obstáculo superior)."""
    rnd = random.Random(seed)
    w, h = 50, 180
    img = Image.new("RGBA", (w, h), CLEAR)
    rect(img, 10, 0, 10, h - 46, P["violet"])
    rect(img, 39, 0, 39, h - 46, P["violet"])
    rect(img, 1, h - 46, 48, h - 4, P["royal"])
    rect(img, 3, h - 44, 46, h - 6, P["shadow"])
    # contenido: "2077" a bloques y barras
    digits = {"2": ["###", "..#", "###", "#..", "###"], "0": ["###", "#.#", "#.#", "#.#", "###"], "7": ["###", "..#", ".#.", ".#.", ".#."]}
    x = 7
    for ch in "2077":
        for r, row in enumerate(digits[ch]):
            for k, v in enumerate(row):
                if v == "#":
                    rect(img, x + k * 2, h - 40 + r * 2, x + k * 2 + 1, h - 39 + r * 2, P["pink"])
        x += 9
    for yy in range(h - 26, h - 8, 3):
        ln = rnd.randint(14, 38)
        rect(img, 6, yy, 6 + ln, yy, P["cyan"] if yy % 2 else P["mint"])
    for x in range(4, 46, 5):
        put(img, x, h - 3, P["amber"])
    outline(img, P["shadow"])
    return img


def top_cargo(seed):
    """Contenedor de carga colgando del gancho de una grúa (obstáculo superior)."""
    w, h = 44, 170
    img = Image.new("RGBA", (w, h), CLEAR)
    rect(img, 21, 0, 22, h - 44, P["violet"])
    # gancho
    rect(img, 18, h - 44, 25, h - 40, P["royal"])
    rect(img, 6, h - 40, 37, h - 39, P["indigo"])
    for i in range(6):
        put(img, 21 - i * 2, h - 40 - i // 2, P["indigo"])
        put(img, 22 + i * 2, h - 40 - i // 2, P["indigo"])
    # contenedor corrugado
    rect(img, 2, h - 38, 41, h - 2, P["wine"])
    for x in range(4, 40, 3):
        rect(img, x, h - 36, x, h - 4, P["plum"])
    rect(img, 2, h - 38, 41, h - 37, P["coral"])
    rect(img, 2, h - 3, 41, h - 2, P["shadow"])
    rect(img, 8, h - 26, 35, h - 16, P["shadow"])
    rect(img, 10, h - 24, 33, h - 18, P["amber"])
    for x in range(12, 32, 4):
        rect(img, x, h - 23, x + 1, h - 19, P["shadow"])
    put(img, 3, h - 2, P["coral"]); put(img, 40, h - 2, P["coral"])
    outline(img, P["shadow"])
    return img


def top_monorail(seed):
    """Viga del monorriel con un tren pasando por debajo (obstáculo superior ancho)."""
    rnd = random.Random(seed)
    w, h = 60, 160
    img = Image.new("RGBA", (w, h), CLEAR)
    rect(img, 26, 0, 33, h - 40, P["royal"])
    rect(img, 28, 0, 31, h - 40, P["indigo"])
    rect(img, 0, h - 42, 59, h - 34, P["royal"])
    rect(img, 0, h - 41, 59, h - 41, P["sky"])
    # tren colgante
    rect(img, 2, h - 33, 57, h - 6, P["violet"])
    rect(img, 2, h - 33, 57, h - 31, P["lilac"])
    for x in range(5, 55, 7):
        rect(img, x, h - 27, x + 4, h - 20, P["cyan"] if rnd.random() < 0.8 else P["amber"])
        rect(img, x, h - 27, x + 4, h - 27, P["white"])
    rect(img, 2, h - 15, 57, h - 13, P["pink"])
    rect(img, 2, h - 5, 57, h - 3, P["shadow"])
    put(img, 2, h - 18, P["amber"]); put(img, 57, h - 18, P["coral"])
    outline(img, P["shadow"])
    return img


def buildings():
    global WIN
    WIN = [P["cyan"], P["cyan"], P["mint"], P["coral"], P["rose"], P["royal"]]
    save(tower_neon(301), "bld_neon")
    save(tower_holo(302), "bld_holo")
    save(tower_capsule(303), "bld_capsule")
    save(platform_hanging(304), "top_hanging")
    save(platform_bridge(305), "top_bridge")
    save(tower_pagoda(306), "bld_pagoda")
    save(tower_corp(307), "bld_corp")
    save(tower_shops(308), "bld_shops")
    save(tower_antenna(309), "bld_antenna")
    save(top_billboard(310), "top_billboard")
    save(top_cargo(311), "top_cargo")
    save(top_monorail(312), "top_monorail")


def sky_objects():
    """Dirigible con pantalla y dron de reparto para el fondo."""
    img = Image.new("RGBA", (48, 18), CLEAR)
    for y in range(18):
        for x in range(48):
            if ((x - 23.5) / 23.5) ** 2 + ((y - 7.5) / 7.5) ** 2 <= 1:
                put(img, x, y, P["indigo"] if y > 4 else P["violet"])
    rect(img, 12, 4, 35, 10, P["shadow"])
    for x in range(13, 35, 3):
        rect(img, x, 5, x + 1, 9, P["pink"] if (x // 3) % 2 else P["cyan"])
    rect(img, 19, 15, 28, 17, P["royal"])
    put(img, 20, 17, P["amber"]); put(img, 27, 17, P["amber"])
    rect(img, 0, 6, 2, 9, P["royal"])
    save(img, "bg_blimp")

    dr = Image.new("RGBA", (11, 6), CLEAR)
    rect(dr, 3, 2, 7, 4, P["violet"])
    rect(dr, 0, 1, 3, 1, P["lilac"]); rect(dr, 7, 1, 10, 1, P["lilac"])
    put(dr, 5, 5, P["coral"])
    rect(dr, 4, 3, 6, 3, P["cyan"])
    save(dr, "bg_drone")


# ---------------------------------------------------------------- arcade: chips y power-ups


def chips():
    """Chips de neón: +1 (cian), +5 (rosa), +10 (dorado). Hexágono con brillo y número."""
    specs = {
        "chip_1": (9, P["cyan"], P["white"], P["royal"]),
        "chip_5": (11, P["pink"], P["rose"], P["plum"]),
        "chip_10": (13, P["amber"], P["white"], hx("7a4a0a")),
    }
    for name, (s_, base, hi, dark) in specs.items():
        img = Image.new("RGBA", (s_, s_), CLEAR)
        c = (s_ - 1) / 2
        for y in range(s_):
            for x in range(s_):
                # hexágono: |dx| + |dy|/2 <= r
                if abs(x - c) * 0.87 + abs(y - c) * 0.5 <= c * 0.87 and abs(y - c) <= c:
                    put(img, x, y, base)
        outline_ring = img.copy().load()
        px = img.load()
        for y in range(s_):
            for x in range(s_):
                if outline_ring[x, y][3] and any(
                    not (0 <= x + dx < s_ and 0 <= y + dy < s_) or outline_ring[x + dx, y + dy][3] == 0
                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                ):
                    px[x, y] = dark
        put(img, int(c) - 1, int(c) - 2, hi)
        put(img, int(c) - 2, int(c) - 1, hi)
        rect(img, int(c), int(c) - 1, int(c), int(c) + 1, dark)
        save(img, name)


PU_ICONS = {
    "shield": ["..###..", ".#####.", "#######", "#######", ".#####.", "..###..", "...#..."],
    "magnet": ["##...##", "##...##", "##...##", "##...##", "##...##", ".#####.", "..###.."],
    "slow": ["#######", ".#...#.", "..#.#..", "...#...", "..#.#..", ".#...#.", "#######"],
    "double": ["#.#.###", "#.#...#", ".#..###", "#.#.#..", "#.#.###", ".......", "......."],
}
PU_COLORS = {"shield": P["cyan"], "magnet": P["coral"], "slow": P["sky"], "double": P["amber"]}


def powerups():
    """Cápsulas de power-up 15x15: borde de neón y su ícono."""
    for name, rows in PU_ICONS.items():
        col = PU_COLORS[name]
        img = Image.new("RGBA", (15, 15), CLEAR)
        rect(img, 1, 0, 13, 14, P["shadow"])
        rect(img, 0, 1, 14, 13, P["shadow"])
        rect(img, 1, 1, 13, 13, (13, 3, 38, 255))
        for i in range(1, 14):
            put(img, i, 1, col); put(img, i, 13, col); put(img, 1, i, col); put(img, 13, i, col)
        for y, row in enumerate(rows):
            for x, v in enumerate(row):
                if v == "#":
                    put(img, 4 + x, 4 + y, P["white"])
        save(img, "pu_" + name)
        # icono suelto para el HUD
        ic = Image.new("RGBA", (7, 7), CLEAR)
        for y, row in enumerate(rows):
            for x, v in enumerate(row):
                if v == "#":
                    put(ic, x, y, col)
        save(ic, "ic_pu_" + name)


# ---------------------------------------------------------------- jefes


def boss_drone():
    """Dron patrulla policial: casco con luces rojo/azul, cañón al frente (mira a la izquierda)."""
    w, h = 44, 28
    img = Image.new("RGBA", (w, h), CLEAR)
    for y in range(6, 22):
        for x in range(6, 40):
            if ((x - 23) / 17) ** 2 + ((y - 14) / 8) ** 2 <= 1:
                put(img, x, y, P["royal"] if y < 14 else P["indigo"])
    rect(img, 12, 10, 34, 12, P["sky"])
    rect(img, 14, 16, 32, 17, P["violet"])
    # rotores
    for x0 in (2, 30):
        rect(img, x0, 3, x0 + 11, 4, P["lilac"])
        rect(img, x0 + 5, 5, x0 + 6, 7, P["royal"])
    # sirenas
    rect(img, 16, 5, 20, 6, P["coral"]); rect(img, 26, 5, 30, 6, P["sky"])
    # cañón
    rect(img, 0, 13, 7, 15, P["shadow"]); rect(img, 0, 14, 3, 14, P["coral"])
    # ojo
    rect(img, 9, 12, 13, 15, P["coral"]); put(img, 10, 13, P["white"])
    rect(img, 18, 22, 28, 24, P["royal"]); put(img, 20, 25, P["amber"]); put(img, 26, 25, P["amber"])
    outline(img, P["shadow"])
    save(img, "boss_drone")


def boss_gunship():
    """Cañonera corporativa: casco anguloso magenta con dos cañones y motor de plasma."""
    w, h = 64, 36
    img = Image.new("RGBA", (w, h), CLEAR)
    for y in range(8, 30):
        x0 = 6 + abs(y - 19) * 2 // 3
        rect(img, x0, y, 56, y, P["magenta"] if y < 19 else P["wine"])
    rect(img, 18, 4, 44, 8, P["violet"])
    rect(img, 24, 5, 38, 7, P["cyan"])
    for yy in (13, 25):
        rect(img, 0, yy, 10, yy + 2, P["shadow"]); rect(img, 0, yy + 1, 4, yy + 1, P["pink"])
    for x in range(16, 50, 6):
        rect(img, x, 18, x + 3, 19, P["amber"])
    rect(img, 56, 14, 63, 24, P["royal"])
    rect(img, 58, 16, 63, 22, P["cyan"]); rect(img, 61, 17, 63, 21, P["white"])
    rect(img, 30, 30, 36, 33, P["royal"])
    outline(img, P["shadow"])
    save(img, "boss_gunship")


def boss_worm():
    """Gusano del metro aéreo: cabeza de locomotora y segmentos-vagón."""
    head = Image.new("RGBA", (30, 24), CLEAR)
    rect(head, 6, 4, 29, 21, P["violet"])
    for i in range(6):
        rect(head, 6 - i, 4 + i, 6 - i, 21 - i, P["violet"])
    rect(head, 2, 9, 9, 14, P["cyan"]); rect(head, 3, 10, 5, 11, P["white"])
    rect(head, 12, 7, 27, 12, P["shadow"])
    for x in range(13, 27, 4):
        rect(head, x, 8, x + 2, 11, P["amber"])
    rect(head, 6, 17, 29, 18, P["pink"])
    rect(head, 0, 15, 4, 16, P["coral"])  # mandíbula de neón
    outline(head, P["shadow"])
    save(head, "boss_worm_head")
    seg = Image.new("RGBA", (22, 18), CLEAR)
    rect(seg, 1, 2, 20, 15, P["indigo"])
    rect(seg, 1, 2, 20, 3, P["lilac"])
    for x in range(3, 19, 5):
        rect(seg, x, 6, x + 3, 10, P["cyan"])
    rect(seg, 1, 12, 20, 12, P["pink"])
    outline(seg, P["shadow"])
    save(seg, "boss_worm_seg")


def projectiles():
    shot = Image.new("RGBA", (8, 3), CLEAR)
    rect(shot, 0, 1, 7, 1, P["cyan"]); rect(shot, 4, 0, 7, 2, P["white"])
    save(shot, "shot_player")
    e = Image.new("RGBA", (6, 6), CLEAR)
    rect(e, 1, 0, 4, 5, P["pink"]); rect(e, 0, 1, 5, 4, P["pink"]); rect(e, 2, 2, 3, 3, P["white"])
    save(e, "shot_enemy")
    m = Image.new("RGBA", (11, 11), CLEAR)
    for y in range(11):
        for x in range(11):
            if (x - 5) ** 2 + (y - 5) ** 2 <= 16:
                put(m, x, y, P["plum"])
    for (x, y) in ((5, 0), (5, 10), (0, 5), (10, 5), (2, 2), (8, 8), (2, 8), (8, 2)):
        put(m, x, y, P["coral"])
    rect(m, 4, 4, 6, 6, P["amber"])
    save(m, "mine")


# ---------------------------------------------------------------- UI


def panel():
    """Panel 24x24 (9-slice con borde de 6 px): fondo translúcido y doble borde de neón."""
    s = 24
    img = Image.new("RGBA", (s, s), CLEAR)
    rect(img, 1, 1, s - 2, s - 2, (13, 3, 38, 225))
    for i, c in ((0, P["pink"]), (2, P["cyan"])):
        rect(img, i, i, s - 1 - i, i, c)
        rect(img, i, s - 1 - i, s - 1 - i, s - 1 - i, c)
        rect(img, i, i, i, s - 1 - i, c)
        rect(img, s - 1 - i, i, s - 1 - i, s - 1 - i, c)
    # esquinas recortadas (look de terminal)
    for (x, y) in ((0, 0), (s - 1, 0), (0, s - 1), (s - 1, s - 1)):
        put(img, x, y, CLEAR)
    save(img, "ui_panel")


def button(name, fill, edge, hi):
    s = 16
    img = Image.new("RGBA", (s, s), CLEAR)
    rect(img, 1, 0, s - 2, s - 1, edge)
    rect(img, 0, 1, s - 1, s - 2, edge)
    rect(img, 1, 1, s - 2, s - 3, fill)
    rect(img, 2, 1, s - 3, 1, hi)
    rect(img, 1, s - 2, s - 2, s - 2, lerp(fill, P["shadow"], 0.5))
    save(img, name)


def icon(name, rows, color=P["white"]):
    h = len(rows)
    w = max(len(r) for r in rows)
    img = Image.new("RGBA", (w, h), CLEAR)
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch == "#":
                put(img, x, y, color)
            elif ch == "+":
                put(img, x, y, (color[0], color[1], color[2], 150))
    save(img, name)


ICONS = {
    "ic_pause": [
        "..........",
        ".###..###.",
        ".###..###.",
        ".###..###.",
        ".###..###.",
        ".###..###.",
        ".###..###.",
        ".###..###.",
        ".###..###.",
        "..........",
    ],
    "ic_gear": [
        "....##....",
        ".#.####.#.",
        "..######..",
        ".###..###.",
        "####..####",
        "####..####",
        ".###..###.",
        "..######..",
        ".#.####.#.",
        "....##....",
    ],
    "ic_music": [
        "....######",
        "....######",
        "....#....#",
        "....#....#",
        "....#....#",
        "....#..###",
        ".####.####",
        "#####.####",
        "#####..##.",
        ".###......",
    ],
    "ic_sound": [
        "....#.....",
        "...##..#..",
        "..###...#.",
        "####..#.#.",
        "####..#.#.",
        "####..#.#.",
        "####..#.#.",
        "..###...#.",
        "...##..#..",
        "....#.....",
    ],
    "ic_globe": [
        "..######..",
        ".#..##..#.",
        "#..#..#..#",
        "##########",
        "#..#..#..#",
        "#..#..#..#",
        "##########",
        "#..#..#..#",
        ".#..##..#.",
        "..######..",
    ],
    "ic_trophy": [
        ".########.",
        "##########",
        "#.######.#",
        "#.######.#",
        ".########.",
        "..######..",
        "....##....",
        "....##....",
        "..######..",
        "..######..",
    ],
    "ic_vibe": [
        "..######..",
        "..#....#..",
        "#.#....#.#",
        "#.#....#.#",
        "..#....#..",
        "#.#....#.#",
        "#.#....#.#",
        "..#....#..",
        "..#.##.#..",
        "..######..",
    ],
}


def medals():
    """Medallas 18x18: chip hexagonal con brillo. Rango: bronce, plata, oro, neón."""
    sets = {
        "medal_bronze": (hx("c56a3c"), hx("ffb09a"), hx("6b2f1a")),
        "medal_silver": (hx("a8b4d8"), hx("f4ecff"), hx("4a4f6b")),
        "medal_gold": (hx("f2b632"), hx("fff0a6"), hx("7a4a0a")),
        "medal_neon": (P["cyan"], P["white"], P["royal"]),
    }
    shape = [
        "......######......",
        "....##########....",
        "...############...",
        "..##############..",
        ".################.",
        ".################.",
        "##################",
        "##################",
        "##################",
        "##################",
        "##################",
        "##################",
        ".################.",
        ".################.",
        "..##############..",
        "...############...",
        "....##########....",
        "......######......",
    ]
    star = [
        "...#...",
        "...#...",
        "..###..",
        "#######",
        ".#####.",
        ".##.##.",
        "#.....#",
    ]
    for name, (base, hi, dark) in sets.items():
        img = Image.new("RGBA", (18, 18), CLEAR)
        for y, row in enumerate(shape):
            for x, ch in enumerate(row):
                if ch == "#":
                    put(img, x, y, base)
        outline_ring = img.copy()
        # borde oscuro y brillo arriba-izquierda
        for y in range(18):
            for x in range(18):
                if shape[y][x] == "#":
                    edge = any(
                        not (0 <= x + dx < 18 and 0 <= y + dy < 18) or shape[y + dy][x + dx] != "#"
                        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                    )
                    if edge:
                        put(img, x, y, dark)
        for i in range(3, 8):
            put(img, i, 11 - i, hi)
        for y, row in enumerate(star):
            for x, ch in enumerate(row):
                if ch == "#":
                    put(img, 6 + x, 6 + y, dark)
        save(img, name)


def ui():
    panel()
    button("ui_button", P["magenta"], P["shadow"], P["lilac"])
    button("ui_button_alt", P["royal"], P["shadow"], P["sky"])
    for name, rows in ICONS.items():
        icon(name, rows)
    medals()


# ---------------------------------------------------------------- audio de UI


def synth(name, notes, vol=0.35, rate=22050):
    """Blip de onda cuadrada con envolvente; notes: [(frecuencia, segundos), ...]."""
    os.makedirs(AUDIO_OUT, exist_ok=True)
    frames = bytearray()
    for freq, dur in notes:
        n = int(rate * dur)
        for i in range(n):
            t = i / rate
            env = min(1.0, i / (rate * 0.004)) * math.exp(-5.0 * t / dur)
            sq = 1.0 if math.sin(2 * math.pi * freq * t) >= 0 else -1.0
            tri = 2 / math.pi * math.asin(math.sin(2 * math.pi * freq * 2 * t))
            v = (0.6 * sq + 0.4 * tri) * env * vol
            frames += struct.pack("<h", int(max(-1, min(1, v)) * 32767))
    with wave.open(os.path.join(AUDIO_OUT, name + ".wav"), "wb") as wv:
        wv.setnchannels(1)
        wv.setsampwidth(2)
        wv.setframerate(rate)
        wv.writeframes(bytes(frames))


def audio():
    synth("ui_click", [(880, 0.05), (1320, 0.06)])
    synth("ui_back", [(660, 0.05), (440, 0.07)])
    synth("ui_toggle", [(1046, 0.04)], vol=0.25)
    synth("new_best", [(523, 0.09), (659, 0.09), (784, 0.09), (1046, 0.22)], vol=0.3)
    synth("chip_1", [(1568, 0.04), (2093, 0.05)], vol=0.18)
    synth("chip_5", [(1175, 0.04), (1568, 0.04), (2349, 0.07)], vol=0.22)
    synth("chip_10", [(1047, 0.05), (1319, 0.05), (1568, 0.05), (2093, 0.1)], vol=0.25)
    synth("powerup", [(392, 0.06), (523, 0.06), (659, 0.06), (784, 0.06), (1047, 0.14)], vol=0.28)
    synth("shield_break", [(330, 0.06), (220, 0.08), (147, 0.12)], vol=0.32)
    synth("perfect", [(1319, 0.05), (1760, 0.08)], vol=0.2)
    synth("alarm", [(880, 0.18), (660, 0.18), (880, 0.18), (660, 0.18)], vol=0.25)
    synth("boss_hit", [(220, 0.03)], vol=0.15)
    synth("boss_down", [(196, 0.1), (147, 0.12), (110, 0.2), (523, 0.08), (784, 0.2)], vol=0.32)


if __name__ == "__main__":
    background()
    traffic()
    sky_objects()
    buildings()
    chips()
    powerups()
    boss_drone()
    boss_gunship()
    boss_worm()
    projectiles()
    ui()
    audio()
    print("Arte generado en", OUT)
