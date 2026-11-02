"""Generate original UI placeholders. Requires Python 3 and Pillow.
Run from any directory: python Tools/generate_ui_sprites.py
"""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets' / 'Art' / 'UI' / 'Generated'
DOC = ROOT / 'Docs' / 'UI'
SCALE = 3
MANIFEST = {}
INK = '#151b20'
EDGE = '#596264'
SAND = '#c9b58b'
ORANGE = '#ed9850'


def save(name, size, draw, border=0):
    image = Image.new('RGBA', (size[0]*SCALE, size[1]*SCALE))
    painter = ImageDraw.Draw(image)
    def box(rect, fill, outline=None, width=1):
        painter.rectangle(tuple(round(v*SCALE) for v in rect), fill=fill,
                          outline=outline, width=width*SCALE)
    def poly(points, fill):
        painter.polygon([(round(x*SCALE), round(y*SCALE)) for x,y in points], fill=fill)
    draw(box, poly)
    image = image.resize(size, Image.Resampling.LANCZOS)
    image.save(OUT / (name + '.png'))
    MANIFEST[name] = {'width':size[0], 'height':size[1], 'border':border}


def panel(name, size, accent=False):
    w,h = size
    def draw(b,p):
        b((1,1,w-2,h-2), INK)
        b((3,3,w-4,h-4), '#252d31', EDGE, 2)
        b((7,7,w-8,h-8), '#20272b', '#333d40')
        b((14,12,42,15), ORANGE if accent else SAND)
        for x in (12,w-13):
            for y in (24,h-13):
                b((x-2,y-2,x+2,y+2), '#11191d')
                b((x-1,y-1,x+1,y), '#84908d')
    save(name,size,draw,28)


def button(name, size, state):
    w,h=size
    fill,edge={'normal':('#303a3e',EDGE), 'hover':('#3d484b',SAND),
               'pressed':('#714d32',ORANGE), 'disabled':('#242b2e','#3e484b')}[state]
    def draw(b,p):
        b((1,1,w-2,h-2), INK)
        b((3,3,w-4,h-4),fill,edge,2)
        if state!='disabled':
            b((8,7,w-9,8),'#627073' if state!='pressed' else '#b57643')
            b((8,h-9,w-9,h-8),'#1b2529')
        if state in ('hover','pressed'):
            b((4,9,7,h-10),ORANGE)
    save(name,size,draw,12)


def build():
    OUT.mkdir(parents=True,exist_ok=True)
    DOC.mkdir(parents=True,exist_ok=True)
    panel('panel_menu',(256,256),True)
    panel('panel_pause',(256,256),True)
    panel('panel_build',(256,128))
    for state in ('normal','hover','pressed','disabled'):
        button('button_'+state,(256,64),state)
        button('build_slot_'+state,(96,96),state)
    save('building_placeholder',(64,64),lambda b,p:b((0,0,64,64),'white'))
    save('overlay_pause',(8,8),lambda b,p:b((0,0,8,8),(7,12,16,185)))
    (OUT/'sprites.json').write_text(json.dumps(MANIFEST,indent=2)+'\n',encoding='utf-8')
    preview()
    # Reopen exported files, not the in-memory canvases.
    for name, spec in MANIFEST.items():
        with Image.open(OUT/(name+'.png')) as im:
            assert im.mode=='RGBA' and im.size==(spec['width'],spec['height']),name
            if name=='building_placeholder':
                assert im.getextrema()==((255,255),)*4
            else:
                assert im.getextrema()[3][0]<255,name
    print(f'Validated {len(MANIFEST)} RGBA sprites in {OUT}')
    print(f'Preview: {DOC / "ui_preview.png"}')


def preview():
    im=Image.new('RGBA',(1440,880),'#10181c')
    d=ImageDraw.Draw(im)
    font_path=Path('C:/Windows/Fonts/segoeui.ttf')
    def font(n):
        return ImageFont.truetype(str(font_path),n) if font_path.exists() else ImageFont.load_default(size=n)
    def text(pos,s,n=20,color=SAND):
        d.text(pos,s,font=font(n),fill=color)
    def paste(name,rect):
        asset=Image.open(OUT/(name+'.png')).convert('RGBA')
        # Preview uses the same nine-slice geometry as Unity, not stretched borders.
        w,h=rect[2],rect[3]
        border=MANIFEST[name]['border']
        if border:
            srcx=(0,border,asset.width-border,asset.width)
            srcy=(0,border,asset.height-border,asset.height)
            dstx=(0,border,w-border,w); dsty=(0,border,h-border,h)
            for j in range(3):
                for i in range(3):
                    patch=asset.crop((srcx[i],srcy[j],srcx[i+1],srcy[j+1]))
                    patch=patch.resize((dstx[i+1]-dstx[i],dsty[j+1]-dsty[j]),Image.Resampling.LANCZOS)
                    im.alpha_composite(patch,(rect[0]+dstx[i],rect[1]+dsty[j]))
        else:
            im.alpha_composite(asset.resize((w,h)),rect[:2])
    text((40,24),'ПУСТОШЬ / НАБОР ИНТЕРФЕЙСА',30)
    text((40,68),'Программная отрисовка • надписи только на превью • постройки — белые квадраты',18,'#8f9e9e')
    text((40,126),'ГЛАВНОЕ МЕНЮ',18)
    paste('panel_menu',(40,164,400,460))
    text((74,197),'НОВЫЙ РУБЕЖ',32)
    text((74,245),'Рабочее название',16,'#8f9e9e')
    for y,label,state in [(300,'Новая игра','hover'),(382,'Настройки','normal'),(464,'Выход','normal')]:
        paste('button_'+state,(72,y,336,64)); text((98,y+16),label,22)
    text((490,126),'ПАУЗА',18)
    paste('panel_pause',(490,164,400,460))
    text((524,197),'ПАУЗА',32)
    for y,label in [(300,'Продолжить'),(382,'Настройки'),(464,'В главное меню')]:
        paste('button_normal',(522,y,336,64)); text((548,y+16),label,22)
    text((940,126),'СОСТОЯНИЯ КНОПОК',18)
    for y,state,label in [(176,'normal','Обычная'),(276,'hover','Наведение'),(376,'pressed','Нажата / выбрана'),(476,'disabled','Недоступна')]:
        paste('button_'+state,(940,y,400,64)); text((966,y+17),label,20)
    text((40,670),'ВЫБОР ПОСТРОЕК / ЗАГЛУШКИ',18)
    paste('panel_build',(40,710,1300,140))
    for i,state in enumerate(('pressed','normal','hover','disabled')):
        x=72+i*114
        paste('build_slot_'+state,(x,732,96,96))
        paste('building_placeholder',(x+24,756,48,48))
    text((580,752),'Белые квадраты заменяются спрайтами зданий.',20)
    text((580,786),'Рамки поддерживают 9-slice; текст добавляется в Unity.',18,'#8f9e9e')
    im.convert('RGB').save(DOC/'ui_preview.png')

if __name__=='__main__':
    build()
