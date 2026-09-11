# A → B → C → B → A appearance loop

20-second, fixed-camera crossfade preview of the three study images.
Generated from `../renders/natural.png`, `illustrated.png`, and `bathymetry.png`.
The camera and terrain stay fixed. This clip remains the timing reference;
Unity recreates the same blend directly from the textures and applies a shared
terrain mask to remove the flat outer seabed.

- `seamount-loop.mp4`: 1440 × 1008, 30 fps, H.264, 600 frames, silent.
- `seamount-loop.gif`: 640 × 448, 12 fps, embedded infinite-loop setting.
- `checkpoints.png`: decoded MP4 frames showing A, B, C, B, A in order.

| Time | Appearance |
| --- | --- |
| 0–1 s | A |
| 1–5 s | A → B |
| 5–5.5 s | B |
| 5.5–9.5 s | B → C |
| 9.5–10.5 s | C |
| 10.5–14.5 s | C → B |
| 14.5–15 s | B |
| 15–19 s | B → A |
| 19–20 s | A |

Each transition uses cosine easing with zero speed at its endpoints. The A hold
continues across the playback seam. The MP4 is one complete cycle; enable repeat
in its player. The GIF repeats automatically.

Reproduce from the repository root:

```sh
python3 docs/art-experiments/seamount-v2/render_loop.py
```

Validation: decoded checkpoints follow A/B/C/B/A; the encoded first and last MP4
frames have 51.46 dB PSNR (small compression differences). The GIF's NETSCAPE
loop count is zero, meaning infinite repetition.
