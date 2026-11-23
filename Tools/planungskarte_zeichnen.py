"""Zeichnet aus dem Gelaende-Export eine 2D-Karte zum Einzeichnen der Viertel."""
import zlib, struct, sys

SRC = r"C:\dev\PROJECT RIO\Screenshots\terrain_export.txt"
OUT = r"C:\dev\PROJECT RIO\Screenshots\RIO_PLANUNGSKARTE.png"

# ---------------------------------------------------------------- PNG
def write_png(path, w, h, rows):
    raw = b"".join(b"\x00" + bytes(r) for r in rows)
    def chunk(tag, data):
        c = tag + data
        return struct.pack(">I", len(data)) + c + struct.pack(">I", zlib.crc32(c) & 0xffffffff)
    hdr = struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0)
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", hdr)
                + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))

# ---------------------------------------------------------------- Font 5x7
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
 ':':(0x00,0x36,0x36,0x00,0x00),'(':(0x00,0x1C,0x22,0x41,0x00),')':(0x00,0x41,0x22,0x1C,0x00),
 '/':(0x20,0x10,0x08,0x04,0x02),'+':(0x08,0x08,0x3E,0x08,0x08),'=':(0x14,0x14,0x14,0x14,0x14),
}

# ---------------------------------------------------------------- Daten lesen
with open(SRC, encoding="utf-8") as f:
    head = f.readline().split()
    minX, minZ, cell = float(head[0]), float(head[1]), float(head[2])
    nx, nz = int(head[3]), int(head[4])
    grid = []
    for _ in range(nz):
        row = []
        for tok in f.readline().split():
            row.append((tok[0], float(tok[1:])))
        grid.append(row)

def at(ix, iz):
    ix = max(0, min(nx - 1, ix)); iz = max(0, min(nz - 1, iz))
    return grid[iz][ix]

# ---------------------------------------------------------------- Leinwand
PX = 9                     # Pixel je 40-m-Zelle  -> 0,15 px/m
MAPW, MAPH = nx * PX, nz * PX
L, T, R, B = 54, 54, 16, 150
W, H = MAPW + L + R, MAPH + T + B
canvas = [bytearray(b"\xf4\xf2\xec" * W) for _ in range(H)]

def put(x, y, c):
    if 0 <= x < W and 0 <= y < H:
        i = x * 3
        canvas[y][i:i+3] = bytes(c)

def rect(x0, y0, x1, y1, c):
    for y in range(max(0, y0), min(H, y1)):
        row = canvas[y]
        for x in range(max(0, x0), min(W, x1)):
            row[x*3:x*3+3] = bytes(c)

def text(x, y, s, c, scale=1):
    for ch in s.upper():
        g = F.get(ch, F[' '])
        for col in range(5):
            bits = g[col]
            for rw in range(7):
                if bits & (1 << rw):
                    rect(x + col*scale, y + rw*scale, x + (col+1)*scale, y + (rw+1)*scale, c)
        x += 6 * scale
    return x

def line(x0, y0, x1, y1, c):
    dx, dy = abs(x1-x0), abs(y1-y0)
    sx = 1 if x0 < x1 else -1
    sy = 1 if y0 < y1 else -1
    err = dx - dy
    while True:
        put(x0, y0, c)
        if x0 == x1 and y0 == y1: break
        e2 = 2*err
        if e2 > -dy: err -= dy; x0 += sx
        if e2 < dx:  err += dx; y0 += sy

# ---------------------------------------------------------------- Farben
WATER  = (0x1D,0x6E,0x88)
SAND   = (0xE9,0xDB,0xB4)
FLAT   = (0xD3,0xE0,0xC0)
HANG   = (0xA6,0xBA,0x8C)
STEEP  = (0x8A,0x81,0x6D)
ROAD   = (0x45,0x45,0x48)
GRIDL  = (0xB2,0xBC,0xA8)
GRIDB  = (0x74,0x80,0x6C)
INK    = (0x20,0x24,0x28)
MARK   = (0xB5,0x40,0x2F)

def slope_at(ix, iz):
    h = at(ix, iz)[1]
    d = 0.0
    for ox, oz in ((1,0),(-1,0),(0,1),(0,-1)):
        n = at(ix+ox, iz+oz)
        if n[1] > -50: d = max(d, abs(n[1] - h))
    return d / cell

# ---------------------------------------------------------------- Gelaende
for iz in range(nz):
    for ix in range(nx):
        code, h = grid[iz][ix]
        if code == 'A' and h > 150: code, h = 'W', -1   # Weltbegrenzung
        if code == 'W' or h < 0.3:
            c = WATER
        elif code == 'S':
            c = SAND
        elif code == 'A':
            c = ROAD
        else:
            s = slope_at(ix, iz)
            c = FLAT if s < 0.10 else (HANG if s < 0.30 else STEEP)
        px = L + ix * PX
        py = T + (nz - 1 - iz) * PX
        rect(px, py, px + PX, py + PX, c)

