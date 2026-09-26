"""Prepare candidate B from the preserved CC0 source (Python + NumPy).

Success criteria: same mono PCM format/duration, no clipping or silent 20 ms
blocks, loop seam below -40 dBFS, and more 80-400 Hz energy relative to 2-8 kHz.
No new pulse train: preserve the recording's natural blade timing.
"""
from pathlib import Path
import hashlib
import wave
import numpy as np

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / "Art/Audio/rotor-aquinn-original.wav"
TARGET = ROOT / "AH64UnityProject/Assets/AH64/Bundle/AH64Audio/sfxAH64RotorHoverGrounded.wav"
SOURCE_HASH = "bd70eb8ae5a472018ab75f3212395826b01e07bbfcb5ebecb29952b943fc6b36"


def read_wave(path):
    with wave.open(str(path), "rb") as stream:
        params = stream.getparams()
        assert params[:4] == (1, 2, 44100, 131418), params
        samples = np.frombuffer(stream.readframes(params.nframes), dtype="<i2") / 32768.0
    return samples


def weight_ratio(samples, frequency):
    power = np.abs(np.fft.rfft(samples)) ** 2
    low = power[(frequency >= 80) & (frequency <= 400)].sum()
    high = power[(frequency >= 2000) & (frequency <= 8000)].sum()
    return 10 * np.log10(low / high)


def main():
    assert hashlib.sha256(SOURCE.read_bytes()).hexdigest() == SOURCE_HASH
    original = read_wave(SOURCE)
    frequency = np.fft.rfftfreq(len(original), 1 / 44100)
    # Broad low shelf adds blade/body weight. Remove inaudible rumble and soften
    # the top end. Circular FFT processing preserves the periodic loop boundary.
    highpass = frequency ** 2 / (frequency ** 2 + 35 ** 2)
    bass = 1 + 1.0 / (1 + (frequency / 400) ** 4)
    treble = 1 / np.sqrt(1 + (frequency / 3500) ** 4)
    result = np.fft.irfft(np.fft.rfft(original) * highpass * bass * treble, n=len(original))
    result *= 10 ** (-1 / 20) / np.max(np.abs(result))
    pcm = np.rint(result * 32767).astype("<i2")
    decoded = pcm.astype(float) / 32768
    peak = 20 * np.log10(np.max(np.abs(decoded)))
    rms = 20 * np.log10(np.sqrt(np.mean(decoded ** 2)))
    seam = abs(decoded[0] - decoded[-1])
    blocks = decoded[:len(decoded) // 882 * 882].reshape(-1, 882)
    minimum_block = 20 * np.log10(np.sqrt(np.mean(blocks ** 2, axis=1)).min())
    boost = weight_ratio(decoded, frequency) - weight_ratio(original, frequency)
    assert -1.01 < peak < -0.99, peak
    assert seam < 0.01, seam
    assert minimum_block > -35, minimum_block
    assert boost > 6, boost
    with wave.open(str(TARGET), "wb") as stream:
        stream.setparams((1, 2, 44100, len(pcm), "NONE", "not compressed"))
        stream.writeframes(pcm.tobytes())
    print(f"PASS: peak={peak:.2f} dBFS; RMS={rms:.2f} dBFS; "
          f"minimum 20ms RMS={minimum_block:.2f} dBFS; seam={seam:.6f}; "
          f"low/high energy ratio increased {boost:.2f} dB")
    print("SHA-256:", hashlib.sha256(TARGET.read_bytes()).hexdigest())


if __name__ == "__main__":
    main()
