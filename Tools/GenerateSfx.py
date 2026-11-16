#!/usr/bin/env python3
"""
Generates the game's sound effects as small mono 16-bit 44.1 kHz WAV files.

Standard library only (no numpy). Deterministic: re-running gives identical files.

    python Tools/GenerateSfx.py                 # writes to Assets/Audio/Sfx
    python Tools/GenerateSfx.py --out some/dir  # writes somewhere else

Every clip is shorter than 1 second and is peak-normalized to its own target (see PEAKS),
all below -1 dBFS, so nothing clips. The targets are also how the clips are balanced against
each other: frequent sounds (projectile fire) are quieter than rare ones (boss spawn).
Edit a recipe or a peak target and re-run to change a sound; Unity re-imports the file on focus.
"""
import argparse
import math
import os
import random
import struct
import wave

SAMPLE_RATE = 44100
MAX_PEAK = 0.89          # about -1 dBFS: hard ceiling for any clip
FADE_IN_SECONDS = 0.002  # avoids a click at the start
FADE_OUT_SECONDS = 0.010 # avoids a click at the end

# Target peak (0..1) per clip. This is the loudness balance between the sounds.
PEAKS = {
    "jump": 0.60,
    "projectile_fire": 0.45,
    "hit": 0.60,
    "crystal_pickup": 0.70,
    "chest_open": 0.55,
    "lose": 0.80,
    "win": 0.80,
    "boss_spawn": 0.85,
    "ui_click": 0.50,
}


# ----------------------------------------------------------------------------- helpers

def count(seconds):
    return int(seconds * SAMPLE_RATE)


def exp_decay(t, rate):
    return math.exp(-rate * t)


def lowpassed_noise(rng, samples, coefficient):
    """White noise through a one-pole low-pass. Smaller coefficient = darker."""
    out, y = [], 0.0
    for _ in range(samples):
        y += coefficient * (rng.uniform(-1.0, 1.0) - y)
        out.append(y)
    return out


def tone(samples, freq_at, harmonics=((1, 1.0),)):
    """Phase-accumulating tone. freq_at(t) -> Hz. harmonics: (multiple, amplitude) pairs."""
    out, phase = [], 0.0
    for i in range(samples):
        t = i / SAMPLE_RATE
        phase += 2.0 * math.pi * freq_at(t) / SAMPLE_RATE
        out.append(sum(a * math.sin(phase * m) for m, a in harmonics))
    return out


def mix_into(dst, src, start_seconds=0.0, gain=1.0):
    offset = count(start_seconds)
    for i, v in enumerate(src):
        j = offset + i
        if j < len(dst):
            dst[j] += v * gain


def note(duration, freq, decay, harmonics=((1, 1.0), (2, 0.3), (3, 0.12)), attack=0.004):
    n = count(duration)
    raw = tone(n, lambda t: freq, harmonics)
    return [v * min(1.0, (i / SAMPLE_RATE) / attack) * exp_decay(i / SAMPLE_RATE, decay) for i, v in enumerate(raw)]


# ----------------------------------------------------------------------------- recipes

def make_jump(rng):
    dur = 0.30
    n = count(dur)
    s = tone(n, lambda t: 300 + 420 * (t / dur), ((1, 1.0), (2, 0.25)))
    return [v * (1 - i / n) ** 1.5 * min(1.0, i / (0.01 * SAMPLE_RATE)) for i, v in enumerate(s)]


def make_projectile_fire(rng):
    n = count(0.16)
    noise = lowpassed_noise(rng, n, 0.45)
    chirp = tone(n, lambda t: 1100 - 4800 * t, ((1, 1.0), (2, 0.2)))
    return [0.6 * noise[i] * exp_decay(i / SAMPLE_RATE, 40) + 0.5 * chirp[i] * exp_decay(i / SAMPLE_RATE, 18) for i in range(n)]


def make_hit(rng):
    n = count(0.24)
    thump = tone(n, lambda t: 55 + 115 * math.exp(-25 * t))
    noise = lowpassed_noise(rng, n, 0.35)
    return [thump[i] * exp_decay(i / SAMPLE_RATE, 14) + 0.6 * noise[i] * exp_decay(i / SAMPLE_RATE, 35) for i in range(n)]


def make_crystal_pickup(rng):
    out = [0.0] * count(0.55)
    mix_into(out, note(0.5, 1318.5, 7.0, ((1, 1.0), (2, 0.35), (3, 0.15))), 0.0, 0.8)    # E6
    mix_into(out, note(0.45, 1975.5, 7.0, ((1, 1.0), (2, 0.35), (3, 0.15))), 0.09, 1.0)  # B6
    return out


def make_chest_open(rng):
    out = [0.0] * count(0.95)
    # wooden creak: a slowly rising, wobbling, softened saw
    n = count(0.55)
    phase, creak, y = 0.0, [], 0.0
    for i in range(n):
        t = i / SAMPLE_RATE
        freq = 140 + 90 * (t / 0.55) + 6 * math.sin(2 * math.pi * 18 * t)
        phase = (phase + freq / SAMPLE_RATE) % 1.0
        y += 0.12 * ((2 * phase - 1) - y)                      # soften the saw
        creak.append(y * math.sin(math.pi * i / n) ** 0.7)     # swell in and out
    mix_into(out, creak, 0.0, 1.0)
    # sparkle as the lid opens
    for start, freq in ((0.50, 1568.0), (0.62, 2093.0), (0.74, 2637.0)):
        mix_into(out, note(0.2, freq, 12.0, ((1, 1.0), (2, 0.2))), start, 0.45)
    return out


