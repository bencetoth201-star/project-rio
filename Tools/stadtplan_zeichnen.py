"""Zeichnet den exportierten Stadtplan als Karte."""
import zlib, struct, math, sys

PLAN = r"C:\dev\PROJECT RIO\Screenshots\rio_plan.txt"
TERR = r"C:\dev\PROJECT RIO\Screenshots\terrain_export.txt"
OUT  = r"C:\dev\PROJECT RIO\Screenshots\RIO_STADTPLAN.png"

SCALE = 0.30          # Pixel je Meter

def write_png(path, w, h, rows):
    raw = b"".join(b"\x00" + bytes(r) for r in rows)
    def chunk(tag, data):
        c = tag + data
        return struct.pack(">I", len(data)) + c + struct.pack(">I", zlib.crc32(c) & 0xffffffff)
    hdr = struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0)
    open(path, "wb").write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", hdr)
          + chunk(b"IDAT", zlib.compress(raw, 6)) + chunk(b"IEND", b""))

F = {
 '0':(0x3E,0x51,0x49,0x45,0x3E),'1':(0x00,0x42,0x7F,0x40,0x00),'2':(0x42,0x61,0x51,0x49,0x46),
 '3':(0x21,0x41,0x45,0x4B,0x31),'4':(0x18,0x14,0x12,0x7F,0x10),'5':(0x27,0x45,0x45,0x45,0x39),
 '6':(0x3C,0x4A,0x49,0x49,0x30),'7':(0x01,0x71,0x09,0x05,0x03),'8':(0x36,0x49,0x49,0x49,0x36),
 '9':(0x06,0x49,0x49,0x29,0x1E),
 'A':(0x7E,0x11,0x11,0x11,0x7E),'B':(0x7F,0x49,0x49,0x49,0x36),'C':(0x3E,0x41,0x41,0x41,0x22),
 'D':(0x7F,0x41,0x41,0x22,0x1C),'E':(0x7F,0x49,0x49,0x49,0x41),'F':(0x7F,0x09,0x09,0x09,0x01),
 'G':(0x3E,0x41,0x49,0x49,0x7A),'H':(0x7F,0x08,0x08,0x08,0x7F),'I':(0x00,0x41,0x7F,0x41,0x00),
 'J':(0x20,0x40,0x41,0x3F,0x01),'K':(0x7F,0x08,0x14,0x22,0x41),'L':(0x7F,0x40,0x40,0x40,0x40),
 'M':(0x7F,0x02,0x0C,0x02,0x7F),'N':(0x7F,0x04,0x08,0x10,0x7F),'O':(0x3E,0x41,0x41,0x41,0x3E),
 'P':(0x7F,0x09,0x09,0x09,0x06),'Q':(0x3E,0x41,0x51,0x21,0x5E),'R':(0x7F,0x09,0x19,0x29,0x46),
 'S':(0x46,0x49,0x49,0x49,0x31),'T':(0x01,0x01,0x7F,0x01,0x01),'U':(0x3F,0x40,0x40,0x40,0x3F),
 'V':(0x1F,0x20,0x40,0x20,0x1F),'W':(0x3F,0x40,0x38,0x40,0x3F),'X':(0x63,0x14,0x08,0x14,0x63),
 'Y':(0x07,0x08,0x70,0x08,0x07),'Z':(0x61,0x51,0x49,0x45,0x43),
 ' ':(0,0,0,0,0),'-':(0x08,0x08,0x08,0x08,0x08),'.':(0x00,0x60,0x60,0x00,0x00),
 '=':(0x14,0x14,0x14,0x14,0x14),'/':(0x20,0x10,0x08,0x04,0x02),':':(0x00,0x36,0x36,0x00,0x00),
}

# ---------------------------------------------------------------- Plan lesen
f = open(PLAN, encoding="utf-8")
head = f.readline().split()
minX, minZ, cell = float(head[1]), float(head[2]), float(head[3])
gw, gh, sw, sh = int(head[4]), int(head[5]), int(head[6]), int(head[7])

assert f.readline().strip() == "MASK"
mask = [f.readline().rstrip("\n") for _ in range(gh)]      # Zeile 0 = Norden

