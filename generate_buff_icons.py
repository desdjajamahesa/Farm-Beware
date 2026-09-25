import math
import os
from PIL import Image, ImageDraw, ImageFilter

def create_heart_image(size=1024, with_plus=False):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    
    # Generate heart points
    scale = 26.0
    cx, cy = size // 2, int(size * 0.53)
    
    points = []
    num_pts = 360
    for i in range(num_pts):
        t = i * (2 * math.pi / num_pts)
        x = 16 * (math.sin(t) ** 3)
        y = -(13 * math.cos(t) - 5 * math.cos(2*t) - 2 * math.cos(3*t) - math.cos(4*t))
        px = cx + x * scale
        py = cy + y * scale
        points.append((px, py))
        
    # Glow layer
    glow_img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow_img)
    glow_draw.polygon(points, fill=(255, 20, 60, 220))
    glow_img = glow_img.filter(ImageFilter.GaussianBlur(40))
    img.paste(glow_img, (0, 0), glow_img)
    
    # Dark border for contrast
    draw = ImageDraw.Draw(img)
    border_pts = []
    border_scale = scale * 1.06
    for i in range(num_pts):
        t = i * (2 * math.pi / num_pts)
        x = 16 * (math.sin(t) ** 3)
        y = -(13 * math.cos(t) - 5 * math.cos(2*t) - 2 * math.cos(3*t) - math.cos(4*t))
        border_pts.append((cx + x * border_scale, cy + y * border_scale))
    draw.polygon(border_pts, fill=(20, 2, 5, 255))
    
    # Main heart fill with vertical gradient
    heart_mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(heart_mask).polygon(points, fill=255)
    
    grad = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    for y in range(size):
        ratio = y / size
        r = int(255 - ratio * 35)
        g = int(55 * (1.0 - ratio * 0.8))
        b = int(75 * (1.0 - ratio * 0.8))
        for x in range(size):
            if heart_mask.getpixel((x, y)) > 0:
                grad.putpixel((x, y), (r, g, b, 255))
                
    img = Image.alpha_composite(img, grad)
    draw = ImageDraw.Draw(img)
    
    # Specular shine on top-left curve
    shine = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    s_draw = ImageDraw.Draw(shine)
    s_draw.ellipse([cx - 280, cy - 290, cx - 70, cy - 110], fill=(255, 255, 255, 180))
    shine = shine.rotate(25, center=(cx - 175, cy - 200))
    shine = shine.filter(ImageFilter.GaussianBlur(16))
    img = Image.alpha_composite(img, shine)
    draw = ImageDraw.Draw(img)
    
    # Inner rim highlight
    inner_pts = []
    inner_scale = scale * 0.88
    for i in range(160, 260): # top left arc
        t = i * (2 * math.pi / num_pts)
        x = 16 * (math.sin(t) ** 3)
        y = -(13 * math.cos(t) - 5 * math.cos(2*t) - 2 * math.cos(3*t) - math.cos(4*t))
        inner_pts.append((cx + x * inner_scale, cy + y * inner_scale))
    if len(inner_pts) > 1:
        draw.line(inner_pts, fill=(255, 220, 230, 240), width=24, joint="curve")
        
    if with_plus:
        px, py = cx + 240, cy + 190
        # Background badge circle
        draw.ellipse([px - 150, py - 150, px + 150, py + 150], fill=(10, 25, 18, 255), outline=(40, 255, 130, 255), width=20)
        # Cross bars
        draw.rounded_rectangle([px - 105, py - 32, px + 105, py + 32], radius=16, fill=(40, 255, 130, 255), outline=(255, 255, 255, 255), width=10)
        draw.rounded_rectangle([px - 32, py - 105, px + 32, py + 105], radius=16, fill=(40, 255, 130, 255), outline=(255, 255, 255, 255), width=10)
        draw.ellipse([px - 26, py - 26, px + 26, py + 26], fill=(255, 255, 255, 255))
        
    return img.resize((256, 256), Image.Resampling.LANCZOS)

