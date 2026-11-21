"""
Colores del auto: recolorea upCar/downCar cambiando el tono de la carrocería (naranja) y del
acento (lavanda); el vidrio cian y el contorno oscuro se mantienen. Se exporta a 1/4 de
resolución (480x270) y Unity lo importa con 25 px/unidad: mismo tamaño en pantalla que el
original, mucho menos memoria. Ver Assets/Scripts/Progression/CarSkins.cs.

    Tools/art/.venv/bin/python Tools/art/skins.py
"""
import colorsys
import os

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Skins")

# índice: (tono carrocería, tono acento, factor saturación, factor brillo)
SKINS = {
    1: (330, 190, 1.0, 1.0),   # neón rosa
    2: (190, 225, 0.8, 1.05),  # cian hielo
    3: (95, 290, 1.0, 1.0),    # tóxico
    4: (46, 28, 1.2, 1.1),     # oro
    5: (245, 300, 0.9, 0.62),  # medianoche
    6: (355, 25, 1.1, 0.95),   # carmesí
    7: (180, 300, 0.35, 1.15), # holo (se colorea en tiempo real)
}


def recolor(img, body, accent, sat_k, val_k):
    px = img.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            hh, ss, vv = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            deg = hh * 360
            if ss < 0.2 or vv < 0.3:
                continue  # contorno oscuro y blancos
            if 15 <= deg <= 70:
                target = body
            elif 245 <= deg <= 315:
                target = accent
            else:
                continue  # vidrio cian, luces
            nr, ng, nb = colorsys.hsv_to_rgb(target / 360, min(1, ss * sat_k), min(1, vv * val_k))
            px[x, y] = (int(nr * 255), int(ng * 255), int(nb * 255), a)
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    for state in ("up", "down"):
        src = Image.open(os.path.join(ROOT, "Assets", "car", f"{state}Car.png")).convert("RGBA")
        small = src.resize((480, 270), Image.LANCZOS)
        for idx, (body, accent, sk, vk) in SKINS.items():
            recolor(small.copy(), body, accent, sk, vk).save(os.path.join(OUT, f"skin{idx}_{state}.png"))
    print("Colores en", OUT)


if __name__ == "__main__":
    main()
