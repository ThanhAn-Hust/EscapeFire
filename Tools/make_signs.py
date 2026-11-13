"""Draws the Vietnamese fire-safety signs used in the drill building (ISO 7010 style) -> Assets/Art/Signs.
Pictograms are drawn from simple shapes here (no third-party art). Re-run after editing; Unity re-imports the PNGs."""
import math
import os
from PIL import Image, ImageDraw, ImageFont

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Art", "Signs")
GREEN = (0, 132, 61)
RED = (204, 20, 26)
WHITE = (255, 255, 255)
DARK = (35, 35, 35)
FONT_B = "C:/Windows/Fonts/arialbd.ttf"
FONT = "C:/Windows/Fonts/arial.ttf"


def font(size, bold=True):
    return ImageFont.truetype(FONT_B if bold else FONT, size)


def limb(d, pts, w, fill):
    d.line(pts, fill=fill, width=w, joint="curve")
    r = w // 2
    for x, y in pts:
        d.ellipse([x - r, y - r, x + r, y + r], fill=fill)


def runner(d, cx, cy, s, fill, facing=1):
    """Running figure about 2*s tall centred at (cx, cy); facing +1 = right."""
    f = facing
    P = lambda x, y: (cx + f * x * s, cy + y * s)
    w = max(4, int(s * 0.22))
    hx, hy = P(0.32, -0.92)
    r = s * 0.17
    d.ellipse([hx - r, hy - r, hx + r, hy + r], fill=fill)
    limb(d, [P(0.18, -0.62), P(-0.08, 0.05)], int(w * 1.25), fill)      # torso, leaning forward
    limb(d, [P(0.15, -0.55), P(0.45, -0.32), P(0.72, -0.48)], w, fill)   # front arm
    limb(d, [P(0.12, -0.55), P(-0.2, -0.38), P(-0.42, -0.58)], w, fill)  # back arm
    limb(d, [P(-0.08, 0.05), P(0.32, 0.32), P(0.28, 0.82)], w, fill)      # front leg
    limb(d, [P(-0.08, 0.05), P(-0.3, 0.42), P(-0.68, 0.5)], w, fill)      # back leg


def arrow(d, cx, cy, s, direction, fill):
    """Thick arrow of half-length s; direction left | right | down | up."""
    shaft, head = s * 0.32, s * 0.75
    pts = [(-s, -shaft), (s - head, -shaft), (s - head, -s * 0.8), (s, 0), (s - head, s * 0.8), (s - head, shaft), (-s, shaft)]
    a = math.radians({"right": 0, "down": 90, "left": 180, "up": 270}[direction])
    d.polygon([(cx + x * math.cos(a) - y * math.sin(a), cy + x * math.sin(a) + y * math.cos(a)) for x, y in pts], fill=fill)


def centred(d, box, text, fnt, fill):
    x0, y0, x1, y1 = box
    l, t, r, b = d.textbbox((0, 0), text, font=fnt)
    d.text(((x0 + x1 - (r - l)) / 2 - l, (y0 + y1 - (b - t)) / 2 - t), text, font=fnt, fill=fill)


def exit_sign(direction):
    """Lightbox face 1024x384: running man leaving through a door, arrow, LỐI THOÁT / EXIT."""
    W, H = 1024, 384
    im = Image.new("RGB", (W, H), GREEN)
    d = ImageDraw.Draw(im)
    d.rectangle([10, 10, W - 11, H - 11], outline=WHITE, width=8)
    left = direction == "left"
    if left:  # pictogram, arrow, text from the left
        d.rectangle([60, 60, 160, 324], outline=WHITE, width=12)
        runner(d, 250, 202, 115, WHITE, -1)
        arrow(d, 430, 192, 66, "left", WHITE)
        tx0, tx1 = 520, W - 30
    else:
        d.rectangle([W - 160, 60, W - 60, 324], outline=WHITE, width=12)
        runner(d, W - 250, 202, 115, WHITE, 1)
        arrow(d, W - 430, 192, 66, direction, WHITE)
        tx0, tx1 = 30, W - 520
    centred(d, (tx0, 40, tx1, 220), "LỐI THOÁT", font(92), WHITE)
    centred(d, (tx0, 215, tx1, H - 40), "EXIT", font(84, False), WHITE)
    im.save(os.path.join(OUT, f"EF_Sign_Exit_{direction.capitalize()}.png"))


def assembly():
    W, H = 768, 960
    im = Image.new("RGB", (W, H), GREEN)
    d = ImageDraw.Draw(im)
    d.rectangle([12, 12, W - 13, H - 13], outline=WHITE, width=10)
    c, cy = W / 2, 300
    for direction, (x, y) in {"right": (c - 240, cy), "left": (c + 240, cy), "down": (c, cy - 200), "up": (c, cy + 220)}.items():
        arrow(d, x, y, 62, direction, WHITE)
    for dx, s in ((-62, 52), (62, 52), (0, 62)):
        hx, hy = c + dx, cy - s * 0.9
        d.ellipse([hx - s * 0.32, hy - s * 0.32, hx + s * 0.32, hy + s * 0.32], fill=WHITE)
        d.rounded_rectangle([hx - s * 0.42, hy + s * 0.42, hx + s * 0.42, hy + s * 1.9], radius=int(s * 0.3), fill=WHITE)
    centred(d, (30, 650, W - 30, 790), "ĐIỂM TẬP KẾT", font(92), WHITE)
    centred(d, (30, 780, W - 30, 900), "ASSEMBLY POINT", font(62, False), WHITE)
    im.save(os.path.join(OUT, "EF_Sign_Assembly.png"))


