"""Generate advanced Fallout / The Fire Rises / Wasteland style UI sprites.
Resolution: High DPI (Scale 4x supersampling with Lanczos downsampling for crisp subpixel bevels).
Output folders:
  - GameJame/Assets/Art/UI/Новое/
  - GameJame/Assets/Art/UI/Новое/Panels/
  - GameJame/Assets/Art/UI/Новое/Buttons/
  - GameJame/Assets/Art/UI/Новое/Slots/
  - GameJame/Assets/Art/UI/Новое/Placeholders/
  - GameJame/Docs/UI/new_ui_preview.png
"""

import json
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
BASE_OUT = ROOT / 'Assets' / 'Art' / 'UI' / 'Новое'
PANELS_DIR = BASE_OUT / 'Panels'
BUTTONS_DIR = BASE_OUT / 'Buttons'
SLOTS_DIR = BASE_OUT / 'Slots'
ICONS_DIR = BASE_OUT / 'Placeholders'
DOCS_DIR = ROOT / 'Docs' / 'UI'

for d in (BASE_OUT, PANELS_DIR, BUTTONS_DIR, SLOTS_DIR, ICONS_DIR, DOCS_DIR):
    d.mkdir(parents=True, exist_ok=True)

SCALE = 4
MANIFEST = {}

# Fallout / The Fire Rises / Wasteland Color Palette
DARK_CHASSIS   = '#101417'  # Deepest shadow / background recess
STEEL_DARK     = '#1c2328'  # Dark oxidized gunmetal
STEEL_BASE     = '#273138'  # Standard armor plate face
STEEL_MID      = '#384650'  # Highlighted steel plate
STEEL_LIGHT    = '#5b6d7a'  # Light steel bevel
STEEL_HIGHLIGHT= '#9ab1bf'  # Sharp specular rim edge

AMBER_DARK     = '#8a4e18'  # Deep amber / rust shadow
AMBER_BASE     = '#d48222'  # Fallout terminal amber
AMBER_BRIGHT   = '#f5a623'  # Radiant energized amber
AMBER_HOT      = '#ffdb6b'  # Core bright glow

RUST_DARK      = '#5e2616'  # Heavy rust recess
RUST_BASE      = '#9e4324'  # Pressed button burnished rust
RUST_HOT       = '#d96130'  # Pressed button active orange

HAZARD_YELLOW  = '#e6a120'
HAZARD_DARK    = '#181b1e'

def draw_rivet(draw, cx, cy, radius, scale, dark=False):
    """Draw a 3D metallic rivet with spherical highlight and drop shadow."""
    r = radius * scale
    x, y = cx * scale, cy * scale
    # Drop shadow
    draw.ellipse([x - r, y - r + scale*0.5, x + r, y + r + scale*0.5], fill='#0a0e10')
    # Rivet body
    if dark:
        draw.ellipse([x - r, y - r, x + r, y + r], fill='#252e34', outline='#14191d', width=max(1, int(scale*0.4)))
        draw.ellipse([x - r*0.5, y - r*0.5, x, y], fill='#43515a')
    else:
        draw.ellipse([x - r, y - r, x + r, y + r], fill='#485864', outline='#182025', width=max(1, int(scale*0.4)))
        # Specular glint on top-left
        draw.ellipse([x - r*0.6, y - r*0.6, x + r*0.1, y + r*0.1], fill='#a6bcc9')
        draw.point([x - r*0.3, y - r*0.3], fill='#ffffff')

def save_sprite(name, size, draw_func, border, category_folder):
    w, h = size
    sw, sh = w * SCALE, h * SCALE
    im = Image.new('RGBA', (sw, sh), (0, 0, 0, 0))
    painter = ImageDraw.Draw(im)
    
    draw_func(painter, w, h, SCALE)
    
    # Supersampled antialiasing down to target dimensions
    final_im = im.resize((w, h), Image.Resampling.LANCZOS)
    
    # Save both in category folder and in root of 'Новое'
    path_cat = category_folder / (name + '.png')
    path_root = BASE_OUT / (name + '.png')
    final_im.save(path_cat, 'PNG', optimize=True)
    final_im.save(path_root, 'PNG', optimize=True)
    
    MANIFEST[name] = {
        'width': w,
        'height': h,
        'border': border,
        'category': category_folder.name
    }
    print(f"  [+] Generated {name}.png ({w}x{h}, border={border})")