def make_lose(rng):
    out = [0.0] * count(0.95)
    for start, freq, decay, length in ((0.00, 392.0, 6.0, 0.4), (0.24, 311.1, 6.0, 0.4), (0.48, 233.1, 3.5, 0.47)):
        n = count(length)
        raw = tone(n, lambda t, f=freq: f * (1 + 0.006 * math.sin(2 * math.pi * 5 * t)), ((1, 1.0), (2, 0.4), (3, 0.2)))
        mix_into(out, [v * exp_decay(i / SAMPLE_RATE, decay) * min(1.0, i / (0.01 * SAMPLE_RATE)) for i, v in enumerate(raw)], start, 0.8)
    return out


def make_win(rng):
    out = [0.0] * count(0.95)
    harmonics = ((1, 1.0), (2, 0.25), (3, 0.12))
    for start, freq in ((0.00, 523.25), (0.10, 659.25), (0.20, 783.99), (0.30, 1046.5)):
        mix_into(out, note(0.25, freq, 9.0, harmonics), start, 0.7)
    for freq in (523.25, 659.25, 783.99, 1046.5):  # final chord
        mix_into(out, note(0.53, freq, 4.0, harmonics), 0.42, 0.45)
    return out


def make_boss_spawn(rng):
    n = count(0.95)
    swell = [min(1.0, (i / SAMPLE_RATE) / 0.25) * exp_decay(max(0.0, i / SAMPLE_RATE - 0.25), 1.6) for i in range(n)]
    sub = tone(n, lambda t: 52)
    drone_a = tone(n, lambda t: 98.0, ((1, 1.0), (2, 0.4)))
    drone_b = tone(n, lambda t: 103.8, ((1, 1.0), (2, 0.4)))
    growl = tone(n, lambda t: 220 - 300 * t if t < 0.5 else 70, ((1, 1.0), (2, 0.5)))
    rumble = lowpassed_noise(rng, n, 0.05)
    out = []
    for i in range(n):
        t = i / SAMPLE_RATE
        growl_env = min(1.0, t / 0.05) * exp_decay(t, 3.0)
        out.append(swell[i] * (0.9 * sub[i] + 0.35 * (drone_a[i] + drone_b[i]) + 4.0 * rumble[i]) + 0.35 * growl[i] * growl_env)
    return out


def make_ui_click(rng):
    n = count(0.06)
    s = tone(n, lambda t: 1900)
    noise = lowpassed_noise(rng, n, 0.6)
    return [s[i] * exp_decay(i / SAMPLE_RATE, 90) + 0.3 * noise[i] * exp_decay(i / SAMPLE_RATE, 300) for i in range(n)]


RECIPES = {
    "jump": make_jump,
    "projectile_fire": make_projectile_fire,
    "hit": make_hit,
    "crystal_pickup": make_crystal_pickup,
    "chest_open": make_chest_open,
    "lose": make_lose,
    "win": make_win,
    "boss_spawn": make_boss_spawn,
    "ui_click": make_ui_click,
}


# ----------------------------------------------------------------------------- output

def finish(samples, target_peak):
    """Remove DC, fade the edges, normalize to target_peak (never above MAX_PEAK)."""
    mean = sum(samples) / len(samples)
    samples = [s - mean for s in samples]
    fade_in, fade_out = count(FADE_IN_SECONDS), count(FADE_OUT_SECONDS)
    n = len(samples)
    for i in range(min(fade_in, n)):
        samples[i] *= i / fade_in
    for i in range(min(fade_out, n)):
        samples[n - 1 - i] *= i / fade_out
    peak = max(abs(s) for s in samples) or 1.0
    gain = min(target_peak, MAX_PEAK) / peak
    return [s * gain for s in samples]


def write_wav(path, samples):
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SAMPLE_RATE)
        w.writeframes(b"".join(struct.pack("<h", int(round(max(-1.0, min(1.0, s)) * 32767))) for s in samples))


def dbfs(x):
    return 20 * math.log10(x) if x > 0 else float("-inf")


def main():
    default_out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Audio", "Sfx")
    parser = argparse.ArgumentParser(description="Generate the game's sound effects.")
    parser.add_argument("--out", default=default_out, help="output folder (default: Assets/Audio/Sfx)")
    args = parser.parse_args()
    out_dir = os.path.abspath(args.out)
    os.makedirs(out_dir, exist_ok=True)

    print(f"Writing to {out_dir}")
    print(f"{'clip':<18}{'length':>8}{'peak':>10}{'rms':>10}")
    for name, recipe in RECIPES.items():
        rng = random.Random(name)  # seeded by name: same file every run
        samples = finish(recipe(rng), PEAKS[name])
        write_wav(os.path.join(out_dir, name + ".wav"), samples)
        rms = math.sqrt(sum(s * s for s in samples) / len(samples))
        peak = max(abs(s) for s in samples)
        print(f"{name:<18}{len(samples) / SAMPLE_RATE:>7.2f}s{dbfs(peak):>8.1f} dB{dbfs(rms):>8.1f} dB")


if __name__ == "__main__":
    main()
