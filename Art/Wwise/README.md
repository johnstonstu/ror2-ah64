# AH-64 rotor bank

Open `AH64Audio/AH64Audio.wproj` with Wwise **2023.1.4.8496**. This matches the
installed RoR2 native sound engine, bank format 150. No Unity integration package
or third-party Wwise plug-ins are needed.

Run `powershell -ExecutionPolicy Bypass -File tools/build-rotor-bank.ps1` from the
repository root. This generates, validates and copies **only AH64Rotor.bnk** into
`Build/plugins/SoundBanks/`. `tools/pack.ps1` requires a fresh validated bank.
Generated output is ignored. Never ship or load the generated Init.bnk: runtime
uses the game's existing SFX_BUS (ID 213475909).

The bank embeds one mono PCM loop and two events, Play_AH64_Rotor and
Stop_AH64_Rotor. The runtime stops its own playing ID. Parameters control pitch
in cents, low-pass and spatial mix. Local pilots hear a 2D bed; remote aircraft
use 3D panning and a C# distance fade. Native attenuation is disabled to avoid
double attenuation. Master/SFX volume and pause remain owned by the game.

The source is the aquinn CC0 Helicopter Sounds recording, prepared in
`Art/Audio/` using `tools/prepare_rotor.py`. Full provenance is preserved in
`Art/Audio/README.md` and the Unity audio folder's LICENSE_SOURCE.txt. The imported
Wwise WAV is a copy of that prepared loop; update it when changing the source.

`tools/author_rotor_bank.py` is initial setup history, not the rebuild command.
It deliberately rejects an already-authored project.

In-game acceptance: bank load succeeds, playback position advances/wraps,
Rotor volume zero removes the loop and restoring it restores that sound.
Then test game volume, pause/focus, death/respawn, stage transitions and multiplayer.
Use `ah64_audio_status` in the console to capture playing ID, position and mix.