# -------------------------------------------------------------
# 1. PANELS (Menu, Pause, Build bar)
# -------------------------------------------------------------
def draw_panel(name, size, has_hazard=False):
    w, h = size
    border = 28 # 9-slice safe border
    
    def render(d, pw, ph, s):
        # Coordinates helper
        def rect(box, fill, outline=None, width=1):
            x0, y0, x1, y1 = [v * s for v in box]
            d.rectangle([x0, y0, x1, y1], fill=fill, outline=outline, width=width * s)
            
        def poly(pts, fill, outline=None, width=1):
            s_pts = [(x * s, y * s) for x, y in pts]
            d.polygon(s_pts, fill=fill, outline=outline)
            if outline and width > 1:
                d.line(s_pts + [s_pts[0]], fill=outline, width=width * s)

        # 1. Outer Chassis Drop Shadow
        rect([2, 2, pw - 2, ph - 2], '#0a0d10')
        
        # 2. Main Armored Frame (with chamfered 45-deg corners)
        ch = 10 # Chamfer size
        frame_pts = [
            (ch, 1), (pw - 1 - ch, 1),
            (pw - 1, ch), (pw - 1, ph - 1 - ch),
            (pw - 1 - ch, ph - 1), (ch, ph - 1),
            (1, ph - 1 - ch), (1, ch)
        ]
        poly(frame_pts, fill=STEEL_DARK, outline=DARK_CHASSIS, width=2)
        
        # 3. Outer Steel Bevel (Simulates directional light from top-left)
        d.line([(ch*s, 2*s), ((pw - 1 - ch)*s, 2*s)], fill=STEEL_LIGHT, width=2*s) # Top highlight
        d.line([(2*s, ch*s), (2*s, (ph - 1 - ch)*s)], fill=STEEL_MID, width=2*s)   # Left highlight
        d.line([((pw - 2)*s, ch*s), ((pw - 2)*s, (ph - 1 - ch)*s)], fill='#12161a', width=2*s) # Right shadow
        d.line([(ch*s, (ph - 2)*s), ((pw - 1 - ch)*s, (ph - 2)*s)], fill='#0d1012', width=2*s) # Bottom shadow

        # 4. Recessed Inner Plate (9-slice safe zone inside margins 20..w-20)
        inner_m = 12
        inner_pts = [
            (inner_m + 6, inner_m), (pw - inner_m - 6, inner_m),
            (pw - inner_m, inner_m + 6), (pw - inner_m, ph - inner_m - 6),
            (pw - inner_m - 6, ph - inner_m), (inner_m + 6, ph - inner_m),
            (inner_m, ph - inner_m - 6), (inner_m, inner_m + 6)
        ]
        poly(inner_pts, fill=STEEL_BASE, outline='#151a1e', width=2)
        
        # Inner plate top-left shadow groove (recess effect)
        d.line([((inner_m+6)*s, (inner_m+1)*s), ((pw - inner_m - 6)*s, (inner_m+1)*s)], fill='#101417', width=2*s)
        d.line([((inner_m+1)*s, (inner_m+6)*s), ((inner_m+1)*s, (ph - inner_m - 6)*s)], fill='#101417', width=2*s)
        
        # 5. Top Header Tab (Hazard Stripes or Amber Tech Plate)
        header_w = 75
        header_h = 10
        hx0 = 24
        hy0 = 12
        if has_hazard:
            # Industrial Hazard Chevron Stripes
            rect([hx0, hy0, hx0 + header_w, hy0 + header_h], fill=HAZARD_DARK, outline='#382810', width=1)
            # Diagonal stripes
            for sx in range(hx0 + 4, hx0 + header_w - 4, 10):
                d.polygon([(sx*s, (hy0 + header_h)*s), ((sx + 6)*s, (hy0 + header_h)*s),
                           ((sx + 10)*s, hy0*s), ((sx + 4)*s, hy0*s)], fill=HAZARD_YELLOW)
        else:
            # Fallout Amber Indicator Bar
            rect([hx0, hy0, hx0 + header_w, hy0 + header_h], fill='#3a2610', outline='#18120a', width=1)
            rect([hx0 + 2, hy0 + 2, hx0 + header_w - 2, hy0 + header_h - 2], fill=AMBER_BASE)
            rect([hx0 + 4, hy0 + 3, hx0 + header_w - 4, hy0 + 5], fill=AMBER_HOT)

        # 6. Heavy 3D Shaded Corner Rivets (Placed strictly inside 28px corner boundary)
        rivet_dist = 18
        for rx, ry in [
            (rivet_dist, rivet_dist + 12),
            (pw - rivet_dist, rivet_dist + 12),
            (rivet_dist, ph - rivet_dist),
            (pw - rivet_dist, ph - rivet_dist)
        ]:
            draw_rivet(d, rx, ry, radius=2.2, scale=s)

        # Top-right status LED indicator (Warm green / amber)
        led_x = pw - 26
        led_y = 17
        d.ellipse([(led_x - 3)*s, (led_y - 3)*s, (led_x + 3)*s, (led_y + 3)*s], fill='#4ade80', outline='#14532d', width=s)
        d.point([led_x*s - s, led_y*s - s], fill='#ffffff')

    save_sprite(name, size, render, border, PANELS_DIR)

