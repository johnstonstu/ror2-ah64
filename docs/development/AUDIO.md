# Rotor audio

AH-64 1.1 uses Wwise for its custom rotor. Recording provenance is in
[Art/Audio](../../Art/Audio/README.md); bank instructions are in
[Art/Wwise](../../Art/Wwise/README.md).

## Why Wwise is required

Inspection of the installed game's `Risk of Rain 2_Data/globalgamemanagers` on
2026-09-26 found `AudioManager.m_DisableAudio = true`. Unity AudioClips can load
and listeners can exist while AudioSource playback remains silent. An assetbundle
does not override the host settings. Do not patch the game or restore that backend.

The verified native runtime is Wwise 2023.1.4.8496, bank format 150. Build
`AH64Rotor.bnk` with that version. Two events and one embedded PCM recording resolve
the game's existing SFX bus. Never ship the author's `Init.bnk`.

## Runtime ownership

Each aircraft owns one emitter and tracks its playing ID. Disable, death and
destruction stop that event. Failed bank loads/event starts have bounded retries.
The local pilot hears a 2D bed; remote aircraft receive spatial panning and distance
fading. Master/SFX volume, pause and focus mute are owned by the game.

Direction, filtered motion and ability state feed smoothed pitch/gain envelopes.
Rotor volume is the only public Audio slider. Existing development config keys
remain bound so upgrades preserve saved tuning. Fresh defaults and a previously
tuned profile can sound different; do not silently reset player settings.

## Diagnostics and acceptance

Normal play does not poll or print playback positions. Invoke `ah64_audio_status`
in the game console when diagnosing a problem; it reports owned event IDs, playback
position and mix. Routine setup details use BepInEx debug logging; actionable
failures remain warnings/errors. Other mods' messages are not muted.

Verify the loop while stationary without firing, then set rotor volume to zero
and restore it. Check master/SFX mute, pause/focus, death/respawn, stage transitions,
and host/client distance behavior. A nonzero event ID alone does not prove audible
playback. Test fresh config defaults separately from a tuned profile.
