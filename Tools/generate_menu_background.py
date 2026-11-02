"""Cinematic Main Menu background generator for the post-apocalyptic RTS.
Produces a crisp, museum-grade 1920x1080 full HD digital artwork:
- Mad Max / Fallout post-apocalyptic aesthetic in stylized low-poly / graphic style.
- Breathtaking wasteland sunset with god rays, glowing sun, distant ruins and mesas.
- Fortified scrap outpost on a dune ridge: detailed lattice watchtower, searchlight beam,
  stacked shipping containers, radio mast with guy wires, windmill, parked buggy, oil barrels.
- Left-side compositional negative space with cinematic darkening tailored for UI menus.
"""

import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
OUT_ASSETS = ROOT / 'Assets' / 'Art' / 'UI' / 'Generated'
OUT_DOCS = ROOT / 'Docs' / 'UI'

OUT_ASSETS.mkdir(parents=True, exist_ok=True)
OUT_DOCS.mkdir(parents=True, exist_ok=True)

# Render at 2560x1440 and supersample down to 1920x1080 for pristine anti-aliasing
W, H = 2560, 1440
TARGET_W, TARGET_H = 1920, 1080

def create_sky_gradient(w, h):
    """Rich cinematic post-apocalyptic sunset sky."""
    stops = [
        (0.00, np.array([16, 22, 38], dtype=np.float64)),   # Deep twilight slate-navy
        (0.20, np.array([32, 36, 56], dtype=np.float64)),   # Dusty twilight purple
        (0.38, np.array([76, 42, 60], dtype=np.float64)),   # Warm dusk mauve
        (0.52, np.array([178, 62, 40], dtype=np.float64)),  # Rich burnt terracotta
        (0.66, np.array([230, 98, 36], dtype=np.float64)),  # Radiant fiery amber
        (0.78, np.array([252, 165, 65], dtype=np.float64)), # Golden solar orange
        (0.90, np.array([255, 212, 118], dtype=np.float64)),# Pale desert horizon gold
        (1.00, np.array([245, 185, 135], dtype=np.float64)),# Atmospheric dust horizon
    ]
    
    arr = np.zeros((h, w, 3), dtype=np.uint8)
    for y in range(h):
        t = y / float(h - 1)
        for k in range(len(stops) - 1):
            if stops[k][0] <= t <= stops[k+1][0]:
                local_t = (t - stops[k][0]) / (stops[k+1][0] - stops[k][0])
                ct = (1.0 - math.cos(local_t * math.pi)) * 0.5
                c = (1.0 - ct) * stops[k][1] + ct * stops[k+1][1]
                arr[y, :] = np.clip(c, 0, 255).astype(np.uint8)
                break
    return Image.fromarray(arr, mode='RGB')

def draw_sun_and_rays(img, sun_pos, sun_r=75):
    """Draw radiant sun disc with soft Gaussian-blurred atmospheric corona and crepuscular god rays."""
    sx, sy = sun_pos
    glow_layer = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    d_glow = ImageDraw.Draw(glow_layer)
    
    # 1. Broad atmospheric bloom
    for r, col in [
        (850, (255, 140, 40, 25)),
        (620, (255, 165, 55, 40)),
        (440, (255, 195, 80, 60)),
        (280, (255, 220, 115, 90)),
        (160, (255, 240, 165, 130)),
    ]:
        d_glow.ellipse([sx - r, sy - r, sx + r, sy + r], fill=col)
        
    # 2. Crepuscular god-rays
    num_rays = 22
    np.random.seed(55)
    for i in range(num_rays):
        base_angle = (i / float(num_rays)) * 2.0 * math.pi
        angle = base_angle + np.random.uniform(-0.04, 0.04)
        width_rad = np.random.uniform(0.035, 0.075)
        ray_len = np.random.uniform(1100, 2000)
        
        p1 = (sx + math.cos(angle - width_rad) * ray_len, sy + math.sin(angle - width_rad) * ray_len)
        p2 = (sx + math.cos(angle + width_rad) * ray_len, sy + math.sin(angle + width_rad) * ray_len)
        
        # Rays only fan upward and across the sky
        if math.sin(angle) < 0.2:
            alpha = int(np.random.uniform(22, 45))
            d_glow.polygon([(sx, sy), p1, p2], fill=(255, 230, 160, alpha))
            
    # Soft blur for atmospheric realism
    glow_blurred = glow_layer.filter(ImageFilter.GaussianBlur(32))
    img.paste(Image.alpha_composite(img.convert('RGBA'), glow_blurred).convert('RGB'), (0, 0))

    # Crisp blinding sun core
    d_core = ImageDraw.Draw(img, 'RGBA')
    d_core.ellipse([sx - sun_r, sy - sun_r, sx + sun_r, sy + sun_r], fill=(255, 248, 225, 255))
    d_core.ellipse([sx - int(sun_r*0.65), sy - int(sun_r*0.65), sx + int(sun_r*0.65), sy + int(sun_r*0.65)],
                   fill=(255, 255, 255, 255))