# -------------------------------------------------------------
# 2. BUTTONS (Normal, Hover, Pressed, Disabled)
# -------------------------------------------------------------
def draw_button(state):
    name = f"button_{state}"
    w, h = 256, 64
    border = 14 # 9-slice safe border
    
    def render(d, pw, ph, s):
        def rect(box, fill, outline=None, width=1):
            x0, y0, x1, y1 = [v * s for v in box]
            d.rectangle([x0, y0, x1, y1], fill=fill, outline=outline, width=width * s)
            
        def poly(pts, fill, outline=None, width=1):
            s_pts = [(x * s, y * s) for x, y in pts]
            d.polygon(s_pts, fill=fill, outline=outline)
            if outline and width > 1:
                d.line(s_pts + [s_pts[0]], fill=outline, width=width * s)

        # Drop shadow
        rect([2, 3, pw - 2, ph - 1], '#080b0d')

        # Chamfer geometry for military armor plate
        ch = 7
        btn_pts = [
            (ch, 1), (pw - 1 - ch, 1),
            (pw - 1, ch), (pw - 1, ph - 1 - ch),
            (pw - 1 - ch, ph - 1), (ch, ph - 1),
            (1, ph - 1 - ch), (1, ch)
        ]

        if state == 'normal':
            poly(btn_pts, fill=STEEL_BASE, outline=DARK_CHASSIS, width=2)
            # Bevel highlights
            d.line([(ch*s, 2*s), ((pw - 1 - ch)*s, 2*s)], fill=STEEL_LIGHT, width=2*s)
            d.line([(2*s, ch*s), (2*s, (ph - 1 - ch)*s)], fill=STEEL_MID, width=2*s)
            d.line([((pw - 2)*s, ch*s), ((pw - 2)*s, (ph - 1 - ch)*s)], fill='#14191d', width=2*s)
            d.line([(ch*s, (ph - 2)*s), ((pw - 1 - ch)*s, (ph - 2)*s)], fill='#101417', width=2*s)
            # Left vertical indicator tab (Amber)
            rect([5, 10, 9, ph - 11], fill=AMBER_BASE, outline='#4a2808', width=1)
            # Top subtle specular accent line
            rect([18, 6, pw - 19, 8], fill=STEEL_MID)

        elif state == 'hover':
            # Glowing energized state (Fallout Pip-Boy Amber Glow)
            poly(btn_pts, fill='#333f48', outline=AMBER_BRIGHT, width=3)
            # Bright golden rim lights
            d.line([(ch*s, 2*s), ((pw - 1 - ch)*s, 2*s)], fill=AMBER_HOT, width=2*s)
            d.line([(2*s, ch*s), (2*s, (ph - 1 - ch)*s)], fill=AMBER_BRIGHT, width=2*s)
            d.line([((pw - 2)*s, ch*s), ((pw - 2)*s, (ph - 1 - ch)*s)], fill=AMBER_BASE, width=2*s)
            d.line([(ch*s, (ph - 2)*s), ((pw - 1 - ch)*s, (ph - 2)*s)], fill=AMBER_DARK, width=2*s)
            # Radiant active indicator tab
            rect([5, 8, 10, ph - 9], fill=AMBER_HOT, outline=AMBER_BRIGHT, width=1)
            # Inner amber accent line
            rect([16, 5, pw - 17, 7], fill=AMBER_BRIGHT)

        elif state == 'pressed':
            # Recessed mechanical click into chassis (Burnished rust & hot orange)
            poly(btn_pts, fill=RUST_BASE, outline=AMBER_BRIGHT, width=2)
            # Inverted inner shadow (sunken button)
            d.line([(ch*s, 2*s), ((pw - 1 - ch)*s, 2*s)], fill='#481a0e', width=3*s)
            d.line([(2*s, ch*s), (2*s, (ph - 1 - ch)*s)], fill='#481a0e', width=3*s)
            d.line([((pw - 2)*s, ch*s), ((pw - 2)*s, (ph - 1 - ch)*s)], fill=RUST_HOT, width=2*s)
            d.line([(ch*s, (ph - 2)*s), ((pw - 1 - ch)*s, (ph - 2)*s)], fill=RUST_HOT, width=2*s)
            # Blazing active tab
            rect([5, 9, 10, ph - 10], fill=AMBER_HOT, outline='#ffffff', width=1)
            rect([16, ph - 9, pw - 17, ph - 7], fill=RUST_HOT)

        elif state == 'disabled':
            # Weathered, oxidized inactive armor plate
            poly(btn_pts, fill='#1b2126', outline='#101417', width=2)
            d.line([(ch*s, 2*s), ((pw - 1 - ch)*s, 2*s)], fill='#2b353d', width=1*s)
            d.line([(ch*s, (ph - 2)*s), ((pw - 1 - ch)*s, (ph - 2)*s)], fill='#0d1012', width=2*s)
            # Dark dimmed tab
            rect([5, 12, 8, ph - 13], fill='#2d363d')

        # Flank rivets (inside left and right margins)
        dark_rivet = (state == 'disabled')
        draw_rivet(d, 16, ph * 0.5, radius=2.0, scale=s, dark=dark_rivet)
        draw_rivet(d, pw - 17, ph * 0.5, radius=2.0, scale=s, dark=dark_rivet)

    save_sprite(name, (w, h), render, border, BUTTONS_DIR)