assert f.readline().strip() == "KIND"
kind = [f.readline().rstrip("\n") for _ in range(sh)]

n = int(f.readline().split()[1])
places = []
for _ in range(n):
    a, b, c, d = f.readline().split()
    places.append((float(a), float(b), float(c), int(d)))

m = int(f.readline().split()[1])
models = {}
for _ in range(m):
    parts = f.readline().split()
    i = int(parts[0])
    models[i] = (float(parts[1]), float(parts[2]), float(parts[3]), parts[4] == "1", parts[5])
f.close()

WORLD_W, WORLD_H = gw * cell, gh * cell
MAPW, MAPH = int(WORLD_W * SCALE), int(WORLD_H * SCALE)
L, T, R, B = 50, 50, 16, 120
W, H = MAPW + L + R, MAPH + T + B
canvas = [bytearray(b"\xf4\xf2\xec" * W) for _ in range(H)]

def rect(x0, y0, x1, y1, c):
    if x1 <= x0: x1 = x0 + 1
    if y1 <= y0: y1 = y0 + 1
    cb = bytes(c)
    for y in range(max(0, y0), min(H, y1)):
        row = canvas[y]
        for x in range(max(0, x0), min(W, x1)):
            row[x*3:x*3+3] = cb

def text(x, y, s, c, sc=1):
    for ch in s.upper():
        g = F.get(ch, F[' '])
        for col in range(5):
            bits = g[col]
            for rw in range(7):
                if bits & (1 << rw):
                    rect(x+col*sc, y+rw*sc, x+(col+1)*sc, y+(rw+1)*sc, c)
        x += 6*sc
    return x

def px(wx): return L + int((wx - minX) * SCALE)
def py(wz): return T + MAPH - int((wz - minZ) * SCALE)

# ---------------------------------------------------------------- Hintergrund
WATER = (0x1D,0x6E,0x88); LAND = (0xD8,0xDF,0xCC); BEACH = (0xE9,0xDB,0xB4)
HILL  = (0xB9,0xBE,0x9E); STEEP = (0x9A,0x92,0x7C)
rect(L, T, L+MAPW, T+MAPH, WATER)

tf = open(TERR, encoding="utf-8")
th = tf.readline().split()
tminX, tminZ, tcell, tnx, tnz = float(th[0]), float(th[1]), float(th[2]), int(th[3]), int(th[4])
tg = []
for _ in range(tnz):
    tg.append([(t[0], float(t[1:])) for t in tf.readline().split()])
tf.close()

def tslope(ix, iz):
    h = tg[iz][ix][1]; d = 0.0
    for ox, oz in ((1,0),(-1,0),(0,1),(0,-1)):
        jx, jz = min(max(ix+ox,0),tnx-1), min(max(iz+oz,0),tnz-1)
        if tg[jz][jx][1] > -50: d = max(d, abs(tg[jz][jx][1]-h))
    return d/tcell

step = max(1, int(tcell*SCALE))
for iz in range(tnz):
    for ix in range(tnx):
        code, h = tg[iz][ix]
        if code == 'A' and h > 150: code = 'W'
        if code == 'W' or h < 0.3: c = WATER
        elif code == 'S': c = BEACH
        else:
            sl = tslope(ix, iz)
            c = LAND if sl < 0.10 else (HILL if sl < 0.30 else STEEP)
        x0 = px(tminX + ix*tcell); y0 = py(tminZ + (iz+1)*tcell)
        rect(x0, y0, x0+step+1, y0+step+1, c)

# ---------------------------------------------------------------- Viertel
KINDC = [(0xC9,0xB6,0xD6),(0xA8,0xCF,0xE0),(0xDC,0xDC,0xC2),(0xC2,0xE2,0xBE),(0xD6,0xC2,0xA8)]
KINDN = ["CENTRO","ORLA","BAIRRO","NOBRE","INDUSTRIE"]
kstep = max(1, int(cell*SCALE)+1)
for gz in range(gh):
    row = kind[gh-1-gz]
    for gx in range(gw):
        if gx >= len(row): continue
        ch = row[gx]
        if ch == '9': continue
        x0 = px(minX + gx*cell); y0 = py(minZ + (gz+1)*cell)
        rect(x0, y0, x0+kstep, y0+kstep, KINDC[int(ch)])

