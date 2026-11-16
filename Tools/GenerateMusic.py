#!/usr/bin/env python3
"""
Generates the game's background music: a gentle, slightly sad 8-bit style loop in A minor (original tune, written for this project).

Standard library only (no numpy). Deterministic: re-running gives the identical file.

    python Tools/GenerateMusic.py                 # writes Assets/Audio/Music/bgm_loop.wav
    python Tools/GenerateMusic.py --out some/dir

Structure: 16 bars of 4/4 at 88 BPM (about 44 s), A natural minor with a harmonic-minor turn (E major, G#) near the end.
Progression: Am - Am - F - F - C - C - G - G, then Am - Am - F - F - Dm - E - Am - Am.
Instruments: soft triangle lead with a slow vibrato (long, falling phrases), triangle bass, a quiet sustained sine pad
and a very quiet pulse arpeggio. No drums: it should sit in the background.
The loop is seamless: the last bar fades out completely, so the end flows into the start.
Edit MELODY, CHORDS or BPM below and re-run to change the tune. Playback volume is set in Unity (AudioManager).
"""
import argparse
import math
import os
import struct
import wave

SAMPLE_RATE = 44100
BPM = 88
STEPS_PER_BAR = 8            # eighth notes
BARS = 16
TARGET_PEAK = 0.55           # music stays well below the sound effects; Unity volume lowers it further
FADE_SECONDS = 0.004

# ----------------------------------------------------------------------------- notes

NOTE_INDEX = {"C": 0, "C#": 1, "D": 2, "D#": 3, "E": 4, "F": 5, "F#": 6, "G": 7, "G#": 8, "A": 9, "A#": 10, "B": 11}


def hz(name):
    """'A4' -> 440.0 Hz."""
    pitch, octave = name[:-1], int(name[-1])
    midi = 12 * (octave + 1) + NOTE_INDEX[pitch]
    return 440.0 * 2 ** ((midi - 69) / 12)


# Chord: (bass root, triad for the pad and the arpeggio)
CHORD_DEFS = {
    "Am": ("A2", ("A3", "C4", "E4")),
    "F":  ("F2", ("F3", "A3", "C4")),
    "C":  ("C3", ("C4", "E4", "G4")),
    "G":  ("G2", ("G3", "B3", "D4")),
    "Dm": ("D3", ("D4", "F4", "A4")),
    "E":  ("E2", ("E3", "G#3", "B3")),
}
CHORDS = ["Am", "Am", "F", "F", "C", "C", "G", "G",
          "Am", "Am", "F", "F", "Dm", "E", "Am", "Am"]

# Lead melody per bar: (start step, note, length in steps). Long notes, falling lines: a quiet, wistful tune.
MELODY = [
    [(0, "E5", 3), (3, "D5", 1), (4, "C5", 2), (6, "B4", 2)],                       # 1  Am
    [(0, "A4", 4), (4, "C5", 2), (6, "E5", 2)],                                     # 2  Am
    [(0, "F5", 3), (3, "E5", 1), (4, "D5", 2), (6, "C5", 2)],                       # 3  F
    [(0, "A4", 4), (4, "C5", 4)],                                                   # 4  F
    [(0, "G5", 3), (3, "E5", 1), (4, "G5", 2), (6, "E5", 2)],                       # 5  C
    [(0, "D5", 2), (2, "C5", 2), (4, "E5", 4)],                                     # 6  C
    [(0, "D5", 3), (3, "B4", 1), (4, "G4", 2), (6, "B4", 2)],                       # 7  G
    [(0, "D5", 4), (4, "G4", 4)],                                                   # 8  G
    [(0, "E5", 3), (3, "D5", 1), (4, "C5", 2), (6, "B4", 2)],                       # 9  Am
    [(0, "A4", 4), (4, "C5", 2), (6, "E5", 2)],                                     # 10 Am
    [(0, "F5", 3), (3, "E5", 1), (4, "D5", 2), (6, "C5", 2)],                       # 11 F
    [(0, "A4", 4), (4, "C5", 4)],                                                   # 12 F
    [(0, "F5", 3), (3, "E5", 1), (4, "D5", 2), (6, "F5", 2)],                       # 13 Dm
    [(0, "E5", 2), (2, "D5", 1), (3, "B4", 1), (4, "G#4", 4)],                      # 14 E (the harmonic-minor turn)
    [(0, "A4", 3), (3, "C5", 1), (4, "E5", 4)],                                     # 15 Am
    [(0, "A4", 5)],                                                                 # 16 Am (rings out, then silence)
]


# ----------------------------------------------------------------------------- voices

def step_seconds():
    return 60.0 / (BPM * 2)