# -------------------------------------------------------------
# 3. BUILD SLOTS (Normal, Hover, Pressed, Disabled)
# -------------------------------------------------------------
def draw_build_slot(state):
    name = f"build_slot_{state}"
    w, h = 96, 96
    border = 16 # 9-slice safe border
    
    def render(d, pw, ph, s):
        def rect(box, fill, outline=None, width=1):
            x0, y0, x1, y1 = [v * s for v in box]
            d.rectangle([x0, y0, x1, y1], fill=fill, outline=outline, width=width * s)
            
        def poly(pts, fill, outline=None, width=1):
            s_pts = [(x * s, y * s) for x, y in pts]
            d.polygon(s_pts, fill=fill, outline=outline)
            if outline and width > 1:
                d.line(s_pts + [s_pts[0]], fill=outline, width=width * s)

        # Drop shadow
        rect([2, 2, pw - 2, ph - 2], '#080b0d')

        # Outer frame with clipped corners
        ch = 8
        frame_pts = [
            (ch, 1), (pw - 1 - ch, 1),
            (pw - 1, ch), (pw - 1, ph - 1 - ch),
            (pw - 1 - ch, ph - 1), (ch, ph - 1),
            (1, ph - 1 - ch), (1, ch)
        ]

        if state == 'normal':
            poly(frame_pts, fill=STEEL_BASE, outline=DARK_CHASSIS, width=2)
            # Recessed center for building icon
            rect([12, 12, pw - 13, ph - 13], fill='#171d22', outline='#101417', width=1)
            # Top-left bevel highlight
            d.line([(ch*s, 2*s), ((pw - 1 - ch)*s, 2*s)], fill=STEEL_LIGHT, width=2*s)
            d.line([(2*s, ch*s), (2*s, (ph - 1 - ch)*s)], fill=STEEL_MID, width=2*s)

        elif state == 'hover':
            poly(frame_pts, fill='#303c44', outline=AMBER_BRIGHT, width=3)
            rect([12, 12, pw - 13, ph - 13], fill='#1e262c', outline=AMBER_BASE, width=1)
            d.line([(ch*s, 2*s), ((pw - 1 - ch)*s, 2*s)], fill=AMBER_HOT, width=2*s)
            d.line([(2*s, ch*s), (2*s, (ph - 1 - ch)*s)], fill=AMBER_BRIGHT, width=2*s)

        elif state == 'pressed':
            poly(frame_pts, fill=RUST_BASE, outline=AMBER_BRIGHT, width=2)
            rect([12, 12, pw - 13, ph - 13], fill='#3b190f', outline=RUST_HOT, width=2)

        elif state == 'disabled':
            poly(frame_pts, fill='#1b2126', outline='#101417', width=2)
            rect([12, 12, pw - 13, ph - 13], fill='#121619')

        # 4 Corner Grip Brackets (Military socket clamps)
        clamp_len = 10
        clamp_w = 3
        # Top-Left
        rect([5, 5, 5 + clamp_len, 5 + clamp_w], fill=AMBER_BASE if state == 'hover' else STEEL_LIGHT)
        rect([5, 5, 5 + clamp_w, 5 + clamp_len], fill=AMBER_BASE if state == 'hover' else STEEL_LIGHT)
        # Top-Right
        rect([pw - 6 - clamp_len, 5, pw - 6, 5 + clamp_w], fill=AMBER_BASE if state == 'hover' else STEEL_LIGHT)
        rect([pw - 6 - clamp_w, 5, pw - 6, 5 + clamp_len], fill=AMBER_BASE if state == 'hover' else STEEL_LIGHT)
        # Bottom-Left
        rect([5, ph - 6 - clamp_w, 5 + clamp_len, ph - 6], fill=AMBER_BASE if state == 'hover' else STEEL_LIGHT)
        rect([5, ph - 6 - clamp_len, 5 + clamp_w, ph - 6], fill=AMBER_BASE if state == 'hover' else STEEL_LIGHT)
        # Bottom-Right
        rect([pw - 6 - clamp_len, ph - 6 - clamp_w, pw - 6, ph - 6], fill=AMBER_BASE if state == 'hover' else STEEL_LIGHT)
        rect([pw - 6 - clamp_w, ph - 6 - clamp_len, pw - 6, ph - 6], fill=AMBER_BASE if state == 'hover' else STEEL_LIGHT)

    save_sprite(name, (w, h), render, border, SLOTS_DIR)

