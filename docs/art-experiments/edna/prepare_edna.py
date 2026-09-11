from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
import numpy as np
import sys
if len(sys.argv) != 3:
    raise SystemExit('Usage: prepare_edna.py ORIGINAL.png OUTPUT_DIRECTORY')
src=Path(sys.argv[1])
out=Path(sys.argv[2])
out.mkdir(parents=True, exist_ok=True)
im=Image.open(src).convert('RGB')
if im.size != (2480, 3508):
    raise SystemExit('This crop is authored for the supplied 2480 x 3508 illustration.')
rgb=np.asarray(im).astype(np.float32)
white=(rgb.min(axis=2)>=244)&((rgb.max(axis=2)-rgb.min(axis=2))<14)
mask=Image.fromarray((white*255).astype('uint8')).copy()
for xy in [(0,0),(im.width-1,0),(0,im.height-1),(im.width-1,im.height-1)]:
 if mask.getpixel(xy)==255: ImageDraw.floodfill(mask,xy,128)
bg=np.asarray(mask)==128
# Two enclosed background gaps in the supplied drawing, outside the face.
bg[1320:1450,748:798] |= white[1320:1450,748:798]
bg[2910:3400,2415:2460] |= white[2910:3400,2415:2460]
expanded=np.asarray(Image.fromarray((bg*255).astype('uint8')).filter(ImageFilter.MaxFilter(5)))>0
alpha=np.ones(rgb.shape[:2],np.float32)
alpha[bg]=0
edge=expanded&~bg
alpha[edge]=np.clip((255-rgb.min(axis=2)[edge])/25,0,1)
# Remove white contamination only in the narrow antialiased boundary.
partial=(alpha>0)&(alpha<1)
rgb[partial]=np.clip((rgb[partial]-255*(1-alpha[partial,None]))/alpha[partial,None],0,255)
rgb[alpha==0]=0
rgba=Image.fromarray(np.dstack([rgb.astype('uint8'),(alpha*255).astype('uint8')]),'RGBA')
def padded_crop(image,padding):
 box=image.getbbox();image=image.crop(box)
 result=Image.new('RGBA',(image.width+2*padding,image.height+2*padding))
 result.alpha_composite(image,(padding,padding));return result
portrait=padded_crop(rgba.crop((0,0,im.width,3150)),24)
portrait.thumbnail((1200,1536),Image.Resampling.LANCZOS)
portrait.save(out/'edna.png')
head=rgba.crop((605,670,1930,2250))
head=padded_crop(head,48)
side=max(head.size)
avatar=Image.new('RGBA',(side,side))
avatar.alpha_composite(head,((side-head.width)//2,(side-head.height)//2))
avatar=avatar.resize((512,512),Image.Resampling.LANCZOS)
avatar.save(out/'edna-avatar.png')
print('Portrait:',portrait.size,'Avatar:',avatar.size,'transparent source background:',int(bg.sum()))
print('Preserved opaque original pixels:',int((alpha==1).sum()))
