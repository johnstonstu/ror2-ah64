# Rotor source

`rotor-aquinn-original.wav` preserves the exact pre-candidate-B loop so audio
preparation is repeatable and does not compound EQ on an already edited WAV.

- Creator: aquinn; title: Helicopter Sounds; CC0.
- Source: https://opengameart.org/content/helicopter-sounds
- Prepared loop: https://github.com/johnstonstu/Dust-Front/blob/main/assets/audio/rotor_loop.wav
- Original SHA-256: `BD70EB8AE5A472018AB75F3212395826B01E07BBFCB5EBECB29952B943FC6B36`

Run `python tools/prepare_rotor.py` with NumPy installed to recreate the heavier
candidate in Unity's bundle folder. The script verifies signal levels and seam
before writing. This source is outside Unity and never ships in the bundle.
Full provenance remains in the bundle's `AH64Audio/LICENSE_SOURCE.txt`.
