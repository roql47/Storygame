# -*- coding: utf-8 -*-
# Auto part-splitter: SAM (hair/face) + anatomical geometric slicing (occluded limbs).
import os, numpy as np
from PIL import Image
import torch
from segment_anything import sam_model_registry, SamAutomaticMaskGenerator

SRC   = r"D:\Mygame\RawAssets\scenario\witch\witch_wai_00002_cutout.png"
OUT   = r"D:\Mygame\RawAssets\scenario\witch\parts"
CKPT  = r"D:\PIXEL_pipeline\sam_models\sam_vit_b_01ec64.pth"
os.makedirs(OUT, exist_ok=True)

rgba = np.array(Image.open(SRC).convert("RGBA"))
H, W = rgba.shape[:2]
rgb  = rgba[..., :3].astype(int)
R, G, B = rgb[..., 0], rgb[..., 1], rgb[..., 2]
alpha = rgba[..., 3]
fg = alpha > 16
ys, xs = np.where(fg)
y0, y1, x0, x1 = ys.min(), ys.max(), xs.min(), xs.max()
bh, bw, cx = (y1 - y0 + 1), (x1 - x0 + 1), (x0 + x1) / 2.0
print("canvas %dx%d  fg bbox x[%d-%d] y[%d-%d]" % (W, H, x0, x1, y0, y1))

# ---- part codes ----
HEAD, HAIR, TORSO, ARM_L, ARM_R, LEG_L, LEG_R = range(7)
NAMES = {HEAD:"head", HAIR:"hair", TORSO:"torso",
         ARM_L:"arm_left", ARM_R:"arm_right", LEG_L:"leg_left", LEG_R:"leg_right"}
KO    = {HEAD:"머리", HAIR:"머리카락", TORSO:"몸통",
         ARM_L:"왼팔", ARM_R:"오른팔", LEG_L:"왼다리", LEG_R:"오른다리"}

# ---- geometric anatomical zones (viewer-oriented L/R) ----
Y, X = np.mgrid[0:H, 0:W]
ry = (Y - y0) / bh
central = np.abs(X - cx) < 0.18 * bw
left = X < cx
code = np.full((H, W), -1, np.int16)
head_band = ry < 0.20
mid_band  = (ry >= 0.20) & (ry < 0.55)
leg_band  = ry >= 0.55
code[head_band] = HEAD
code[mid_band & central] = TORSO
code[mid_band & ~central & left]  = ARM_L
code[mid_band & ~central & ~left] = ARM_R
code[leg_band & left]  = LEG_L
code[leg_band & ~left] = LEG_R

# ---- color masks ----
# pink hair: R high, B close to G (pink), not strongly green-dominant skin
hair_col = (R > 175) & (B > 145) & (np.abs(G - B) < 40) & ((G - B) <= 22) & (R >= G - 12)
# peach skin: green clearly above blue
skin_col = (R > 195) & (G > 150) & ((G - B) > 16) & (R >= G)
code[fg & hair_col] = HAIR           # hair wins globally (long flowing hair)
code[head_band & fg & skin_col] = HEAD

# ---- SAM: refine hair & face boundaries (the genuinely separable parts) ----
device = "cuda" if torch.cuda.is_available() else "cpu"
print("SAM on", device)
sam = sam_model_registry["vit_b"](checkpoint=CKPT).to(device)
gen = SamAutomaticMaskGenerator(sam, points_per_side=24, pred_iou_thresh=0.86,
                                stability_score_thresh=0.90, min_mask_region_area=0)
masks = gen.generate(rgba[..., :3])
print("SAM masks:", len(masks))

for m in sorted(masks, key=lambda d: -d["area"]):
    seg = m["segmentation"] & fg
    n = int(seg.sum())
    if n < 400:
        continue
    frac_hair = (hair_col[seg]).mean()
    frac_skin = (skin_col[seg]).mean()
    cyr = (np.where(seg)[0].mean() - y0) / bh   # centroid row ratio
    # a segment that's mostly hair-colored -> hair (clean strands)
    if frac_hair > 0.5:
        code[seg] = HAIR
    # a skin segment up in the head area -> head/face
    elif frac_skin > 0.45 and cyr < 0.30:
        code[seg] = HEAD

code[~fg] = -1

# ---- save 7 aligned full-canvas layers + preview ----
PALETTE = {HEAD:(255,170,120), HAIR:(255,105,180), TORSO:(80,160,255),
           ARM_L:(120,220,120), ARM_R:(60,170,60), LEG_L:(230,210,90), LEG_R:(200,160,40)}
preview = np.zeros((H, W, 4), np.uint8)
counts = {}
for c in range(7):
    sel = (code == c) & fg
    counts[c] = int(sel.sum())
    layer = np.zeros((H, W, 4), np.uint8)
    layer[sel] = rgba[sel]                       # keep original pixels+alpha
    Image.fromarray(layer, "RGBA").save(os.path.join(OUT, "witch_%s.png" % NAMES[c]))
    pr, pg, pb = PALETTE[c]
    preview[sel] = (pr, pg, pb, 255)
Image.fromarray(preview, "RGBA").save(os.path.join(OUT, "_preview_labels.png"))

print("\n=== parts saved to", OUT, "===")
for c in range(7):
    print("  witch_%-10s (%s): %d px" % (NAMES[c], KO[c], counts[c]))