# ---------------------------------------------------------------- Raster
def wx2px(x): return L + int((x - minX) / cell * PX)
def wz2py(z): return T + int((nz - 1 - (z - minZ) / cell) * PX)

step = 500.0
x = minX
while x <= minX + nx*cell:
    px = wx2px(x)
    big = abs((x / 1000.0) - round(x / 1000.0)) < 1e-6
    rect(px, T, px + (2 if big else 1), T + MAPH, GRIDB if big else GRIDL)
    x += step
z = minZ
while z <= minZ + nz*cell:
    py = wz2py(z)
    big = abs((z / 1000.0) - round(z / 1000.0)) < 1e-6
    rect(L, py, L + MAPW, py + (2 if big else 1), GRIDB if big else GRIDL)
    z += step

# Spalten- und Zeilenbeschriftung
letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
i = 0
x = minX
while x < minX + nx*cell:
    px = wx2px(x + step/2)
    text(px - 6, T - 22, letters[i], INK, 2)
    i += 1; x += step
i = 1
z = minZ + nx*0  # unten anfangen
zz = minZ + (nz*cell) - step
while zz >= minZ - 1:
    py = wz2py(zz + step/2)
    text(8, py - 7, str(i), INK, 2)
    i += 1; zz -= step

# ---------------------------------------------------------------- Orte
def to_world(px_, py_): return ((px_ - 845.0) * 8.0, (540.0 - py_) * 8.0)

PLACES = [
 ("HAFEN",525,448),("FLUGHAFEN GROSS",648,448),("JEWELERY SUL",560,524),
 ("BANCO CENTRAL",737,524),("HOSPITAL RIO",650,570),("CLOTHSTORE",730,580),
 ("BANCO DO MORRO",656,620),("ESTADIO",566,620),("AIRSTRIP OESTE",436,620),
 ("JESUS-STATUE",680,762),("NIGHTCLUB",1030,258),("HOSPITAL NITEROI",1150,376),
 ("JEWELERY NITEROI",1020,430),("CASINO",986,466),("BANCO NITEROI",1070,456),
 ("AIRSTRIP NITEROI",1186,490),("MILITAERBASIS",1236,586),
]

for n, (name, sx, sy) in enumerate(PLACES, 1):
    wx, wz = to_world(sx, sy)
    px, py = wx2px(wx), wz2py(wz)
    rect(px-7, py-7, px+8, py+8, (0xF4,0xF2,0xEC))
    rect(px-6, py-6, px+7, py+7, MARK)
    lab = str(n)
    text(px - (3 if len(lab) == 1 else 6), py - 3, lab, (0xFF,0xFF,0xFF), 1)

# Strand- und Seemittelpunkt
for name, sx, sy, col in (("STRAND",812,702,(0xE0,0xA8,0x30)), ("SEE",556,700,(0x30,0x90,0xC0))):
    wx, wz = to_world(sx, sy)
    px, py = wx2px(wx), wz2py(wz)
    for r in range(9, 12):
        for a in range(0, 360, 4):
            import math
            put(px + int(r*math.cos(math.radians(a))), py + int(r*math.sin(math.radians(a))), col)

# Rahmen
rect(L-2, T-2, L+MAPW+2, T, INK); rect(L-2, T+MAPH, L+MAPW+2, T+MAPH+2, INK)
rect(L-2, T-2, L, T+MAPH+2, INK); rect(L+MAPW, T-2, L+MAPW+2, T+MAPH+2, INK)

# ---------------------------------------------------------------- Legende
ly = T + MAPH + 14
text(L, ly, "RIO - PLANUNGSKARTE   1 KAESTCHEN = 500 M   WELT 8200 X 5600 M", INK, 2)
ly += 22
sw = 26
items = [(WATER,"WASSER"),(SAND,"STRAND"),(FLAT,"FLACH - HIER GEHT STADT"),
         (HANG,"HANG - FAVELA MOEGLICH"),(STEEP,"STEIL - BERG"),(ROAD,"BESTEHENDE STRASSE / PISTE")]
lx = L
for c, lab in items:
    rect(lx, ly, lx+sw, ly+12, c); rect(lx, ly, lx+sw, ly+1, INK)
    lx = text(lx + sw + 6, ly + 3, lab, INK, 1) + 22
ly += 20

names = [f"{i}={n}" for i, (n, _, _) in enumerate(PLACES, 1)]
lx = L
for s in names:
    if lx > W - 300:
        lx = L; ly += 12
    lx = text(lx, ly, s, INK, 1) + 14
ly += 16
text(L, ly, "GELB=STRANDBOGEN   BLAU=SEE   ROT=ORTE AUS DER LEGENDE", INK, 1)

write_png(OUT, W, H, canvas)
print(f"geschrieben: {OUT}  ({W}x{H} px)")
