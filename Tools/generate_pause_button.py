"""Generate Fallout / The Fire Rises style Pause Button assets.
Generates:
  - button_pause_normal.png (64x64 HUD pause button)
  - button_pause_hover.png (64x64 energized amber glow)
  - button_pause_pressed.png (64x64 recessed rust active)
  - button_pause_disabled.png (64x64 offline/dimmed)
  - icon_pause.png (standalone 48x48 pause icon)
  - icon_play.png (standalone 48x48 play/resume icon)
Outputs to:
  - GameJame/Assets/Art/UI/Новое/Buttons/
  - GameJame/Assets/Art/UI/Новое/Placeholders/
  - GameJame/Assets/Art/UI/Новое/
  - GameJame/Docs/UI/pause_button_preview.png
"""

from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
BASE_OUT = ROOT / 'Assets' / 'Art' / 'UI' / 'Новое'
BUTTONS_DIR = BASE_OUT / 'Buttons'
ICONS_DIR = BASE_OUT / 'Placeholders'
DOCS_DIR = ROOT / 'Docs' / 'UI'

for d in (BASE_OUT, BUTTONS_DIR, ICONS_DIR, DOCS_DIR):
    d.mkdir(parents=True, exist_ok=True)

SCALE = 4

# Colors
DARK_CHASSIS    = '#0e1215'
STEEL_DARK      = '#1a2126'
STEEL_BASE      = '#273138'
STEEL_MID       = '#384650'
STEEL_LIGHT     = '#5b6d7a'
STEEL_HIGHLIGHT = '#9ab1bf'

AMBER_DARK      = '#8a4e18'
AMBER_BASE      = '#d48222'
AMBER_BRIGHT    = '#f5a623'
AMBER_HOT       = '#ffdb6b'

RUST_BASE       = '#9e4324'
RUST_HOT        = '#d96130'

def draw_rivet(draw, cx, cy, radius, scale, dark=False):
    r = radius * scale
    x, y = cx * scale, cy * scale
    draw.ellipse([x - r, y - r + scale*0.5, x + r, y + r + scale*0.5], fill='#080b0c')
    if dark:
        draw.ellipse([x - r, y - r, x + r, y + r], fill='#252e34', outline='#14191d', width=max(1, int(scale*0.4)))
    else:
        draw.ellipse([x - r, y - r, x + r, y + r], fill='#485864', outline='#182025', width=max(1, int(scale*0.4)))
        draw.ellipse([x - r*0.6, y - r*0.6, x + r*0.1, y + r*0.1], fill='#a6bcc9')
        draw.point([x - r*0.3, y - r*0.3], fill='#ffffff')