# -------------------------------------------------------------
# 4. PLACEHOLDERS (Building square & Pause overlay)
# -------------------------------------------------------------
def draw_placeholders():
    # 1. building_placeholder.png (64x64): Stylized white isometric building / blueprint cube
    def render_bld(d, pw, ph, s):
        # Crisp pure white isometric building icon with subtle blueprint line
        top = [
            (pw * 0.5 * s, 8 * s),
            ((pw - 10) * s, 24 * s),
            (pw * 0.5 * s, 40 * s),
            (10 * s, 24 * s)
        ]
        left_face = [
            (10 * s, 24 * s),
            (pw * 0.5 * s, 40 * s),
            (pw * 0.5 * s, 56 * s),
            (10 * s, 40 * s)
        ]
        right_face = [
            (pw * 0.5 * s, 40 * s),
            ((pw - 10) * s, 24 * s),
            ((pw - 10) * s, 40 * s),
            (pw * 0.5 * s, 56 * s)
        ]
        d.polygon(top, fill='#ffffff')
        d.polygon(left_face, fill='#e2e8f0')
        d.polygon(right_face, fill='#cbd5e1')
        d.line(top + [top[0]], fill='#ffffff', width=2*s)
        d.line(left_face, fill='#ffffff', width=2*s)
        d.line(right_face, fill='#ffffff', width=2*s)

    save_sprite('building_placeholder', (64, 64), render_bld, 0, ICONS_DIR)

    # 2. overlay_pause.png (32x32): Post-apocalyptic atmospheric dark vignette overlay
    def render_overlay(d, pw, ph, s):
        # Semi-transparent dark slate-charcoal tint
        d.rectangle([0, 0, pw * s, ph * s], fill=(10, 15, 20, 195))

    save_sprite('overlay_pause', (32, 32), render_overlay, 0, ICONS_DIR)

