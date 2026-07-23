from __future__ import annotations

from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[1]
MASTER = ROOT / "assets" / "branding" / "fotur-app-icon.png"
APP_ASSETS = ROOT / "src" / "FoturTypingHelper.App" / "Assets"
DMG_BACKGROUND = ROOT / "assets" / "branding" / "dmg-background.png"


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = [
        Path("C:/Windows/Fonts") / ("segoeuib.ttf" if bold else "segoeui.ttf"),
        Path("/System/Library/Fonts/SFNS.ttf"),
        Path("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf" if bold
             else "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"),
    ]
    for candidate in candidates:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size)
    return ImageFont.load_default()


def fitted_icon(side: int, content_ratio: float = 0.90) -> Image.Image:
    source = Image.open(MASTER).convert("RGBA")
    alpha = source.getchannel("A")
    bbox = alpha.getbbox()
    if bbox is None:
        raise RuntimeError("Master icon has no visible pixels")
    source = source.crop(bbox)
    target = int(side * content_ratio)
    source.thumbnail((target, target), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    canvas.alpha_composite(source, ((side - source.width) // 2, (side - source.height) // 2))
    return canvas


def make_app_assets() -> None:
    APP_ASSETS.mkdir(parents=True, exist_ok=True)
    app_icon = fitted_icon(1024)
    app_icon.save(APP_ASSETS / "FoturTypingHelper.png", optimize=True)
    app_icon.save(
        APP_ASSETS / "FoturTypingHelper.ico",
        format="ICO",
        sizes=[(16, 16), (20, 20), (24, 24), (32, 32), (40, 40),
               (48, 48), (64, 64), (128, 128), (256, 256)],
    )


def make_dmg_background() -> None:
    width, height = 660, 420
    image = Image.new("RGBA", (width, height), "#071014")
    pixels = image.load()
    for y in range(height):
        for x in range(width):
            t = (x / width) * 0.55 + (y / height) * 0.45
            pixels[x, y] = (
                int(7 + 8 * t),
                int(16 + 7 * t),
                int(20 + 11 * t),
                255,
            )

    glow = Image.new("RGBA", image.size, (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow)
    glow_draw.ellipse((-190, -250, 360, 290), fill=(93, 255, 244, 72))
    glow_draw.ellipse((380, 130, 850, 590), fill=(255, 91, 221, 60))
    glow = glow.filter(ImageFilter.GaussianBlur(92))
    image = Image.alpha_composite(image, glow)

    draw = ImageDraw.Draw(image)
    small_icon = fitted_icon(74, 0.94)
    image.alpha_composite(small_icon, (42, 31))
    draw.text((126, 40), "Fotur Typing Helper", font=font(26, True), fill="#F4FAF9")
    draw.text((127, 76), "Drag to Applications to install", font=font(14), fill="#9EB5B4")

    # Finder places the application and Applications alias over these two zones.
    cyan, violet, magenta = (93, 255, 244), (141, 107, 255), (255, 91, 221)
    y = 246
    for index in range(220):
        t = index / 219
        if t < 0.5:
            local = t * 2
            color = tuple(int(cyan[i] * (1 - local) + violet[i] * local) for i in range(3))
        else:
            local = (t - 0.5) * 2
            color = tuple(int(violet[i] * (1 - local) + magenta[i] * local) for i in range(3))
        x = 220 + index
        draw.line((x, y, x + 1, y), fill=color + (225,), width=4)
    draw.polygon([(449, y), (432, y - 11), (432, y + 11)], fill=magenta + (240,))

    draw.rounded_rectangle((35, 356, 625, 389), radius=14, fill="#142126",
                           outline="#26383C", width=1)
    draw.text(
        (330, 372),
        "After copying, launch Fotur from Applications",
        anchor="mm",
        font=font(12),
        fill="#A8BCBB",
    )
    image.convert("RGB").save(DMG_BACKGROUND, quality=95, optimize=True)


if __name__ == "__main__":
    make_app_assets()
    make_dmg_background()
    print(f"Generated {APP_ASSETS / 'FoturTypingHelper.png'}")
    print(f"Generated {APP_ASSETS / 'FoturTypingHelper.ico'}")
    print(f"Generated {DMG_BACKGROUND}")
