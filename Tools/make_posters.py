"""Draws the training-centre posters (menu / quiz / AAR / lesson backdrop) -> Assets/Art/Signs.
Own artwork from simple shapes; Vietnamese text in Arial. Re-run after editing."""
import math
import os

from PIL import Image, ImageDraw, ImageFilter, ImageFont

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Art", "Signs")
RED = (200, 30, 30)
ORANGE = (240, 120, 30)
YELLOW = (255, 200, 40)
WHITE = (255, 255, 255)
DARK = (30, 32, 38)
NAVY = (18, 40, 78)
GREEN = (0, 132, 61)


def font(size, bold=True):
    return ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf", size)


def centred(d, box, text, fnt, fill):
    x0, y0, x1, y1 = box
    l, t, r, b = d.textbbox((0, 0), text, font=fnt)
    d.text(((x0 + x1 - (r - l)) / 2 - l, (y0 + y1 - (b - t)) / 2 - t), text, font=fnt, fill=fill)


def flame(d, cx, cy, s, outer=ORANGE, inner=YELLOW):
    """Stylised flame: a teardrop pointed at the top (x = sin t * sin(t/2), y = -cos t) and a smaller inner one."""
    def drop(scale, col, lean):
        pts = []
        for k in range(0, 361, 4):
            t = math.radians(k)
            x = math.sin(t) * math.sin(t / 2) ** 1.4 * scale * 0.75
            y = -math.cos(t) * scale
            x += lean * (1 - (y / scale + 1) / 2) * scale * 0.25  # tip leans a little
            pts.append((cx + x, cy + y))
        d.polygon(pts, fill=col)
    drop(s, outer, 0.6)
    drop(s * 0.55, inner, 0.3)


def gradient(w, h, top, bottom):
    im = Image.new("RGB", (w, h), top)
    d = ImageDraw.Draw(im)
    for y in range(h):
        k = y / (h - 1)
        d.line([(0, y), (w, y)], fill=tuple(int(top[i] + (bottom[i] - top[i]) * k) for i in range(3)))
    return im


def banner(name, title, subtitle):
    W, H = 2048, 512
    im = gradient(W, H, (150, 18, 18), (60, 8, 10))
    d = ImageDraw.Draw(im)
    for i in range(0, W, 64):  # subtle diagonal stripes
        d.line([(i, H), (i + 260, 0)], fill=(170, 30, 28), width=10)
    d.rectangle([0, H - 26, W, H], fill=YELLOW)
    d.ellipse([60, 60, 452, 452], fill=WHITE)
    flame(d, 256, 280, 150, RED, ORANGE)
    centred(d, (500, 80, W - 40, 300), title, font(90), WHITE)
    centred(d, (500, 290, W - 40, 440), subtitle, font(60, False), (255, 225, 190))
    im.save(os.path.join(OUT, name))


def poster(name, title, color, draw_body):
    W, H = 768, 1024
    im = Image.new("RGB", (W, H), (247, 246, 242))
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, W, 170], fill=color)
    centred(d, (20, 10, W - 20, 160), title, font(64), WHITE)
    draw_body(d, W, H)
    d.rectangle([0, H - 18, W, H], fill=color)
    im.save(os.path.join(OUT, name))


def pass_body(d, W, H):
    steps = [("P", "Rút chốt an toàn", "tay kia giữ chặt bình"),
             ("A", "Chĩa loa vào GỐC lửa", "đứng cách 1,8 – 2,4 m"),
             ("S", "Bóp cò", "bóp hết, giữ đều tay"),
             ("S", "Quét qua lại", "ở gốc lửa đến khi tắt hẳn")]
    y = 205
    for letter, line1, line2 in steps:
        d.rounded_rectangle([40, y, 200, y + 160], radius=28, fill=RED)
        centred(d, (40, y, 200, y + 160), letter, font(110), WHITE)
        d.text((230, y + 22), line1, font=font(46), fill=DARK)
        d.text((230, y + 90), line2, font=font(36, False), fill=(90, 90, 96))
        y += 195