# -------------------------------------------------------------
# 5. PREVIEW SHEET GENERATOR
# -------------------------------------------------------------
def generate_preview():
    pw, ph = 1440, 920
    im = Image.new('RGB', (pw, ph), '#0c1014')
    d = ImageDraw.Draw(im)
    
    font_path = Path('C:/Windows/Fonts/segoeui.ttf')
    bold_path = Path('C:/Windows/Fonts/segoeuib.ttf')
    def font(sz, bold=False):
        p = bold_path if bold and bold_path.exists() else font_path
        return ImageFont.truetype(str(p), sz) if p.exists() else ImageFont.load_default(size=sz)

    # Title Banner
    d.rectangle([0, 0, pw, 70], fill='#141b20')
    d.line([(0, 69), (pw, 69)], fill='#f5a623', width=2)
    d.text((45, 18), "WASTELAND RTS - NEW UI ASSETS (FALLOUT / THE FIRE RISES STYLE)", fill='#f5a623', font=font(24, True))
    d.text((pw - 320, 24), "Assets/Art/UI/Новое/", fill='#94a3b8', font=font(18))

    # Paste panels with 9-slice preview
    def paste_asset(name, x, y, w, h):
        cat = MANIFEST[name]['category']
        src_path = BASE_OUT / cat / (name + '.png')
        if not src_path.exists():
            src_path = BASE_OUT / (name + '.png')
        src = Image.open(src_path).convert('RGBA')
        
        b = MANIFEST[name]['border']
        if b > 0 and (src.width != w or src.height != h):
            # 9-slice resize
            target = Image.new('RGBA', (w, h), (0, 0, 0, 0))
            sw, sh = src.size
            # 4 Corners
            target.paste(src.crop((0, 0, b, b)), (0, 0))
            target.paste(src.crop((sw - b, 0, sw, b)), (w - b, 0))
            target.paste(src.crop((0, sh - b, b, sh)), (0, h - b))
            target.paste(src.crop((sw - b, sh - b, sw, sh)), (w - b, h - b))
            # 4 Edges
            target.paste(src.crop((b, 0, sw - b, b)).resize((w - 2*b, b)), (b, 0))
            target.paste(src.crop((b, sh - b, sw - b, sh)).resize((w - 2*b, b)), (b, h - b))
            target.paste(src.crop((0, b, b, sh - b)).resize((b, h - 2*b)), (0, b))
            target.paste(src.crop((sw - b, b, sw, sh - b)).resize((b, h - 2*b)), (w - b, b))
            # Center
            target.paste(src.crop((b, b, sw - b, sh - b)).resize((w - 2*b, h - 2*b)), (b, b))
            im.paste(target, (x, y), target)
        else:
            scaled = src.resize((w, h), Image.Resampling.LANCZOS)
            im.paste(scaled, (x, y), scaled)

    # Column 1: Panels
    d.text((45, 95), "PANELS (9-Slice)", fill='#cbd5e1', font=font(18, True))
    paste_asset('panel_menu', 45, 130, 280, 360)
    d.text((65, 150), "MAIN MENU", fill='#f5a623', font=font(18, True))
    d.text((65, 460), "panel_menu.png", fill='#64748b', font=font(14))

    paste_asset('panel_pause', 360, 130, 260, 360)
    d.text((380, 150), "PAUSE", fill='#f5a623', font=font(18, True))
    d.text((380, 460), "panel_pause.png", fill='#64748b', font=font(14))

    # Bottom: Build Panel
    d.text((45, 520), "BUILD BAR (9-Slice)", fill='#cbd5e1', font=font(18, True))
    paste_asset('panel_build', 45, 555, 575, 140)
    d.text((65, 575), "CONSTRUCTION DOCK", fill='#f5a623', font=font(16, True))
    # Place building slots inside build bar preview
    for idx, state in enumerate(['normal', 'hover', 'pressed', 'disabled']):
        paste_asset(f'build_slot_{state}', 245 + idx * 80, 580, 72, 72)
        if state != 'disabled':
            paste_asset('building_placeholder', 257 + idx * 80, 592, 48, 48)

    # Column 2: Buttons
    d.text((680, 95), "BUTTON STATES", fill='#cbd5e1', font=font(18, True))
    states = [
        ('button_normal', 'NORMAL STATE', 130, '#cbd5e1'),
        ('button_hover', 'HOVER / ENERGIZED', 215, '#ffe08a'),
        ('button_pressed', 'PRESSED / ACTIVE', 300, '#ffbe8a'),
        ('button_disabled', 'DISABLED / OFFLINE', 385, '#64748b')
    ]
    for b_name, label, by, txt_col in states:
        paste_asset(b_name, 680, by, 320, 68)
        d.text((710, by + 22), label, fill=txt_col, font=font(18, True))
        d.text((1020, by + 26), f"{b_name}.png", fill='#64748b', font=font(14))

    # Column 3: Build Slots Details
    d.text((680, 485), "BUILD SLOTS (Standalone 96x96)", fill='#cbd5e1', font=font(18, True))
    slot_states = [
        ('build_slot_normal', 'Normal', 680),
        ('build_slot_hover', 'Hover', 795),
        ('build_slot_pressed', 'Pressed', 910),
        ('build_slot_disabled', 'Locked', 1025)
    ]
    for s_name, s_lbl, sx in slot_states:
        paste_asset(s_name, sx, 525, 96, 96)
        d.text((sx + 15, 630), s_lbl, fill='#94a3b8', font=font(14))
        if s_lbl != 'Locked':
            paste_asset('building_placeholder', sx + 18, 543, 60, 60)

    # Footer Info
    d.line([(45, 830), (pw - 45, 830)], fill='#253038', width=1)
    d.text((45, 850), "All sprites are configured for Unity 9-slice borders. Resolution scale x4 with subpixel Lanczos filtering.", fill='#64748b', font=font(15))
    d.text((pw - 340, 850), "Format: RGBA PNG with alpha", fill='#64748b', font=font(15))

    out_prev = DOCS_DIR / 'new_ui_preview.png'
    im.save(out_prev, 'PNG', optimize=True)
    print(f"  [+] Saved preview to {out_prev}")

def main():
    print("Generating Fallout / The Fire Rises style UI sprites...")
    # 1. Panels
    draw_panel('panel_menu', (256, 256), has_hazard=True)
    draw_panel('panel_pause', (256, 256), has_hazard=False)
    draw_panel('panel_build', (320, 128), has_hazard=True)
    
    # 2. Buttons
    for st in ('normal', 'hover', 'pressed', 'disabled'):
        draw_button(st)
        
    # 3. Build Slots
    for st in ('normal', 'hover', 'pressed', 'disabled'):
        draw_build_slot(st)
        
    # 4. Placeholders & Overlays
    draw_placeholders()
    
    # 5. Manifest JSON
    manifest_path = BASE_OUT / 'sprites.json'
    manifest_path.write_text(json.dumps(MANIFEST, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    print(f"  [+] Wrote manifest to {manifest_path}")
    
    # 6. Preview sheet
    generate_preview()
    print("All new UI assets successfully generated!")

if __name__ == '__main__':
    main()