def triangle(phase):
    p = phase % 1.0
    return 4 * p - 1 if p < 0.5 else 3 - 4 * p


def pulse(phase, duty):
    return 1.0 if (phase % 1.0) < duty else -1.0


def add_note(buf, start_s, length_s, freq, kind, volume, attack_s=0.01, release_s=0.08, vibrato=0.0):
    start = int(start_s * SAMPLE_RATE)
    n = int((length_s + release_s) * SAMPLE_RATE)
    phase = 0.0
    for i in range(n):
        j = start + i
        if j >= len(buf):
            break
        t = i / SAMPLE_RATE
        f = freq * (1 + vibrato * math.sin(2 * math.pi * 4.5 * t)) if (vibrato and t > 0.25) else freq
        phase += f / SAMPLE_RATE
        if kind == "lead":      # soft, round: triangle plus a little octave sine
            s = 0.85 * triangle(phase) + 0.15 * math.sin(4 * math.pi * phase)
        elif kind == "arp":     # thin, quiet 12.5 % pulse
            s = pulse(phase, 0.125)
        elif kind == "pad":
            s = math.sin(2 * math.pi * phase)
        else:                   # bass
            s = triangle(phase)
        attack = min(1.0, t / attack_s)
        if t < length_s:
            env = attack * (0.8 + 0.2 * math.exp(-2.5 * t)) if kind in ("lead", "bass") else attack
        else:
            env = max(0.0, 1.0 - (t - length_s) / release_s) * (0.8 if kind in ("lead", "bass") else 1.0)
        buf[j] += s * env * volume


# ----------------------------------------------------------------------------- render

def render():
    step = step_seconds()
    bar_len = STEPS_PER_BAR * step
    buf = [0.0] * int(BARS * bar_len * SAMPLE_RATE)

    for bar in range(BARS):
        bar_start = bar * bar_len
        last = bar == BARS - 1
        bass_note, triad = CHORD_DEFS[CHORDS[bar]]
        root, fifth = hz(bass_note), hz(bass_note) * 1.5

        # sustained pad: the whole chord, slow attack, fades before the bar ends (the last bar fades earlier so the loop is silent)
        pad_len = bar_len * (0.55 if last else 0.96)
        for name in triad:
            add_note(buf, bar_start, pad_len, hz(name), "pad", 0.06, attack_s=0.35, release_s=0.6 if not last else 1.2)
        # bass: a long root, then the fifth halfway through the bar
        add_note(buf, bar_start, step * 3.6, root, "bass", 0.45, attack_s=0.02, release_s=0.15)
        if not last:
            add_note(buf, bar_start + 4 * step, step * 3.4, fifth, "bass", 0.35, attack_s=0.02, release_s=0.15)
        # very quiet arpeggio on every eighth: root, third, fifth, third ...
        if not last:
            pattern = (0, 1, 2, 1, 0, 1, 2, 1)
            for s in range(STEPS_PER_BAR):
                add_note(buf, bar_start + s * step, step * 0.7, hz(triad[pattern[s]]) * 2, "arp", 0.035, attack_s=0.004, release_s=0.06)
        # lead melody
        for s, name, length in MELODY[bar]:
            add_note(buf, bar_start + s * step, length * step * 0.95, hz(name), "lead", 0.34, attack_s=0.03, release_s=0.18, vibrato=0.006)
    return buf


def finish(samples):
    mean = sum(samples) / len(samples)
    samples = [s - mean for s in samples]
    fade = int(FADE_SECONDS * SAMPLE_RATE)
    n = len(samples)
    for i in range(fade):
        samples[i] *= i / fade
        samples[n - 1 - i] *= i / fade
    peak = max(abs(s) for s in samples) or 1.0
    return [s * TARGET_PEAK / peak for s in samples]


def main():
    default_out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Audio", "Music")
    parser = argparse.ArgumentParser(description="Generate the background music loop.")
    parser.add_argument("--out", default=default_out, help="output folder (default: Assets/Audio/Music)")
    args = parser.parse_args()
    out_dir = os.path.abspath(args.out)
    os.makedirs(out_dir, exist_ok=True)

    samples = finish(render())
    path = os.path.join(out_dir, "bgm_loop.wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SAMPLE_RATE)
        w.writeframes(b"".join(struct.pack("<h", int(round(max(-1.0, min(1.0, s)) * 32767))) for s in samples))

    rms = math.sqrt(sum(s * s for s in samples) / len(samples))
    print(f"Wrote {path}")
    print(f"{len(samples) / SAMPLE_RATE:.2f} s, {BARS} bars at {BPM} BPM, peak {20 * math.log10(max(abs(s) for s in samples)):.1f} dBFS, rms {20 * math.log10(rms):.1f} dBFS")


if __name__ == "__main__":
    main()