def elevator_no():
    W, H = 768, 960
    im = Image.new("RGB", (W, H), WHITE)
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, W - 1, H - 1], outline=RED, width=24)
    c, cy, R = W / 2, 330, 250
    d.rectangle([c - 120, cy - 170, c + 120, cy + 150], outline=DARK, width=14)
    d.line([(c, cy - 170), (c, cy + 150)], fill=DARK, width=8)
    d.polygon([(c - 70, cy - 120), (c - 40, cy - 160), (c - 10, cy - 120)], fill=DARK)
    d.polygon([(c + 10, cy - 160), (c + 40, cy - 120), (c + 70, cy - 160)], fill=DARK)
    d.ellipse([c - 30, cy - 85, c + 30, cy - 25], fill=DARK)
    d.rounded_rectangle([c - 40, cy - 15, c + 40, cy + 120], radius=18, fill=DARK)
    for fx in (-70, 0, 70):
        d.polygon([(c + fx - 28, cy + 200), (c + fx, cy + 158), (c + fx + 28, cy + 200)], fill=RED)
    d.ellipse([c - R, cy - R, c + R, cy + R], outline=RED, width=34)
    a = math.radians(45)
    d.line([(c - R * math.cos(a), cy - R * math.sin(a)), (c + R * math.cos(a), cy + R * math.sin(a))], fill=RED, width=34)
    centred(d, (40, 610, W - 40, 720), "KHÔNG DÙNG", font(84), RED)
    centred(d, (40, 715, W - 40, 815), "THANG MÁY", font(84), RED)
    centred(d, (40, 815, W - 40, 920), "KHI CÓ CHÁY", font(70), DARK)
    im.save(os.path.join(OUT, "EF_Sign_NoElevator.png"))


def escape_plan():
    """Floor-3 escape plan: lab, corridor, stairwells A/B, lift; you-are-here dot; green routes."""
    W, H = 1024, 768
    im = Image.new("RGB", (W, H), WHITE)
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, W - 1, H - 1], outline=GREEN, width=18)
    d.rectangle([18, 18, W - 19, 110], fill=GREEN)
    centred(d, (18, 18, W - 19, 110), "SƠ ĐỒ THOÁT NẠN — TẦNG 3", font(56), WHITE)
    sx = lambda x: 300 + (x + 9) * 24   # world x -> px
    sz = lambda z: 150 + (7 - z) * 20   # world z (north up) -> px
    wall = (60, 60, 60)

    def room(x0, z0, x1, z1, label, fill=(235, 235, 235), at=None):
        d.rectangle([sx(x0), sz(z1), sx(x1), sz(z0)], fill=fill, outline=wall, width=6)
        if label and at is None:
            centred(d, (sx(x0), sz(z1), sx(x1), sz(z0)), label, font(26), DARK)
        elif label:
            d.text((sx(at[0]), sz(at[1])), label, font=font(24), fill=DARK)

    room(-9, -7, 9, 7, "PHÒNG LAB")
    room(-3, -13.2, 3, -7, "HÀNH LANG →", at=(-10.6, -12.2))
    room(-1.3, -18.6, 1.3, -13.2, "CẦU THANG A", (210, 240, 215), at=(1.6, -16.4))
    room(3, -11.8, 8.4, -9.2, "CẦU THANG B", (210, 240, 215), at=(3.3, -12.2))
    room(-5.2, -11.3, -3, -9.7, "", (250, 220, 220))
    d.text((sx(-11.4), sz(-9.6)), "THANG MÁY", font=font(22), fill=RED)
    d.line([(sx(-5.0), sz(-9.9)), (sx(-3.2), sz(-11.1))], fill=RED, width=6)
    d.line([(sx(-5.0), sz(-11.1)), (sx(-3.2), sz(-9.9))], fill=RED, width=6)

    def route(pts):
        p = [(sx(x), sz(z)) for x, z in pts]
        d.line(p, fill=GREEN, width=12)
        (x0, y0), (x1, y1) = p[-2], p[-1]
        ang = math.atan2(y1 - y0, x1 - x0)
        d.polygon([(x1 + 24 * math.cos(ang + k), y1 + 24 * math.sin(ang + k)) for k in (0, 2.4, -2.4)], fill=GREEN)

    route([(0, -2), (0, -10), (0, -14.8)])
    route([(0.3, -10.5), (4.8, -10.5)])
    yx, yz = 2.2, -7.6  # on the corridor wall by the lab door
    d.ellipse([sx(yx) - 16, sz(yz) - 16, sx(yx) + 16, sz(yz) + 16], fill=RED)
    d.text((sx(yx) + 24, sz(yz) - 14), "BẠN ĐANG Ở ĐÂY", font=font(24), fill=RED)
    d.line([(60, 715), (130, 715)], fill=GREEN, width=12)
    d.text((145, 699), "Lối thoát nạn", font=font(26, False), fill=DARK)
    d.text((390, 699), "Không dùng thang máy khi có cháy • Gọi 114", font=font(26, False), fill=DARK)
    im.save(os.path.join(OUT, "EF_Sign_EscapePlan.png"))


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for direction in ("left", "right", "down"):
        exit_sign(direction)
    assembly()
    elevator_no()
    escape_plan()
    print("signs ->", os.path.abspath(OUT))
