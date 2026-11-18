"""Synthesises the drill's sound effects (no third-party audio): fire-alarm bell, CO2 and water extinguisher
spray, breaker clack -> Assets/Art/Audio/*.wav (44.1 kHz mono 16-bit). Loops are seamless (whole cycles / crossfade)."""
import os
import wave

import numpy as np

SR = 44100
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Art", "Audio")
rng = np.random.default_rng(7)


def save(name, x, peak=0.85):
    x = x / (np.max(np.abs(x)) + 1e-9) * peak
    data = (x * 32767).astype(np.int16)
    with wave.open(os.path.join(OUT, name), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())
    print(name, f"{len(x) / SR:.2f}s")


def lowpass(x, cutoff):
    """One-pole low-pass, run twice (12 dB/oct)."""
    a = np.exp(-2 * np.pi * cutoff / SR)
    for _ in range(2):
        y = np.empty_like(x)
        acc = 0.0
        for i, v in enumerate(x):
            acc = (1 - a) * v + a * acc
            y[i] = acc
        x = y
    return x


def loop_crossfade(x, fade):
    """Makes a seamless loop: the tail is faded into the head."""
    n = int(fade * SR)
    head, body, tail = x[:n], x[n:-n], x[-n:]
    ramp = np.linspace(0, 1, n)
    return np.concatenate([body, tail * (1 - ramp) + head * ramp])


def alarm_bell(seconds=2.0):
    """Electric fire bell: a hammer striking a gong ~20 times a second (the classic 'reng reng' of school bells)."""
    n = int(seconds * SR)
    t = np.arange(n) / SR
    strike_rate = 20.0  # 40 strikes over 2 s -> whole number of cycles, loops cleanly
    phase = (t * strike_rate) % 1.0
    env = np.exp(-phase / 0.018) * 0.9 + 0.1  # each strike decays, the gong keeps ringing a little
    f0 = 1180.0
    partials = [(1.0, 1.0), (2.76, 0.55), (5.40, 0.3), (8.93, 0.15)]  # bell-like inharmonic partials
    tone = sum(a * np.sin(2 * np.pi * f0 * r * t) for r, a in partials)
    click = (rng.standard_normal(n) * np.exp(-phase / 0.002)) * 0.25
    return tone * env + click


def spray(seconds, cutoff, rumble, crackle):
    n = int((seconds + 0.3) * SR)
    noise = rng.standard_normal(n)
    hiss = noise - lowpass(noise, cutoff)  # high-passed noise: the jet
    body = lowpass(rng.standard_normal(n), rumble) * 3.0  # low roar of the discharge
    x = hiss * 0.7 + body * 0.5
    if crackle:  # CO2: the snow / dry-ice crackle in the horn
        pops = (rng.random(n) < 0.0015) * rng.standard_normal(n) * 6.0
        x += lowpass(pops, 3000.0)
    return loop_crossfade(x, 0.3)


def clack():
    n = int(0.35 * SR)
    t = np.arange(n) / SR
    thump = np.sin(2 * np.pi * 140 * t) * np.exp(-t / 0.03)
    snap = rng.standard_normal(n) * np.exp(-t / 0.006)
    ring = np.sin(2 * np.pi * 2300 * t) * np.exp(-t / 0.05) * 0.2
    second = np.zeros(n)
    k = int(0.045 * SR)  # the contacts settle a moment later
    second[k:] = (rng.standard_normal(n - k) * np.exp(-t[: n - k] / 0.004)) * 0.5
    return thump + snap + ring + second


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    save("EF_FireAlarmBell.wav", alarm_bell())
    save("EF_CO2_Spray.wav", spray(2.0, 2500.0, 180.0, True))
    save("EF_Water_Spray.wav", spray(2.0, 900.0, 300.0, False), peak=0.7)
    save("EF_Breaker_Clack.wav", clack())