def draw_distant_mountains(img):
    """Layered atmospheric mountain ranges and desert ruins silhouettes."""
    d = ImageDraw.Draw(img, 'RGBA')
    
    # Range 1: Far distant jagged peaks / canyon mesas (pale misty violet-umber)
    np.random.seed(1337)
    pts1 = [(0, H)]
    y_base1 = H * 0.44
    for x in range(0, W + 40, 20):
        noise = (math.sin(x * 0.003) * 50 +
                 math.sin(x * 0.008) * 30 +
                 math.cos(x * 0.016) * 14)
        if (x // 240) % 2 == 0 and abs(math.sin(x * 0.004)) > 0.35:
            noise -= 22
        pts1.append((x, int(y_base1 + noise)))
    pts1.append((W, H))
    d.polygon(pts1, fill=(105, 52, 64, 210))
    
    # Range 2: Mid-distance sandstone plateaus & eroded spires
    pts2 = [(0, H)]
    y_base2 = H * 0.49
    for x in range(0, W + 30, 15):
        noise = (math.sin(x * 0.004 + 1.2) * 42 +
                 math.cos(x * 0.011) * 26 +
                 math.sin(x * 0.024) * 12)
        pts2.append((x, int(y_base2 + noise)))
    pts2.append((W, H))
    d.polygon(pts2, fill=(142, 68, 52, 235))

    # Distant silhouettes of old-world transmission pylons
    def draw_pylon(px, py, ph, pw):
        legs = [(px - pw*0.5, py), (px - pw*0.2, py - ph), (px + pw*0.2, py - ph), (px + pw*0.5, py)]
        d.line([legs[0], legs[1]], fill=(120, 58, 48, 190), width=2)
        d.line([legs[3], legs[2]], fill=(120, 58, 48, 190), width=2)
        for frac in (0.35, 0.65, 0.95):
            cy = py - ph * frac
            cw = pw * (1.2 if frac > 0.5 else 0.8)
            d.line([(px - cw*0.5, cy), (px + cw*0.5, cy)], fill=(120, 58, 48, 190), width=2)
        d.line([legs[0], legs[2]], fill=(120, 58, 48, 140), width=1)
        d.line([legs[3], legs[1]], fill=(120, 58, 48, 140), width=1)

    draw_pylon(W * 0.78, H * 0.48, 95, 26)
    draw_pylon(W * 0.86, H * 0.50, 75, 22)
    draw_pylon(W * 0.93, H * 0.52, 60, 18)

def draw_desert_dunes(img):
    """Smooth sweeping organic desert dunes with sharp golden rim-lit crests."""
    d = ImageDraw.Draw(img, 'RGBA')
    
    # ---------------------------------------------------------
    # DUNE 1 (Far background dune: spans full width around y=0.55*H)
    # ---------------------------------------------------------
    xs1 = np.linspace(0, W, 80)
    pts_dune1 = [(0, H)]
    for x in xs1:
        y = H * 0.54 + math.sin(x * 0.0022 + 0.5) * 55 + math.cos(x * 0.005) * 25
        pts_dune1.append((x, y))
    pts_dune1.append((W, H))
    
    # Fill dune 1 with rich sunset terra-cotta
    d.polygon(pts_dune1, fill=(162, 74, 52, 255))
    # Golden edge rim line
    for i in range(1, len(pts_dune1) - 2):
        d.line([pts_dune1[i], pts_dune1[i+1]], fill=(245, 175, 105, 220), width=3)

    # ---------------------------------------------------------
    # DUNE 2 (Mid-distance Outpost Ridge: rises to the right base plateau)
    # ---------------------------------------------------------
    xs2 = np.linspace(W * 0.15, W + 40, 90)
    pts_dune2 = [(W * 0.15, H)]
    for x in xs2:
        t = (x - W * 0.15) / (W * 0.85)
        # Smooth ridge: rises up to anchor the base around x = 0.65*W, y = 0.58*H
        y_ridge = H * 0.74 - math.sin(t * math.pi * 0.82) * (H * 0.17) + math.sin(x * 0.004) * 15
        pts_dune2.append((x, y_ridge))
    pts_dune2.append((W + 40, H))
    
    # Fill with warm golden desert sand
    d.polygon(pts_dune2, fill=(195, 96, 48, 255))
    
    # Crisp golden crest highlight along the top edge
    for i in range(1, len(pts_dune2) - 2):
        d.line([pts_dune2[i], pts_dune2[i+1]], fill=(255, 222, 138, 255), width=4)

    # ---------------------------------------------------------
    # DUNE 3 (Left background dune: creates depth in negative space)
    # ---------------------------------------------------------
    xs3 = np.linspace(-40, W * 0.62, 70)
    pts_dune3 = [(-40, H)]
    for x in xs3:
        t = (x + 40) / (W * 0.66)
        y = H * 0.64 + math.sin(t * math.pi) * (H * 0.12) + math.cos(x * 0.006) * 16
        pts_dune3.append((x, y))
    pts_dune3.append((W * 0.62, H))
    d.polygon(pts_dune3, fill=(145, 66, 46, 255))
    for i in range(1, len(pts_dune3) - 2):
        d.line([pts_dune3[i], pts_dune3[i+1]], fill=(225, 155, 95, 200), width=3)

    # ---------------------------------------------------------
    # DUNE 4 (Grand Foreground Dune: sweeping smoothly across the whole lower frame)
    # ---------------------------------------------------------
    xs4 = np.linspace(-60, W + 60, 90)
    pts_dune4 = [(-60, H)]
    for x in xs4:
        t = (x + 60) / (W + 120)
        y = H * 0.86 - math.sin(t * math.pi * 0.92) * (H * 0.15) + math.sin(x * 0.004) * 16
        pts_dune4.append((x, y))
    pts_dune4.append((W + 60, H))
    d.polygon(pts_dune4, fill=(118, 52, 38, 255))
    for i in range(1, len(pts_dune4) - 2):
        d.line([pts_dune4[i], pts_dune4[i+1]], fill=(240, 168, 102, 230), width=4)

def draw_outpost_base(img, sun_pos):
    """Draw the detailed fortified survivor base with scrap structures, watchtower, and lights."""
    d = ImageDraw.Draw(img, 'RGBA')
    
    # Base location anchor: dune ridge on the right side
    bx = W * 0.62
    by = H * 0.57
    
    # Base foundation mound / packed earth rampart (smooth blend)
    d.polygon([(bx - 260, by + 160), (bx + 380, by + 175),
               (bx + 340, by + 80), (bx - 230, by + 60)],
              fill=(165, 80, 44, 255))
    # Shadow under buildings
    d.ellipse([bx - 220, by + 85, bx + 280, by + 165], fill=(32, 16, 14, 210))

    # ----------------------------------------------------
    # 1. SHIPPING CONTAINERS & COMMAND WORKSHOP
    # ----------------------------------------------------
    # Bottom container (Weathered Rust Red)
    c1_w, c1_h = 320, 115
    c1_x, c1_y = bx - 140, by + 35
    
    d.rectangle([c1_x, c1_y, c1_x + c1_w, c1_y + c1_h], fill=(168, 54, 38, 255), outline=(95, 30, 22, 255), width=3)
    # Vertical corrugation ribs
    for rx in range(int(c1_x + 18), int(c1_x + c1_w - 15), 18):
        d.line([(rx, c1_y + 4), (rx, c1_y + c1_h - 4)], fill=(132, 40, 28, 255), width=4)
        d.line([(rx + 4, c1_y + 4), (rx + 4, c1_y + c1_h - 4)], fill=(195, 75, 55, 255), width=2)
    # Top rim highlight
    d.line([(c1_x, c1_y), (c1_x + c1_w, c1_y)], fill=(235, 120, 85, 255), width=3)
    d.text((c1_x + 35, c1_y + 35), "SECTOR-4", fill=(225, 190, 150, 180))

    # Upper stacked container (Military Olive / Desert Teal)
    c2_w, c2_h = 290, 105
    c2_x, c2_y = bx - 90, by - 75
    d.rectangle([c2_x, c2_y, c2_x + c2_w, c2_y + c2_h], fill=(48, 92, 85, 255), outline=(28, 55, 50, 255), width=3)
    for rx in range(int(c2_x + 16), int(c2_x + c2_w - 14), 18):
        d.line([(rx, c2_y + 4), (rx, c2_y + c2_h - 4)], fill=(36, 72, 66, 255), width=4)
        d.line([(rx + 4, c2_y + 4), (rx + 4, c2_y + c2_h - 4)], fill=(75, 125, 118, 255), width=2)
    d.line([(c2_x, c2_y), (c2_x + c2_w, c2_y)], fill=(120, 175, 165, 255), width=3)
    # Hazard stripes on container corner
    for hx in range(int(c2_x + c2_w - 65), int(c2_x + c2_w - 10), 12):
        d.polygon([(hx, c2_y + 8), (hx + 8, c2_y + 8), (hx - 4, c2_y + 38), (hx - 12, c2_y + 38)],
                  fill=(240, 175, 45, 230))

    # Workshop Corrugated Awning / Porch
    aw_pts = [(c1_x - 45, c1_y + 20), (c1_x + 120, c1_y - 10),
              (c1_x + 105, c1_y + 15), (c1_x - 55, c1_y + 45)]
    d.polygon(aw_pts, fill=(185, 135, 58, 255), outline=(100, 70, 30, 255))
    d.line([(c1_x - 45, c1_y + 35), (c1_x - 45, c1_y + c1_h + 10)], fill=(75, 55, 40, 255), width=5)
    d.line([(c1_x + 25, c1_y + 20), (c1_x + 25, c1_y + c1_h + 10)], fill=(75, 55, 40, 255), width=5)

    # Industrial Smokestack & Drifting Low-Poly Smoke Puffs
    st_x, st_y = c2_x + c2_w - 45, c2_y - 95
    d.rectangle([st_x, st_y, st_x + 24, c2_y], fill=(70, 74, 80, 255), outline=(45, 48, 52, 255), width=2)
    d.line([(st_x + 12, st_y), (st_x + 12, c2_y)], fill=(110, 115, 120, 255), width=3)
    
    smoke_specs = [
        (st_x + 12, st_y - 20, 24, (215, 175, 145, 180)),
        (st_x + 35, st_y - 48, 38, (200, 160, 135, 160)),
        (st_x + 65, st_y - 82, 54, (185, 145, 125, 130)),
        (st_x + 105, st_y - 120, 72, (170, 130, 115, 100)),
        (st_x + 155, st_y - 160, 95, (155, 120, 110, 70)),
    ]
    for smx, smy, smr, smcol in smoke_specs:
        pts_sm = []
        for a_idx in range(7):
            ang = a_idx * (2.0 * math.pi / 7.0)
            r_var = smr * (0.85 + (a_idx % 2)*0.3)
            pts_sm.append((smx + math.cos(ang) * r_var, smy + math.sin(ang) * r_var))
        d.polygon(pts_sm, fill=smcol)

    # ----------------------------------------------------
    # 2. THE WATCHTOWER / SNIPER TOWER (Dominant Landmark)
    # ----------------------------------------------------
    tx = bx + 220
    ty_base = by + 135
    tw_h = 440
    tw_base_w = 110
    tw_top_w = 75
    ty_top = ty_base - tw_h
    
    # 4 Steel lattice legs
    l1 = [(tx - tw_base_w*0.5, ty_base), (tx - tw_top_w*0.5, ty_top)]
    l2 = [(tx + tw_base_w*0.5, ty_base), (tx + tw_top_w*0.5, ty_top)]
    d.line(l1, fill=(65, 70, 78, 255), width=7)
    d.line(l2, fill=(65, 70, 78, 255), width=7)
    d.line([(l1[0][0] + 2, l1[0][1]), (l1[1][0] + 2, l1[1][1])], fill=(125, 132, 145, 255), width=3)
    d.line([(l2[0][0] - 2, l2[0][1]), (l2[1][0] - 2, l2[1][1])], fill=(155, 162, 175, 255), width=3)
    
    # Tower platforms & diagonal cross struts
    num_tiers = 5
    for k in range(num_tiers):
        t_frac0 = k / float(num_tiers)
        t_frac1 = (k + 1) / float(num_tiers)
        y0 = ty_base - tw_h * t_frac0
        y1 = ty_base - tw_h * t_frac1
        w0 = tw_base_w * (1.0 - t_frac0) + tw_top_w * t_frac0
        w1 = tw_base_w * (1.0 - t_frac1) + tw_top_w * t_frac1
        d.line([(tx - w0*0.5, y0), (tx + w0*0.5, y0)], fill=(75, 80, 88, 255), width=4)
        d.line([(tx - w0*0.5, y0), (tx + w1*0.5, y1)], fill=(55, 60, 68, 220), width=3)
        d.line([(tx + w0*0.5, y0), (tx - w1*0.5, y1)], fill=(55, 60, 68, 220), width=3)

    # Observation Deck Platform
    plat_w = 145
    plat_h = 16
    d.rectangle([tx - plat_w*0.5, ty_top - plat_h, tx + plat_w*0.5, ty_top],
                fill=(95, 75, 55, 255), outline=(50, 40, 30, 255), width=2)
    # Guard Rails
    rail_h = 32
    d.rectangle([tx - plat_w*0.5 + 4, ty_top - plat_h - rail_h, tx + plat_w*0.5 - 4, ty_top - plat_h],
                outline=(80, 85, 92, 255), width=3)
    for rx in range(int(tx - plat_w*0.5 + 16), int(tx + plat_w*0.5 - 10), 18):
        d.line([(rx, ty_top - plat_h - rail_h), (rx, ty_top - plat_h)], fill=(75, 80, 88, 255), width=2)

    # Guard Cabin / Lookout Hut
    cab_w = 95
    cab_h = 65
    cab_y = ty_top - plat_h - cab_h
    d.rectangle([tx - cab_w*0.5, cab_y, tx + cab_w*0.5, ty_top - plat_h],
                fill=(145, 65, 45, 255), outline=(80, 35, 25, 255), width=2)
    # Glowing window slit (warm amber guard light inside)
    d.rectangle([tx - 25, cab_y + 18, tx + 25, cab_y + 36], fill=(255, 195, 85, 255))
    d.rectangle([tx - 20, cab_y + 22, tx + 20, cab_y + 32], fill=(255, 245, 180, 255))

    # Tin Overhang Roof (Slight tilt)
    roof_w = 160
    roof_y = cab_y - 12
    d.polygon([(tx - roof_w*0.5 - 10, roof_y + 8), (tx + roof_w*0.5 + 10, roof_y - 6),
               (tx + roof_w*0.5 + 8, roof_y), (tx - roof_w*0.5 - 8, roof_y + 14)],
              fill=(180, 115, 55, 255), outline=(95, 60, 28, 255))

    # Radio Mast & Antenna Array on Tower Roof
    mast_h = 135
    d.line([(tx, roof_y - 2), (tx, roof_y - mast_h)], fill=(150, 155, 162, 255), width=4)
    d.line([(tx - 28, roof_y - mast_h + 35), (tx + 28, roof_y - mast_h + 35)], fill=(150, 155, 162, 255), width=3)
    d.line([(tx - 18, roof_y - mast_h + 65), (tx + 18, roof_y - mast_h + 65)], fill=(150, 155, 162, 255), width=3)
    # Guy-wire cables spanning down to the dunes
    d.line([(tx, roof_y - mast_h + 20), (tx - 160, ty_base - 80)], fill=(90, 95, 102, 140), width=1)
    d.line([(tx, roof_y - mast_h + 20), (tx + 190, ty_base - 60)], fill=(90, 95, 102, 140), width=1)
    # Top Aircraft Warning Beacon (Glowing Red/Orange)
    beacon_pos = (tx, roof_y - mast_h - 4)
    d.ellipse([beacon_pos[0] - 6, beacon_pos[1] - 6, beacon_pos[0] + 6, beacon_pos[1] + 6], fill=(255, 75, 45, 255))
    d.ellipse([beacon_pos[0] - 3, beacon_pos[1] - 3, beacon_pos[0] + 3, beacon_pos[1] + 3], fill=(255, 220, 200, 255))

    # Weathered Wasteland Pennant / Banner on mast tip
    flag_pts = [(tx, roof_y - mast_h + 10), (tx + 50, roof_y - mast_h + 22), (tx, roof_y - mast_h + 34)]
    d.polygon(flag_pts, fill=(215, 68, 38, 255), outline=(130, 35, 20, 255))
    d.line([(tx + 10, roof_y - mast_h + 18), (tx + 26, roof_y - mast_h + 26)], fill=(245, 235, 215, 255), width=2)

    # Scrap Satellite Dish mounted on tower leg
    dish_x = tx - tw_top_w*0.5 - 24
    dish_y = ty_top - 35
    d.ellipse([dish_x - 22, dish_y - 32, dish_x + 22, dish_y + 32], fill=(160, 165, 172, 255), outline=(90, 95, 100, 255), width=2)
    d.line([(dish_x, dish_y), (dish_x + 28, dish_y + 12)], fill=(110, 115, 120, 255), width=3)

    # ----------------------------------------------------
    # 3. POWER GENERATION - WIND TURBINE
    # ----------------------------------------------------
    wt_x = bx + 440
    wt_y_base = by + 210
    wt_h = 310
    d.line([(wt_x, wt_y_base), (wt_x, wt_y_base - wt_h)], fill=(115, 120, 128, 255), width=8)
    d.line([(wt_x + 2, wt_y_base), (wt_x + 2, wt_y_base - wt_h)], fill=(185, 190, 198, 255), width=3)
    nac_y = wt_y_base - wt_h
    d.rectangle([wt_x - 16, nac_y - 12, wt_x + 24, nac_y + 12], fill=(175, 65, 42, 255), outline=(85, 30, 20, 255), width=2)
    for rot_deg in (15, 135, 255):
        rad = math.radians(rot_deg)
        bl_len = 115
        bl_end = (wt_x + math.cos(rad) * bl_len, nac_y + math.sin(rad) * bl_len)
        d.line([(wt_x, nac_y), bl_end], fill=(225, 220, 212, 255), width=6)
        d.line([(wt_x + math.cos(rad)*15, nac_y + math.sin(rad)*15), bl_end], fill=(255, 255, 255, 255), width=2)

    # ----------------------------------------------------
    # 4. WASTELAND BUGGY / SCOUT VEHICLE (Parked near gate)
    # ----------------------------------------------------
    vx = bx - 220
    vy = by + 165
    
    d.ellipse([vx - 75, vy + 40, vx + 85, vy + 75], fill=(35, 20, 18, 210))
    d.rectangle([vx - 62, vy + 18, vx - 36, vy + 58], fill=(30, 32, 35, 255))
    d.rectangle([vx + 38, vy + 18, vx + 64, vy + 58], fill=(30, 32, 35, 255))
    d.ellipse([vx - 54, vy + 28, vx - 44, vy + 48], fill=(150, 155, 160, 255))
    d.ellipse([vx + 46, vy + 28, vx + 56, vy + 48], fill=(150, 155, 160, 255))
    buggy_body = [(vx - 68, vy + 22), (vx + 72, vy + 22),
                  (vx + 65, vy - 2), (vx + 18, vy - 6),
                  (vx - 25, vy + 4), (vx - 68, vy + 8)]
    d.polygon(buggy_body, fill=(185, 82, 36, 255), outline=(95, 40, 18, 255), width=2)
    cage_pts = [(vx - 18, vy - 6), (vx + 12, vy - 6), (vx + 32, vy - 32), (vx - 10, vy - 32)]
    d.polygon(cage_pts, outline=(55, 58, 62, 255), width=3)
    d.rectangle([vx - 74, vy + 12, vx - 68, vy + 32], fill=(90, 95, 100, 255))
    d.ellipse([vx - 72, vy + 15, vx - 64, vy + 23], fill=(255, 235, 140, 255))

    # ----------------------------------------------------
    # 5. PERIMETER PROPS: BARRELS, CRATES, TANK TRAPS
    # ----------------------------------------------------
    barrels = [
        (bx - 165, by + 125, 26, 42, (178, 55, 38)),
        (bx - 140, by + 128, 26, 42, (215, 155, 35)),
        (bx - 152, by + 88,  26, 42, (165, 48, 32)),
        (bx - 65,  by + 160, 26, 42, (45, 105, 98)),
        (bx - 42,  by + 164, 26, 42, (185, 62, 42)),
        (bx + 85,  by + 185, 28, 45, (205, 145, 32)),
        (bx + 112, by + 188, 28, 45, (48, 110, 102)),
    ]
    for bar_x, bar_y, bar_w, bar_h, bar_col in barrels:
        d.rectangle([bar_x, bar_y, bar_x + bar_w, bar_y + bar_h], fill=bar_col, outline=(35, 25, 20, 255), width=2)
        d.line([(bar_x, bar_y + 12), (bar_x + bar_w, bar_y + 12)], fill=(35, 25, 20, 255), width=2)
        d.line([(bar_x, bar_y + 28), (bar_x + bar_w, bar_y + 28)], fill=(35, 25, 20, 255), width=2)
        d.line([(bar_x + 3, bar_y + 2), (bar_x + bar_w - 3, bar_y + 2)], fill=(245, 185, 125, 255), width=2)

    # Tank Traps (Czech Hedgehogs) defending the perimeter
    for trap_x, trap_y, trap_s in [
        (bx - 310, by + 185, 32),
        (bx - 260, by + 235, 36),
        (bx - 180, by + 280, 42),
        (bx - 70,  by + 315, 46),
        (bx + 60,  by + 335, 48),
        (bx + 190, by + 345, 48),
    ]:
        d.line([(trap_x - trap_s, trap_y + trap_s*0.7), (trap_x + trap_s, trap_y - trap_s*0.7)], fill=(62, 65, 70, 255), width=6)
        d.line([(trap_x - trap_s*0.6, trap_y - trap_s*0.7), (trap_x + trap_s*0.6, trap_y + trap_s*0.7)], fill=(52, 55, 60, 255), width=6)
        d.line([(trap_x, trap_y - trap_s), (trap_x, trap_y + trap_s*0.8)], fill=(75, 78, 85, 255), width=6)

    # ----------------------------------------------------
    # 6. VEGETATION - DESERT CACTI
    # ----------------------------------------------------
    def draw_cactus(cx, cy, ch, cw):
        d.rectangle([cx - cw*0.5, cy - ch, cx + cw*0.5, cy], fill=(62, 98, 72, 255), outline=(32, 52, 38, 255), width=2)
        d.rectangle([cx - cw*1.6, cy - ch*0.65, cx - cw*0.5, cy - ch*0.5], fill=(62, 98, 72, 255), outline=(32, 52, 38, 255), width=2)
        d.rectangle([cx - cw*1.6, cy - ch*0.85, cx - cw*0.8, cy - ch*0.65], fill=(62, 98, 72, 255), outline=(32, 52, 38, 255), width=2)
        d.rectangle([cx + cw*0.5, cy - ch*0.5, cx + cw*1.5, cy - ch*0.35], fill=(62, 98, 72, 255), outline=(32, 52, 38, 255), width=2)
        d.rectangle([cx + cw*0.8, cy - ch*0.72, cx + cw*1.5, cy - ch*0.5], fill=(62, 98, 72, 255), outline=(32, 52, 38, 255), width=2)
        d.line([(cx + cw*0.5 - 1, cy - ch), (cx + cw*0.5 - 1, cy)], fill=(115, 165, 125, 255), width=2)

    draw_cactus(W * 0.28, H * 0.74, 95, 18)
    draw_cactus(W * 0.35, H * 0.78, 75, 15)
    draw_cactus(W * 0.88, H * 0.78, 120, 22)
    draw_cactus(W * 0.94, H * 0.82, 85, 16)

def draw_cinematic_lighting_and_ui_negative_space(img):
    """Render the towering searchlight beam, warm lantern glows, dust motes and left-side menu darkening."""
    w, h = img.size
    
    # 1. Volumetric Searchlight Beam with soft Gaussian blur
    beam_layer = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    d_beam = ImageDraw.Draw(beam_layer)
    
    bx = w * 0.62
    by = h * 0.57
    tx = bx + 220
    ty_base = by + 135
    tw_h = 440
    ty_top = ty_base - tw_h
    
    searchlight_origin = (tx - 35, ty_top - 12)
    beam_end_left = (-w * 0.05, h * 1.15)
    beam_end_right = (w * 0.46, h * 1.15)
    
    d_beam.polygon([searchlight_origin, beam_end_left, beam_end_right], fill=(255, 240, 180, 45))
    core_left = (w * 0.08, h * 1.15)
    core_right = (w * 0.36, h * 1.15)
    d_beam.polygon([searchlight_origin, core_left, core_right], fill=(255, 252, 220, 65))
    
    beam_blurred = beam_layer.filter(ImageFilter.GaussianBlur(25))
    img.paste(Image.alpha_composite(img.convert('RGBA'), beam_blurred).convert('RGB'), (0, 0))

    # 2. Atmospheric Overlays (Glows, Dust, UI Darkening)
    overlay = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    d_over = ImageDraw.Draw(overlay)
    
    # Searchlight fixture glow
    d_over.ellipse([searchlight_origin[0] - 30, searchlight_origin[1] - 30,
                    searchlight_origin[0] + 30, searchlight_origin[1] + 30],
                   fill=(255, 255, 220, 180))

    # Workshop & Base Warm Atmospheric Glow
    workshop_pos = (bx - 70, by + 65)
    for r, a in ((260, 16), (170, 28), (95, 45), (45, 90)):
        d_over.ellipse([workshop_pos[0] - r, workshop_pos[1] - r,
                        workshop_pos[0] + r, workshop_pos[1] + r],
                       fill=(255, 165, 55, a))
                       
    # Tower Beacon Glow
    beacon_screen = (tx, ty_top - 145)
    for r, a in ((80, 20), (45, 40), (20, 95)):
        d_over.ellipse([beacon_screen[0] - r, beacon_screen[1] - r,
                        beacon_screen[0] + r, beacon_screen[1] + r],
                       fill=(255, 60, 40, a))

    # Floating Sunlit Dust Motes
    np.random.seed(888)
    for _ in range(400):
        dx = np.random.uniform(0, w)
        dy = np.random.uniform(h * 0.25, h)
        dr = np.random.uniform(1.2, 4.5)
        da = int(np.random.uniform(25, 120))
        d_over.ellipse([dx - dr, dy - dr, dx + dr, dy + dr], fill=(255, 225, 155, da))

    # -------------------------------------------------------------
    # CRITICAL: Left-Side Menu Negative Space (RTS Menu UI Design Standard)
    # -------------------------------------------------------------
    # The left 42% of the screen is smoothly darkened with a deep post-apocalyptic slate-black gradient
    # This guarantees that the game title, logo, and all menu buttons (Play, Load, Settings, Quit)
    # will have 100% contrast, crystal-clear readability and zero visual clutter.
    ui_darken_arr = np.zeros((h, w, 4), dtype=np.uint8)
    dark_tone = np.array([12, 16, 22], dtype=np.float64)
    
    fade_width = int(w * 0.44)
    for x in range(w):
        if x < fade_width:
            t = x / float(fade_width)
            alpha = int(210 * ((1.0 - t)**1.35))
            ui_darken_arr[:, x, 0] = dark_tone[0]
            ui_darken_arr[:, x, 1] = dark_tone[1]
            ui_darken_arr[:, x, 2] = dark_tone[2]
            ui_darken_arr[:, x, 3] = alpha
            
    ui_darken_img = Image.fromarray(ui_darken_arr, mode='RGBA')
    overlay = Image.alpha_composite(overlay, ui_darken_img)

    # Vignette (Cinematic Edge Darkening)
    vig_arr = np.zeros((h, w, 4), dtype=np.uint8)
    cx, cy = w * 0.5, h * 0.5
    max_d = math.hypot(cx, cy)
    y_idx, x_idx = np.indices((h, w))
    dist_map = np.hypot(x_idx - cx, y_idx - cy)
    vig_t = np.clip((dist_map - max_d * 0.52) / (max_d * 0.48), 0.0, 1.0)
    vig_arr[..., 0] = 6
    vig_arr[..., 1] = 8
    vig_arr[..., 2] = 12
    vig_arr[..., 3] = (vig_t**2 * 135).astype(np.uint8)
    vig_img = Image.fromarray(vig_arr, mode='RGBA')
    overlay = Image.alpha_composite(overlay, vig_img)

    base_rgba = img.convert('RGBA')
    final_rgba = Image.alpha_composite(base_rgba, overlay)
    return final_rgba.convert('RGB')

def main():
    print(f"Creating Cinematic RTS Menu Background at {W}x{H} (final {TARGET_W}x{TARGET_H})...")
    img = create_sky_gradient(W, H)
    sun_pos = (int(W * 0.74), int(H * 0.46))
    draw_sun_and_rays(img, sun_pos, sun_r=70)
    draw_distant_mountains(img)
    draw_desert_dunes(img)
    draw_outpost_base(img, sun_pos)
    img = draw_cinematic_lighting_and_ui_negative_space(img)
    
    print(f"Downsampling to {TARGET_W}x{TARGET_H} via Lanczos...")
    final_img = img.resize((TARGET_W, TARGET_H), Image.Resampling.LANCZOS)
    
    path_assets = OUT_ASSETS / 'menu_background.png'
    path_docs = OUT_DOCS / 'menu_background.png'
    
    final_img.save(path_assets, 'PNG', optimize=True)
    final_img.save(path_docs, 'PNG', optimize=True)
    
    print(f"Successfully generated Menu Background:")
    print(f"  -> {path_assets}")
    print(f"  -> {path_docs}")

if __name__ == '__main__':
    main()