# ---------------------------------------------------------------- Bauten
ROAD = (0x3C,0x3C,0x40)
CLS = {}
def cls_colour(i):
    if i in CLS: return CLS[i]
    if i <= 4:    c = (0xC2,0x60,0x3F)          # Favela
    elif i <= 9:  c = (0xE6,0xD0,0x8A)          # einfach / mittel
    elif i <= 14: c = (0x7F,0xB0,0xD4)          # gehoben / reich
    elif i in (16,17): c = (0xD0,0x45,0xA0)     # Mansions
    elif i in (19,20): c = (0x2B,0x4C,0x8C)     # Tuerme
    elif i in (23,24,25): c = (0x45,0xA0,0x9A)  # Eckgebaeude
    elif i in (26,27): c = (0x4E,0x9E,0x4E)     # Praca / Parque
    else: c = (0x8A,0x5A,0xC2)                  # Sonderbauten
    CLS[i] = c
    return c

for (x, z, yaw, mi) in places:
    lw, ld, hgt, isroad, name = models[mi]
    if int(yaw) % 180 == 90: lw, ld = ld, lw
    x0 = px(x - lw*0.5); x1 = px(x + lw*0.5)
    y1 = py(z - ld*0.5); y0 = py(z + ld*0.5)
    rect(x0, y0, x1, y1, ROAD if isroad else cls_colour(mi))

# ---------------------------------------------------------------- Raster
GRID = (0x70,0x78,0x6C)
g = 500.0
x = minX
while x <= minX + WORLD_W:
    rect(px(x), T, px(x)+1, T+MAPH, GRID); x += g
z = minZ
while z <= minZ + WORLD_H:
    rect(L, py(z), L+MAPW, py(z)+1, GRID); z += g

letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
i = 0; x = minX
while x < minX + WORLD_W:
    text(px(x+g/2)-5, T-20, letters[i], (0x20,0x24,0x28), 2); i += 1; x += g
i = 1; z = minZ + WORLD_H - g
while z >= minZ - 1:
    text(8, py(z+g/2)-7, str(i), (0x20,0x24,0x28), 2); i += 1; z -= g

INK = (0x20,0x24,0x28)
rect(L-2, T-2, L+MAPW+2, T, INK); rect(L-2, T+MAPH, L+MAPW+2, T+MAPH+2, INK)
rect(L-2, T-2, L, T+MAPH+2, INK); rect(L+MAPW, T-2, L+MAPW+2, T+MAPH+2, INK)

# ---------------------------------------------------------------- Legende
ly = T + MAPH + 12
roads = sum(1 for p in places if models[p[3]][3])
text(L, ly, f"RIO - STADTPLAN   {len(places)-roads} BAUTEN   {roads} STRASSENKACHELN   1 KAESTCHEN = 500 M", INK, 2)
ly += 22
lx = L
items = [(ROAD,"STRASSE"),((0xC2,0x60,0x3F),"FAVELA"),((0xE6,0xD0,0x8A),"EINFACH/MITTEL"),
         ((0x7F,0xB0,0xD4),"GEHOBEN/REICH"),((0x2B,0x4C,0x8C),"TURM"),((0xD0,0x45,0xA0),"MANSION"),
         ((0x45,0xA0,0x9A),"ECKHAUS"),((0x4E,0x9E,0x4E),"PLATZ/PARK"),((0x8A,0x5A,0xC2),"SONDERBAU")]
for c, lab in items:
    rect(lx, ly, lx+22, ly+11, c); lx = text(lx+26, ly+2, lab, INK, 1) + 16
ly += 18
lx = L
for c, lab in zip(KINDC, KINDN):
    rect(lx, ly, lx+22, ly+11, c); lx = text(lx+26, ly+2, "VIERTEL "+lab, INK, 1) + 16

write_png(OUT, W, H, canvas)
print(f"geschrieben: {OUT}  ({W}x{H})")