def call_body(d, W, H):
    centred(d, (0, 190, W, 470), "114", font(250), RED)
    items = ["1. Địa chỉ, tòa nhà", "2. Tầng, phòng", "3. Cái gì đang cháy", "4. Có người mắc kẹt?"]
    y = 500
    for it in items:
        d.rounded_rectangle([60, y, W - 60, y + 92], radius=20, fill=(255, 235, 230))
        d.text((90, y + 20), it, font=font(46), fill=DARK)
        y += 112
    centred(d, (0, H - 80, W, H - 25), "Nói chậm, rõ, không cúp máy trước", font(34, False), (90, 90, 96))


def triangle_body(d, W, H):
    cx, top, base = W / 2, 250, 760
    pts = [(cx, top), (cx - 300, base), (cx + 300, base)]
    d.polygon(pts, outline=RED, width=22)
    flame(d, cx, 600, 120)
    d.text((cx - 95, top - 70 + 0), "", font=font(40), fill=DARK)
    centred(d, (cx - 200, 180, cx + 200, 240), "NHIỆT", font(52), RED)
    centred(d, (20, base + 20, cx, base + 90), "CHẤT CHÁY", font(48), DARK)
    centred(d, (cx, base + 20, W - 20, base + 90), "OXY", font(52), (30, 90, 170))
    centred(d, (20, base + 110, W - 20, base + 220), "Bỏ 1 trong 3 yếu tố → lửa tắt", font(40, False), DARK)


def four_body(d, W, H):
    items = [("Chỉ huy", "tại chỗ"), ("Lực lượng", "tại chỗ"), ("Phương tiện", "tại chỗ"), ("Hậu cần", "tại chỗ")]
    y = 215
    for i, (a, b) in enumerate(items):
        d.ellipse([50, y + 10, 170, y + 130], fill=NAVY)
        centred(d, (50, y + 10, 170, y + 130), str(i + 1), font(70), WHITE)
        d.text((200, y + 18), a, font=font(54), fill=DARK)
        d.text((200, y + 82), b, font=font(40, False), fill=(90, 90, 96))
        y += 175
    centred(d, (20, H - 110, W - 20, H - 30), "Phương châm PCCC tại chỗ", font(36, False), NAVY)


def label(name, title, sub):
    W, H = 512, 256
    im = Image.new("RGB", (W, H), RED)
    d = ImageDraw.Draw(im)
    d.rectangle([10, 10, W - 11, H - 11], outline=WHITE, width=6)
    centred(d, (20, 20, W - 20, 160), title, font(64), WHITE)
    centred(d, (20, 150, W - 20, 230), sub, font(34, False), WHITE)
    im.save(os.path.join(OUT, name))


def whiteboard(name):
    W, H = 2048, 768
    im = Image.new("RGB", (W, H), (246, 247, 244))
    d = ImageDraw.Draw(im)
    blue, red, black = (30, 70, 160), (190, 40, 40), (40, 40, 45)
    d.text((70, 50), "Bài 3: Mạng máy tính — Mô hình TCP/IP", font=font(70), fill=blue)
    d.line([(70, 145), (1300, 145)], fill=blue, width=5)
    layers = ["Ứng dụng (HTTP, DNS)", "Giao vận (TCP, UDP)", "Mạng (IP)", "Liên kết (Ethernet, Wi-Fi)"]
    y = 190
    for i, l in enumerate(layers):
        d.rectangle([90, y, 760, y + 95], outline=black, width=5)
        d.text((115, y + 22), f"{4 - i}. {l}", font=font(44, False), fill=black)
        y += 115
    # a small network sketch
    for (x, yy) in [(1000, 260), (1260, 260), (1130, 470)]:
        d.rectangle([x - 70, yy - 45, x + 70, yy + 45], outline=black, width=5)
    d.line([(1000, 305), (1130, 425)], fill=black, width=4)
    d.line([(1260, 305), (1130, 425)], fill=black, width=4)
    d.text((1075, 520), "Router", font=font(34, False), fill=black)
    d.text((1480, 200), "Bài tập về nhà:", font=font(46), fill=red)
    d.text((1480, 270), "• Đọc chương 3", font=font(40, False), fill=black)
    d.text((1480, 330), "• Cài Wireshark", font=font(40, False), fill=black)
    d.text((1480, 600), "Thứ Tư, 18/11", font=font(40, False), fill=(110, 110, 120))
    im.save(os.path.join(OUT, name))