def create_lightning_image(size=1024, with_regen=False):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    
    # Sharp, thick stylized lightning bolt
    pts = [
        (610, 60),   # Top point
        (260, 520),  # Upper left bend
        (500, 520),  # Inner right notch
        (330, 960),  # Bottom needle tip
        (790, 440),  # Lower right bend
        (550, 440),  # Inner left notch
        (700, 60)    # Top right angle
    ]
    
    # Glow layer
    glow_img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow_img)
    glow_draw.polygon(pts, fill=(255, 225, 0, 220) if not with_regen else (100, 255, 80, 220))
    glow_img = glow_img.filter(ImageFilter.GaussianBlur(40))
    img.paste(glow_img, (0, 0), glow_img)
    
    # Dark border
    border_img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    b_draw = ImageDraw.Draw(border_img)
    b_draw.polygon(pts, fill=(25, 20, 5, 255), outline=(25, 20, 5, 255), width=36)
    b_draw.line(pts + [pts[0]], fill=(25, 20, 5, 255), width=40, joint="curve")
    img = Image.alpha_composite(img, border_img)
    
    # Main gradient
    mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(mask).polygon(pts, fill=255)
    
    grad = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    for y in range(size):
        ratio = y / size
        if not with_regen:
            r = int(255)
            g = int(245 - ratio * 60)
            b = int(30 * (1.0 - ratio))
        else:
            r = int(120 + (1.0 - ratio) * 130)
            g = int(255 - ratio * 20)
            b = int(40)
        for x in range(size):
            if mask.getpixel((x, y)) > 0:
                grad.putpixel((x, y), (r, g, b, 255))
                
    img = Image.alpha_composite(img, grad)
    draw = ImageDraw.Draw(img)
    
    # Center white energy highlight core
    core_pts = [
        (625, 120),
        (360, 500),
        (515, 500),
        (390, 860),
        (720, 460),
        (550, 460),
        (665, 120)
    ]
    draw.polygon(core_pts, fill=(255, 255, 240, 240))
    draw.line([(605, 90), (320, 510)], fill=(255, 255, 255, 255), width=20)
    
    if with_regen:
        px, py = 760, 750
        draw.ellipse([px - 120, py - 120, px + 120, py + 120], fill=(10, 25, 18, 255), outline=(80, 255, 130, 255), width=18)
        draw.arc([px - 80, py - 80, px + 80, py + 80], start=40, end=300, fill=(60, 255, 120, 255), width=24)
        arrow = [(px + 45, py - 70), (px + 90, py - 65), (px + 65, py - 20)]
        draw.polygon(arrow, fill=(255, 255, 255, 255))
        
    return img.resize((256, 256), Image.Resampling.LANCZOS)

def create_speed_image(size=1024):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    glow_img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow_img)
    
    trails = [
        [(70, 390), (350, 390)],
        [(110, 500), (400, 500)],
        [(50, 610), (340, 610)],
        [(140, 720), (430, 720)],
    ]
    for p1, p2 in trails:
        glow_draw.line([p1, p2], fill=(0, 220, 255, 220), width=42)
        
    # High-contrast winged boot
    boot_pts = [
        (480, 280),
        (600, 280),
        (620, 440),
        (790, 510),
        (940, 630),
        (940, 750),
        (680, 770),
        (460, 780),
        (430, 720),
        (430, 500),
        (460, 380),
    ]
    wing1 = [(460, 360), (190, 170), (370, 290)]
    wing2 = [(440, 440), (140, 280), (340, 380)]
    wing3 = [(430, 510), (170, 410), (320, 470)]
    
    glow_draw.polygon(boot_pts, fill=(0, 210, 255, 220))
    glow_draw.polygon(wing1, fill=(100, 240, 255, 220))
    glow_draw.polygon(wing2, fill=(100, 240, 255, 220))
    glow_draw.polygon(wing3, fill=(100, 240, 255, 220))
    glow_img = glow_img.filter(ImageFilter.GaussianBlur(36))
    img.paste(glow_img, (0, 0), glow_img)
    
    b_draw = ImageDraw.Draw(img)
    b_draw.polygon(boot_pts, fill=(5, 25, 40, 255), outline=(5, 25, 40, 255), width=34)
    b_draw.polygon(wing1, fill=(5, 25, 40, 255), outline=(5, 25, 40, 255), width=30)
    b_draw.polygon(wing2, fill=(5, 25, 40, 255), outline=(5, 25, 40, 255), width=30)
    b_draw.polygon(wing3, fill=(5, 25, 40, 255), outline=(5, 25, 40, 255), width=30)
    for p1, p2 in trails:
        b_draw.line([p1, p2], fill=(5, 25, 40, 255), width=46)
        
    draw = ImageDraw.Draw(img)
    for p1, p2 in trails:
        draw.line([p1, p2], fill=(0, 230, 255, 255), width=28)
        draw.line([(p1[0] + 40, p1[1]), (p2[0] - 20, p2[1])], fill=(240, 255, 255, 255), width=14)
        
    b_mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(b_mask).polygon(boot_pts, fill=255)
    b_grad = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    for y in range(size):
        ratio = y / size
        r = int(10 + ratio * 20)
        g = int(230 - ratio * 50)
        b = int(255 - ratio * 20)
        for x in range(size):
            if b_mask.getpixel((x, y)) > 0:
                b_grad.putpixel((x, y), (r, g, b, 255))
    img = Image.alpha_composite(img, b_grad)
    draw = ImageDraw.Draw(img)
    
    draw.polygon(wing1, fill=(170, 250, 255, 255), outline=(255, 255, 255, 255), width=12)
    draw.polygon(wing2, fill=(100, 235, 255, 255), outline=(240, 255, 255, 255), width=12)
    draw.polygon(wing3, fill=(40, 210, 255, 255), outline=(220, 250, 255, 255), width=12)
    
    # Sole of shoe
    draw.polygon([(460, 750), (680, 740), (940, 710), (930, 760), (670, 780), (450, 790)], fill=(255, 255, 255, 255))
    draw.line([(580, 460), (880, 640)], fill=(255, 255, 255, 240), width=20)
    
    return img.resize((256, 256), Image.Resampling.LANCZOS)

