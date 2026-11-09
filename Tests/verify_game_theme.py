#!/usr/bin/env python3
"""Read-only game-art theme contract; does NOT claim compiled or Unity visual success."""
from pathlib import Path
import json, math, sys, zipfile

ROOT = Path(__file__).resolve().parents[1]
THEME = ROOT/'src/Foundation/UI/Theme/FoundationGameTheme.cs'
HOST = ROOT/'src/Foundation/UI/Panels/FoundationPanelHost.cs'
BUILDER = ROOT/'src/Foundation/UI/Panels/FoundationPanelBuilder.cs'
CONTEXT = ROOT/'src/Foundation/UI/ModBar/ModBarContext.cs'
MANIFEST = ROOT/'Docs/UI/Game-Asset-Theme-Snapshot.json'
checks=[]

def check(name, condition):
    checks.append({'check':name,'status':'PASS' if condition else 'FAIL'})

def read(path):return path.read_text(encoding='utf-8-sig') if path.exists() else ''
s=read(THEME);host=read(HOST);builder=read(BUILDER);ctx=read(CONTEXT)
manifest=json.loads(read(MANIFEST))
check('Native assets documented with SHA256 and game-update policy',bool(manifest.get('snapshotSha256')) and len(manifest['snapshotSha256'])==64 and 'update' in manifest['updatePolicy'])
check('Only native game sprites used',all(needle in s for needle in ('DecoRectBorder','DefaultBorderDark','SimpleBox','Button02','Button03','Resources.FindObjectsOfTypeAll<Sprite>()')))
check('Native Options button prioritized', 'Resources.FindObjectsOfTypeAll<OptionsUI>()' in s and 'optionsButton ?? Sprite("Button02")' in s)
check('Native frames are nine-sliced', 'Image.Type.Sliced' in s and 'NativeGameFrame' in s)
check('Panel background guaranteed opaque', 'FoundationGameTheme.ApplyPanel(background)' in host and 'image.color = Panel;' in s and '1f);' in s)
check('Native frame and theme used by popup host', 'FoundationGameTheme.AddPanelBorder(rect)' in host and 'bool hasNativeFrame' in host)
check('Panel width/height respect live canvas size', 'FitPanelWithinGameCanvas();' in host and 'Mathf.Min(560f' in host)
check('All custom-panel controls use game theme', all(k in builder for k in ('ApplyWell(background)','ApplySliderHandle(handleImage','ApplyWell(image)','effectColor = FoundationGameTheme.Brass')) and 'panelTheme: true' in host and 'FoundationGameTheme.ApplyButton(image' in ctx)
check('Existing mod bar rendering remains opt-in', 'bool panelTheme = false' in ctx)
check('Native Options UI stays cloned', 'Instantiate(buttonTemplate.gameObject' in read(ROOT/'src/Foundation/UI/Settings/ModConfigTemplates.cs'))
check('No green hard-coded into generic custom panel', 'new Color(.75f, .58f, .31f' not in builder and 'CopyImage(background, host.NativeButton)' not in builder)
check('TMP font protected from oversized text', all(x in s for x in ('text.fontSizeMin = Mathf.Min(13f, max)', 'text.overflowMode = TextOverflowModes.Ellipsis')) and 'label.textWrappingMode = TextWrappingModes.Normal' in builder)
check('Theme can fall back without raw game asset redistribution', 'if (sprite) UseSlice(image, sprite, Color.white);' in s and 'image.color = Well;' in s)
check('Existing one-production-DLL contract', '<AssemblyName>Foundation</AssemblyName>' in read(ROOT/'Foundation.csproj'))

def luminance(rgb):
 def channel(x):
  x=x/255
  return x/12.92 if x<=.04045 else ((x+.055)/1.055)**2.4
 r,g,b=map(channel,rgb)
 return .2126*r+.7152*g+.0722*b

def contrast(a,b):
 a,b=sorted([luminance(a),luminance(b)])
 return (b+.05)/(a+.05)
palette = manifest['pixelReference']
for foreground,background,target in [('primaryTextRGB','panelBackgroundRGB',7),('primaryTextRGB','inputWellRGB',7),('secondaryTextRGB','panelBackgroundRGB',7),('brassAccentRGB','panelBackgroundRGB',4.5)]:
 actual=contrast(palette[foreground],palette[background])
 check(f'Contrast {foreground} on {background}: {actual:.2f} >= {target}', actual >= target)

# Optional: if the permanent assets snapshot is mounted, validate the raw asset catalog.
ASSETS=Path(sys.argv[1]) if len(sys.argv)>1 else Path('/mnt/data/Assets.zip')
if ASSETS.exists():
 import hashlib
 check('Immutable assets snapshot SHA256 matches', hashlib.sha256(ASSETS.read_bytes()).hexdigest()==manifest['snapshotSha256'])
 with zipfile.ZipFile(ASSETS) as z:
  all_names=set(z.namelist())
  for name in manifest['primarySpriteNames']:
   found=manifest['spriteEvidence'].get(name,[])
   check(f'Native sprite manifest present: {name}',bool(found) and all(entry['origin'] in all_names for entry in found))

summary={q:sum(c['status']==q for c in checks) for q in ('PASS','FAIL')}
report={'scope':'theme source checks and palette contrast, not visual gameplay verification',
        'source':manifest['snapshotSha256'],'summary':summary,'checks':checks}
(ROOT/'Docs/UI/game-theme-static-checks.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(summary))
for c in checks:
 if c['status']=='FAIL':print('FAIL',c['check'])
sys.exit(1 if summary['FAIL'] else 0)
