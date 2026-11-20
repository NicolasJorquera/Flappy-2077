"""
Gráficos de tienda de Flappy 2077 (gráfico destacado 1024x500 e ícono 512x512) armados con el
mismo arte del juego: cielo, luna, skyline, edificios pixel-art, el auto y la fuente pixel-art
del menú (los glifos se leen de Assets/Scripts/UI/PixelFont.cs para que sean idénticos).

    Tools/art/.venv/bin/python Tools/art/generate.py        # primero, si cambió el arte
    Tools/art/.venv/bin/python Tools/art/store_graphics.py
Salida: build/play-games/
"""
import os
import re

from PIL import Image, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
PIX = os.path.join(ROOT, "Assets", "Resources", "Pixel")
OUT = os.path.join(ROOT, "build", "play-games")

PINK = (255, 50, 161)
CYAN = (64, 244, 210)
WHITE = (244, 236, 255)
PURPLE_GLOW = (90, 0, 122)
AMBER = (255, 211, 107)


def load(name):
    return Image.open(os.path.join(PIX, name + ".png")).convert("RGBA")


# ------------------------------------------------------------------ fuente pixel-art


def load_glyphs():
    src = open(os.path.join(ROOT, "Assets", "Scripts", "UI", "PixelFont.cs"), encoding="utf-8").read()
    glyphs = {}
    for ch, body in re.findall(r"\{\s*'(\\?.)',\s*new\[\]\s*\{([^}]*)\}\s*\}", src):
        rows = re.findall(r'"([.#]*)"', body)
        if len(rows) == 7:
            glyphs[ch.replace("\\", "")] = rows
    return glyphs


GLYPHS = load_glyphs()


def text_size(text, px):
    w = 0
    for c in text:
        w += (3 if c == " " else len(GLYPHS[c][0])) + 1
    return (w - 1) * px, 7 * px


def draw_text(img, text, x, y, px, color, glow=None, shadow=(10, 4, 32, 230)):
    """Texto pixel-art con sombra dura y halo de neón suave, como PixelText en el juego."""
    def stamp(target, ox, oy, col):
        cx = x + ox
        for c in text:
            if c == " ":
                cx += 4 * px
                continue
            rows = GLYPHS[c]
            for r, row in enumerate(rows):
                for k, v in enumerate(row):
                    if v == "#":
                        target.paste(col, (cx + k * px, y + oy + r * px, cx + (k + 1) * px, y + oy + (r + 1) * px))
            cx += (len(rows[0]) + 1) * px

    if glow:
        halo = Image.new("RGBA", img.size, (0, 0, 0, 0))
        stamp(halo, 0, 0, glow + (200,))
        img.alpha_composite(halo.filter(ImageFilter.GaussianBlur(px * 1.6)))
        img.alpha_composite(halo.filter(ImageFilter.GaussianBlur(px * 0.6)))
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    off = max(2, px // 3)
    stamp(layer, off, off, shadow)
    stamp(layer, 0, 0, color + (255,))
    img.alpha_composite(layer)


# ------------------------------------------------------------------ escena


def scene(w, h, scale):
    """Escena a resolución de arte (w x h) que luego se amplía x scale sin suavizado."""
    base = Image.new("RGBA", (w, h))
    sky = load("bg_sky")
    sky = sky.resize((max(w, sky.width), h), Image.NEAREST).crop((0, 0, w, h))
    base.alpha_composite(sky)

    moon = load("bg_moon")
    base.alpha_composite(moon, (w - moon.width - int(w * 0.08), int(h * 0.1)))

    far = load("bg_far")
    base.alpha_composite(far.crop((0, 0, w, far.height)), (0, h - far.height + 18))
    mid = load("bg_mid")
    base.alpha_composite(mid.crop((40, 0, 40 + w, mid.height)), (0, h - mid.height + 34))

    # Edificios de primer plano a la derecha y estructura colgante arriba (como en el juego).
    holo = load("bld_holo")
    base.alpha_composite(holo, (w - 52, h - 95))
    neon = load("bld_neon")
    base.alpha_composite(neon, (w - 100, h - 70))

    # Tráfico lejano.
    for name, (x, y) in (("traffic_b", (int(w * 0.62), 40)), ("traffic_a", (int(w * 0.80), 70)), ("traffic_c", (int(w * 0.55), 64))):
        base.alpha_composite(load(name), (x, y))

    # Bruma de neón al pie.
    haze = Image.new("RGBA", (w, 30), (0, 0, 0, 0))
    for yy in range(30):
        a = int(110 * (yy / 29) ** 1.5)
        haze.paste((158, 33, 217, a), (0, yy, w, yy + 1))
    base.alpha_composite(haze, (0, h - 30))
    return base.resize((w * scale, h * scale), Image.NEAREST)


def car_sprite(width):
    car = Image.open(os.path.join(ROOT, "Assets", "car", "upCar.png")).convert("RGBA")
    car = car.crop(car.getbbox())
    ratio = width / car.width
    return car.resize((width, int(car.height * ratio)), Image.LANCZOS)


def trail(img, x, y, length, px):
    """Estela de píxeles rosa/cian detrás del auto."""
    import random

    rnd = random.Random(7)
    for i in range(length):
        t = i / length
        for _ in range(2):
            cx = x - i * px * 1.6 - rnd.randint(0, px)
            cy = y + rnd.randint(-px * 2, px * 2) + int(t * px * 6)
            col = PINK if rnd.random() < 0.55 else CYAN
            a = int(255 * (1 - t) ** 1.3)
            s = px if rnd.random() < 0.7 else px * 2
            img.alpha_composite(Image.new("RGBA", (s, s), col + (a,)), (int(cx), int(cy)))


def feature_graphic():
    W, H, S = 1024, 500, 4
    img = scene(W // S, H // S, S)

    # Viñeta oscura a la izquierda para que el título se lea.
    shade = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for x in range(W):
        a = int(170 * max(0.0, 1 - x / (W * 0.62)) ** 1.4)
        shade.paste((8, 2, 24, a), (x, 0, x + 1, H))
    img.alpha_composite(shade)

    # Auto con estela, a la derecha del título, volando hacia la derecha.
    car = car_sprite(250)
    cx, cy = 600, 150
    trail(img, cx + 40, cy + car.height // 2 + 6, 26, 6)
    glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    glow.alpha_composite(Image.new("RGBA", car.size, PINK + (120,)), (cx, cy + 18))
    img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(28)))
    img.alpha_composite(car.rotate(6, resample=Image.BICUBIC, expand=True), (cx, cy - 10))

    # Título y tagline.
    draw_text(img, "FLAPPY", 64, 112, 13, WHITE, glow=(155, 33, 217))
    draw_text(img, "2077", 64, 222, 19, PINK, glow=(255, 50, 161))
    draw_text(img, "NEO CITY NIGHT RUN", 68, 380, 5, CYAN)
    return img.convert("RGB")


def game_icon():
    """Play Games pide el mismo ícono que la app en Google Play: el logo actual."""
    logo = Image.open(os.path.join(ROOT, "Assets", "Logos", "Flappy 2077 logo (3).png")).convert("RGB")
    return logo.resize((512, 512), Image.LANCZOS)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    feature_graphic().save(os.path.join(OUT, "feature-graphic-1024x500.png"))
    game_icon().save(os.path.join(OUT, "game-icon-512.png"))
    print("Gráficos en", OUT)