def create_attack_damage_image(size=1024):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    
    def get_sword(angle, cx, cy):
        s_img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        d = ImageDraw.Draw(s_img)
        blade = [
            (512, 100),
            (565, 210),
            (560, 660),
            (464, 660),
            (459, 210),
        ]
        # Blade dark border
        d.polygon(blade, fill=(255, 140, 20, 255), outline=(30, 10, 0, 255), width=28)
        # Blade inner
        d.polygon(blade, fill=(255, 145, 20, 255))
        # Crossguard
        d.rounded_rectangle([375, 655, 649, 715], radius=18, fill=(230, 95, 15, 255), outline=(30, 10, 0, 255), width=22)
        # Grip
        d.rounded_rectangle([485, 715, 539, 860], radius=10, fill=(80, 35, 15, 255), outline=(30, 10, 0, 255), width=18)
        # Pommel
        d.ellipse([475, 850, 549, 924], fill=(255, 170, 30, 255), outline=(30, 10, 0, 255), width=20)
        # Blade fuller
        d.line([(512, 130), (512, 645)], fill=(255, 255, 240, 255), width=18)
        # Blade edge highlight
        d.line([(512, 110), (560, 210), (555, 650)], fill=(255, 240, 160, 255), width=12)
        return s_img.rotate(angle, center=(cx, cy), resample=Image.Resampling.BICUBIC)

    sword1 = get_sword(-36, 512, 512)
    sword2 = get_sword(36, 512, 512)
    
    glow_img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    glow_img = Image.alpha_composite(glow_img, sword1)
    glow_img = Image.alpha_composite(glow_img, sword2)
    glow_img = glow_img.filter(ImageFilter.GaussianBlur(36))
    
    img = Image.alpha_composite(img, glow_img)
    img = Image.alpha_composite(img, sword1)
    img = Image.alpha_composite(img, sword2)
    
    # Clash flash
    draw = ImageDraw.Draw(img)
    cx, cy = 512, 480
    draw.ellipse([cx - 45, cy - 45, cx + 45, cy + 45], fill=(255, 255, 240, 255))
    draw.polygon([(cx, cy - 100), (cx + 22, cy - 22), (cx + 100, cy), (cx + 22, cy + 22), 
                  (cx, cy + 100), (cx - 22, cy + 22), (cx - 100, cy), (cx - 22, cy - 22)], 
                 fill=(255, 250, 200, 255))
                 
    return img.resize((256, 256), Image.Resampling.LANCZOS)