def create_pause_button(state, size=64):
    name = f"button_pause_{state}"
    sw, sh = size * SCALE, size * SCALE
    im = Image.new('RGBA', (sw, sh), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    s = SCALE
    pw, ph = size, size

    def rect(box, fill, outline=None, width=1):
        x0, y0, x1, y1 = [v * s for v in box]
        d.rectangle([x0, y0, x1, y1], fill=fill, outline=outline, width=width * s)

    def poly(pts, fill, outline=None, width=1):
        s_pts = [(x * s, y * s) for x, y in pts]
        d.polygon(s_pts, fill=fill, outline=outline)
        if outline and width > 1:
            d.line(s_pts + [s_pts[0]], fill=outline, width=width * s)

    # 1. Drop shadow
    rect([2, 3, pw - 2, ph - 1], '#06080a')

    # 2. Armored Plate with 45-degree chamfers
    ch = 6
    btn_pts = [
        (ch, 1), (pw - 1 - ch, 1),
        (pw - 1, ch), (pw - 1, ph - 1 - ch),
        (pw - 1 - ch, ph - 1), (ch, ph - 1),
        (1, ph - 1 - ch), (1, ch)
    ]

    if state == 'normal':
        poly(btn_pts, fill=STEEL_BASE, outline=DARK_CHASSIS, width=2)
        # Bevels
        d.line([(ch*s, 2*s), ((pw - 1 - ch)*s, 2*s)], fill=STEEL_LIGHT, width=2*s)
        d.line([(2*s, ch*s), (2*s, (ph - 1 - ch)*s)], fill=STEEL_MID, width=2*s)
        d.line([((pw - 2)*s, ch*s), ((pw - 2)*s, (ph - 1 - ch)*s)], fill='#14191d', width=2*s)
        d.line([(ch*s, (ph - 2)*s), ((pw - 1 - ch)*s, (ph - 2)*s)], fill='#101417', width=2*s)
        
        # Pause bars color
        bar_fill = '#e2e8f0'
        bar_edge = '#64748b'
        bar_hi = '#ffffff'

    elif state == 'hover':
        poly(btn_pts, fill='#333f48', outline=AMBER_BRIGHT, width=3)
        d.line([(ch*s, 2*s), ((pw - 1 - ch)*s, 2*s)], fill=AMBER_HOT, width=2*s)
        d.line([(2*s, ch*s), (2*s, (ph - 1 - ch)*s)], fill=AMBER_BRIGHT, width=2*s)
        d.line([((pw - 2)*s, ch*s), ((pw - 2)*s, (ph - 1 - ch)*s)], fill=AMBER_BASE, width=2*s)
        d.line([(ch*s, (ph - 2)*s), ((pw - 1 - ch)*s, (ph - 2)*s)], fill=AMBER_DARK, width=2*s)
        
        bar_fill = AMBER_HOT
        bar_edge = AMBER_BASE
        bar_hi = '#ffffff'

    elif state == 'pressed':
        poly(btn_pts, fill=RUST_BASE, outline=AMBER_BRIGHT, width=2)
        d.line([(ch*s, 2*s), ((pw - 1 - ch)*s, 2*s)], fill='#481a0e', width=3*s)
        d.line([(2*s, ch*s), (2*s, (ph - 1 - ch)*s)], fill='#481a0e', width=3*s)
        d.line([((pw - 2)*s, ch*s), ((pw - 2)*s, (ph - 1 - ch)*s)], fill=RUST_HOT, width=2*s)
        d.line([(ch*s, (ph - 2)*s), ((pw - 1 - ch)*s, (ph - 2)*s)], fill=RUST_HOT, width=2*s)
        
        bar_fill = '#ffffff'
        bar_edge = RUST_HOT
        bar_hi = '#ffffff'

    elif state == 'disabled':
        poly(btn_pts, fill='#1b2126', outline='#101417', width=2)
        d.line([(ch*s, 2*s), ((pw - 1 - ch)*s, 2*s)], fill='#2b353d', width=1*s)
        d.line([(ch*s, (ph - 2)*s), ((pw - 1 - ch)*s, (ph - 2)*s)], fill='#0d1012', width=2*s)
        
        bar_fill = '#38444c'
        bar_edge = '#22292f'
        bar_hi = '#485761'

    # 3. Corner Rivets
    dark_r = (state == 'disabled')
    draw_rivet(d, 8, 8, radius=1.6, scale=s, dark=dark_r)
    draw_rivet(d, pw - 8, 8, radius=1.6, scale=s, dark=dark_r)
    draw_rivet(d, 8, ph - 8, radius=1.6, scale=s, dark=dark_r)
    draw_rivet(d, pw - 8, ph - 8, radius=1.6, scale=s, dark=dark_r)

    # 4. Two Centered Vertical Pause Bars "||"
    bar_w = 7
    bar_h = 24
    gap = 7
    cx = pw * 0.5
    cy = ph * 0.5
    
    # Left bar
    lx0 = cx - gap*0.5 - bar_w
    lx1 = cx - gap*0.5
    ly0 = cy - bar_h*0.5
    ly1 = cy + bar_h*0.5
    rect([lx0, ly0, lx1, ly1], fill=bar_fill, outline=bar_edge, width=1)
    d.line([(lx0*s, ly0*s), (lx1*s, ly0*s)], fill=bar_hi, width=s)
    d.line([(lx0*s, ly0*s), (lx0*s, ly1*s)], fill=bar_hi, width=s)

    # Right bar
    rx0 = cx + gap*0.5
    rx1 = cx + gap*0.5 + bar_w
    rect([rx0, ly0, rx1, ly1], fill=bar_fill, outline=bar_edge, width=1)
    d.line([(rx0*s, ly0*s), (rx1*s, ly0*s)], fill=bar_hi, width=s)
    d.line([(rx0*s, ly0*s), (rx0*s, ly1*s)], fill=bar_hi, width=s)

    # Downsample
    final_im = im.resize((size, size), Image.Resampling.LANCZOS)
    final_im.save(BUTTONS_DIR / f"{name}.png", 'PNG', optimize=True)
    final_im.save(BASE_OUT / f"{name}.png", 'PNG', optimize=True)
    print(f"  [+] Saved {name}.png ({size}x{size})")
    return final_im

def create_standalone_icons():
    # 1. icon_pause.png (48x48)
    size = 48
    im = Image.new('RGBA', (size*SCALE, size*SCALE), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    s = SCALE
    
    bar_w = 6 * s
    bar_h = 26 * s
    gap = 8 * s
    cx = size * 0.5 * s
    cy = size * 0.5 * s
    
    for bx in (cx - gap*0.5 - bar_w, cx + gap*0.5):
        # Drop shadow
        d.rectangle([bx + s, cy - bar_h*0.5 + s, bx + bar_w + s, cy + bar_h*0.5 + s], fill='#0d1215')
        # Main bar
        d.rectangle([bx, cy - bar_h*0.5, bx + bar_w, cy + bar_h*0.5], fill='#ffffff', outline=STEEL_LIGHT, width=s)
        # Highlight
        d.line([(bx, cy - bar_h*0.5), (bx + bar_w, cy - bar_h*0.5)], fill='#ffffff', width=s)
        
    final_im = im.resize((size, size), Image.Resampling.LANCZOS)
    final_im.save(ICONS_DIR / 'icon_pause.png', 'PNG', optimize=True)
    final_im.save(BASE_OUT / 'icon_pause.png', 'PNG', optimize=True)
    print("  [+] Saved icon_pause.png (48x48)")

    # 2. icon_play.png (48x48)
    im_play = Image.new('RGBA', (size*SCALE, size*SCALE), (0, 0, 0, 0))
    dp = ImageDraw.Draw(im_play)
    tri_pts = [
        (14 * s, 10 * s),
        (38 * s, 24 * s),
        (14 * s, 38 * s)
    ]
    # Drop shadow
    shadow_pts = [(x + s, y + s) for x, y in tri_pts]
    dp.polygon(shadow_pts, fill='#0d1215')
    dp.polygon(tri_pts, fill='#ffffff', outline=STEEL_LIGHT)
    dp.line([tri_pts[0], tri_pts[1]], fill='#ffffff', width=s)
    
    final_play = im_play.resize((size, size), Image.Resampling.LANCZOS)
    final_play.save(ICONS_DIR / 'icon_play.png', 'PNG', optimize=True)
    final_play.save(BASE_OUT / 'icon_play.png', 'PNG', optimize=True)
    print("  [+] Saved icon_play.png (48x48)")

def create_preview_sheet():
    w, h = 640, 240
    im = Image.new('RGB', (w, h), '#0c1014')
    d = ImageDraw.Draw(im)
    
    font_path = Path('C:/Windows/Fonts/segoeuib.ttf')
    font = ImageFont.truetype(str(font_path), 16) if font_path.exists() else ImageFont.load_default()
    font_sm = ImageFont.truetype(str(font_path), 12) if font_path.exists() else ImageFont.load_default()
    
    d.rectangle([0, 0, w, 40], fill='#141b20')
    d.line([(0, 39), (w, 39)], fill='#f5a623', width=2)
    d.text((25, 10), "PAUSE BUTTON ASSETS (FALLOUT / THE FIRE RISES)", fill='#f5a623', font=font)
    
    states = [
        ('button_pause_normal', 'Normal', 35),
        ('button_pause_hover', 'Hover', 135),
        ('button_pause_pressed', 'Pressed', 235),
        ('button_pause_disabled', 'Disabled', 335)
    ]
    for name, lbl, x in states:
        btn_im = Image.open(BUTTONS_DIR / f"{name}.png")
        im.paste(btn_im, (x, 70), btn_im)
        d.text((x + 8, 145), lbl, fill='#94a3b8', font=font_sm)
        
    # Standalone icons
    d.text((455, 55), "ICONS (48x48)", fill='#cbd5e1', font=font_sm)
    ic_pause = Image.open(ICONS_DIR / 'icon_pause.png')
    ic_play = Image.open(ICONS_DIR / 'icon_play.png')
    
    im.paste(ic_pause, (460, 85), ic_pause)
    d.text((465, 145), "Pause", fill='#94a3b8', font=font_sm)
    
    im.paste(ic_play, (535, 85), ic_play)
    d.text((545, 145), "Play", fill='#94a3b8', font=font_sm)
    
    d.line([(25, 195), (w - 25, 195)], fill='#253038', width=1)
    d.text((25, 208), "Saved to Assets/Art/UI/Новое/Buttons/ and Placeholders/", fill='#64748b', font=font_sm)
    
    im.save(DOCS_DIR / 'pause_button_preview.png', 'PNG')
    print("  [+] Generated pause_button_preview.png")

def main():
    print("Generating Pause Button assets...")
    for st in ('normal', 'hover', 'pressed', 'disabled'):
        create_pause_button(st)
    create_standalone_icons()
    create_preview_sheet()
    print("All pause button assets successfully generated!")

if __name__ == '__main__':
    main()