def rules_body(d, W, H):
    items = ["Không hút thuốc, không dùng lửa", "Không để đồ dễ cháy gần thiết bị điện",
             "Tắt máy, ngắt điện khi ra về", "Không chặn lối thoát, cửa thoát nạn",
             "Biết vị trí bình chữa cháy, nút báo cháy", "Khi cháy: báo động, gọi 114, thoát nạn"]
    y = 205
    for i, it in enumerate(items):
        d.text((50, y), f"{i + 1}.", font=font(44), fill=RED)
        d.text((115, y + 4), it, font=font(36, False), fill=DARK)
        y += 125


def clock(name):
    W = H = 512
    im = Image.new("RGB", (W, H), WHITE)
    d = ImageDraw.Draw(im)
    d.ellipse([8, 8, W - 8, H - 8], outline=DARK, width=18)
    for k in range(12):
        a = math.radians(k * 30)
        r1, r2 = 200, 225 if k % 3 else 185
        d.line([(256 + math.sin(a) * r1, 256 - math.cos(a) * r1), (256 + math.sin(a) * r2, 256 - math.cos(a) * r2)], fill=DARK, width=10 if k % 3 == 0 else 5)
    def hand(deg, length, width):
        a = math.radians(deg)
        d.line([(256, 256), (256 + math.sin(a) * length, 256 - math.cos(a) * length)], fill=DARK, width=width)
    hand(10 * 30 + 15, 120, 14)   # 10:30
    hand(180, 180, 8)
    d.ellipse([240, 240, 272, 272], fill=RED)
    im.save(os.path.join(OUT, name))


def gate_sign(name):
    W, H = 2048, 384
    im = gradient(W, H, (18, 52, 110), (10, 30, 70))
    d = ImageDraw.Draw(im)
    d.rectangle([0, H - 20, W, H], fill=YELLOW)
    centred(d, (40, 40, W - 40, 230), "TRƯỜNG ĐẠI HỌC — KHU GIẢNG ĐƯỜNG B1", font(84), WHITE)
    centred(d, (40, 220, W - 40, 340), "Điểm tập kết khi có cháy: sân trước tòa B1", font(56, False), (220, 230, 245))
    im.save(os.path.join(OUT, name))


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    banner("EF_Banner_TrainingCentre.png", "TRUNG TÂM HUẤN LUYỆN PCCC", "Học thật  ·  Làm thật  ·  An toàn thật")
    banner("EF_Banner_Debrief.png", "PHÒNG RÚT KINH NGHIỆM", "Nhìn lại · Hiểu vì sao · Làm tốt hơn lần sau")
    banner("EF_Banner_Lesson.png", "PHÒNG BÀI GIẢNG PCCC", "Hiểu luật · Thực hành tận tay · Tự kiểm tra")
    poster("EF_Poster_PASS.png", "DÙNG BÌNH: PASS", RED, pass_body)
    poster("EF_Poster_114.png", "CHÁY — GỌI NGAY", RED, call_body)
    poster("EF_Poster_Triangle.png", "TAM GIÁC CHÁY", (210, 90, 20), triangle_body)
    poster("EF_Poster_4TaiCho.png", "4 TẠI CHỖ", NAVY, four_body)
    label("EF_Label_HoseCabinet.png", "TỦ CHỮA CHÁY", "FIRE HOSE CABINET")
    whiteboard("EF_Whiteboard_Lecture.png")
    poster("EF_Poster_NoiQuy.png", "NỘI QUY PCCC", RED, rules_body)
    clock("EF_ClockFace.png")
    gate_sign("EF_Sign_Gate.png")
    print("posters ->", os.path.abspath(OUT))