def create_attack_speed_image(size=1024):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    glow_img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    g_draw = ImageDraw.Draw(glow_img)
    g_draw.arc([140, 140, 880, 880], start=190, end=350, fill=(220, 80, 255, 220), width=56)
    g_draw.arc([120, 160, 900, 900], start=20, end=170, fill=(220, 80, 255, 220), width=56)
    glow_img = glow_img.filter(ImageFilter.GaussianBlur(36))
    img.paste(glow_img, (0, 0), glow_img)
    
    draw = ImageDraw.Draw(img)
    draw.arc([160, 160, 860, 860], start=200, end=340, fill=(240, 150, 255, 255), width=32)
    draw.arc([180, 180, 840, 840], start=215, end=320, fill=(255, 255, 255, 255), width=16)
    draw.arc([160, 180, 860, 880], start=25, end=160, fill=(240, 150, 255, 255), width=32)
    draw.arc([180, 200, 840, 860], start=40, end=145, fill=(255, 255, 255, 255), width=16)
    
    def get_dagger(angle, cx, cy):
        d_img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        d = ImageDraw.Draw(d_img)
        blade = [
            (512, 160),
            (555, 270),
            (560, 570),
            (475, 570),
            (480, 310),
        ]
        d.polygon(blade, fill=(220, 85, 255, 255), outline=(25, 5, 35, 255), width=26)
        d.polygon(blade, fill=(225, 90, 255, 255))
        d.line([(512, 170), (550, 275), (555, 565)], fill=(255, 255, 255, 255), width=18)
        d.rounded_rectangle([450, 565, 584, 610], radius=16, fill=(150, 35, 210, 255), outline=(25, 5, 35, 255), width=20)
        d.rounded_rectangle([490, 610, 544, 740], radius=10, fill=(55, 15, 90, 255), outline=(25, 5, 35, 255), width=18)
        d.ellipse([484, 735, 550, 795], fill=(180, 45, 240, 255), outline=(25, 5, 35, 255), width=18)
        return d_img.rotate(angle, center=(cx, cy), resample=Image.Resampling.BICUBIC)

    d1 = get_dagger(-40, 512, 512)
    d2 = get_dagger(40, 512, 512)
    img = Image.alpha_composite(img, d1)
    img = Image.alpha_composite(img, d2)
    
    return img.resize((256, 256), Image.Resampling.LANCZOS)

def create_frame_background(size=64):
    # 64x64 solid dark obsidian rounded slate (perfect for 9-slicing with border=20)
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    pad = 2
    radius = 14
    # Deep slate glass
    draw.rounded_rectangle([pad, pad, size - pad, size - pad], radius=radius, fill=(16, 20, 28, 235))
    return img

def create_frame_border(size=64):
    # 64x64 rounded border outline (perfect for 9-slicing with border=20)
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    pad = 2
    radius = 14
    draw.rounded_rectangle([pad, pad, size - pad, size - pad], radius=radius, outline=(255, 255, 255, 255), width=3)
    return img

def create_cooldown_overlay(size=64):
    # 64x64 solid white rounded rectangle for Radial360 sweep without any artifacts
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    pad = 2
    radius = 14
    draw.rounded_rectangle([pad, pad, size - pad, size - pad], radius=radius, fill=(255, 255, 255, 255))
    return img

def main():
    target_dirs = [
        r"F:\unity\Farm-Beware\Assets\Textures\Icons\Buffs",
        r"F:\unity\Farm-Beware\Assets\Resources\Icons\Buffs"
    ]
    for d in target_dirs:
        os.makedirs(d, exist_ok=True)
        
    icons = {
        "Icon_Buff_Heart.png": create_heart_image(with_plus=False),
        "Icon_Buff_HealthRegen.png": create_heart_image(with_plus=True),
        "Icon_Buff_Lightning.png": create_lightning_image(with_regen=False),
        "Icon_Buff_StaminaRegen.png": create_lightning_image(with_regen=True),
        "Icon_Buff_Speed.png": create_speed_image(),
        "Icon_Buff_AttackDamage.png": create_attack_damage_image(),
        "Icon_Buff_AttackSpeed.png": create_attack_speed_image(),
        "Buff_Frame_Background.png": create_frame_background(),
        "Buff_Frame_Border.png": create_frame_border(),
        "Buff_Cooldown_Overlay.png": create_cooldown_overlay(),
    }
    
    for filename, img in icons.items():
        for d in target_dirs:
            path = os.path.join(d, filename)
            img.save(path, format="PNG")
            print(f"Saved {path}")

if __name__ == "__main__":
    main()
