# EDNA guide artwork

Source: the user’s original `edna.png` illustration, supplied on 2026-09-07. The source file remains unchanged.

`edna.png` is the transparent half-body cutout (1200 × 1405 RGBA). `edna-avatar.png` is a separately framed, square head-and-shoulders image (512 × 512 RGBA); Unity displays the complete sprite rather than taking a fractional runtime crop.

Following the user’s approval to use the transparent version throughout, local processing removes the connected white background, two enclosed background gaps and empty margins. A narrow antialiased boundary is cleaned before resizing. The drawing, face, glasses, ponytail and clothing are preserved. Both files have real alpha values from 0 to 255 and transparent corners. The earlier image-generation attempts with baked checkerboards are not used.

The reproducible preparation script is `docs/art-experiments/edna/prepare_edna.py`, with the source image and output directory supplied as arguments. Unity imports these sprites at up to 1024 pixels without mipmaps and with alpha transparency enabled. The same transparent portrait is embedded on the right of every dialogue card.
