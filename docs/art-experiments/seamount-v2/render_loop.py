"""Create the requested A -> B -> C -> B -> A loop from the aligned renders.

Uses FFmpeg's runtime blend opacity commands, with cosine easing computed once
per frame. The geometry and camera stay fixed; this is an appearance preview.
Run with ordinary Python 3: python3 render_loop.py
"""
from pathlib import Path
import math
import shutil
import subprocess

ROOT = Path(__file__).resolve().parent
OUT = ROOT / 'motion'
FPS = 30
SECONDS = 20
WIDTH, HEIGHT = 1440, 1008


def ease(t):
    t = max(0.0, min(1.0, t))
    return (1.0 - math.cos(math.pi * t)) * .5


def weights(t):
    # B is fully present while the C overlay fades in and out.
    b = ease((t - 1) / 4) * (1 - ease((t - 15) / 4))
    c = ease((t - 5.5) / 4) * (1 - ease((t - 10.5) / 4))
    return b, c


def main():
    ffmpeg = shutil.which('ffmpeg')
    if not ffmpeg:
        raise RuntimeError('FFmpeg is required')
    OUT.mkdir(exist_ok=True)
    paths = [ROOT / 'renders' / (name + '.png') for name in ('natural', 'illustrated', 'bathymetry')]
    for path in paths:
        if not path.is_file():
            raise FileNotFoundError(path)
    commands = OUT / 'blend-commands.txt'
    with commands.open('w') as f:
        for n in range(FPS * SECONDS):
            b, c = weights(n / FPS)
            # Slightly before the frame timestamp avoids microsecond rounding.
            timestamp = max(0.0, (n - .1) / FPS)
            f.write(f'{timestamp:.9f} blend@ab all_opacity {b:.9f};\n')
            f.write(f'{timestamp:.9f} blend@bc all_opacity {c:.9f};\n')

    filters = (
        f'[0:v]scale={WIDTH}:{HEIGHT}:flags=lanczos,format=gbrp,'
        'sendcmd=f=blend-commands.txt[a];'
        f'[1:v]scale={WIDTH}:{HEIGHT}:flags=lanczos,format=gbrp[b];'
        f'[2:v]scale={WIDTH}:{HEIGHT}:flags=lanczos,format=gbrp[c];'
        '[b][a]blend@ab=all_mode=normal:all_opacity=0[ab];'
        '[c][ab]blend@bc=all_mode=normal:all_opacity=0,format=yuv420p[out]'
    )
    args = [ffmpeg, '-y', '-hide_banner', '-loglevel', 'error', '-filter_complex_threads', '4']
    for path in paths:
        args += ['-loop', '1', '-framerate', str(FPS), '-i', str(path)]
    args += ['-filter_complex', filters, '-map', '[out]', '-frames:v', str(FPS * SECONDS),
             '-an', '-c:v', 'libx264', '-preset', 'medium', '-crf', '17', '-threads', '8',
             '-pix_fmt', 'yuv420p', '-movflags', '+faststart', '-progress', 'encode-progress.txt',
             'seamount-loop.mp4']
    print('Encoding 20-second eased A-B-C-B-A video...', flush=True)
    subprocess.run(args, cwd=OUT, check=True)
    print('Encoding an automatically repeating preview...', flush=True)
    subprocess.run([
        ffmpeg, '-y', '-hide_banner', '-loglevel', 'error', '-i', 'seamount-loop.mp4',
        '-filter_complex',
        '[0:v]fps=12,scale=640:448:flags=lanczos,split[a][b];'
        '[a]palettegen=stats_mode=full:reserve_transparent=0:max_colors=192[p];'
        '[b][p]paletteuse=dither=bayer:bayer_scale=4',
        '-loop', '0', 'seamount-loop.gif'
    ], cwd=OUT, check=True)
    print('Saved:', OUT / 'seamount-loop.mp4', flush=True)
    print('Saved:', OUT / 'seamount-loop.gif', flush=True)


if __name__ == '__main__':
    main()
