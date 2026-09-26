# Rotor playback failure: findings and repair plan

Research date: 2026-09-26. Candidate B failed its listening test. This is a
playback-backend failure, not an unresolved volume-tuning problem.

Update: candidate C implements the Wwise migration and is installed in
`demo time new`. Authoring/build and package validation pass. The embedded PCM
has the correct length and differs from the source by at most one 16-bit
quantization step after Wwise conversion. In-game audible playback, mix and
lifecycle acceptance below still require the user's playtest. See
`HANDOFF-1.1.0.md` for installed hashes and backup location.

## Confirmed evidence

The installed candidate-B DLL SHA-256 matches the build:
`6516912576E7D45E1FACE601907BE2FDB09E88058910C57097FC5385A5C52C4A`.
The latest Unity Player.log (13:29 local time) contains:

```text
Rotor audio candidate B: clip=sfxAH64RotorHoverGrounded, playing=False,
gain=0.450, settingsGain=1.000, spatialBlend=0,
listenerVolume=1.000, listenerPaused=False.
Rotor Unity listener: Scene Camera, distance=49.7.
```

Read-only inspection of the installed game's `Risk of Rain 2_Data/globalgamemanagers`
using UnityPy found this AudioManager configuration:

```json
{"m_Volume": 1.0, "m_DisableAudio": true}
```

Our mod's Unity authoring project has `m_DisableAudio: 0`. That setting does not
override the host game's setting when its assetbundle is loaded. A valid AudioClip
and an active AudioListener are therefore insufficient: Unity playback is disabled
in the actual game. Source EQ, priority, spatialBlend and gain cannot repair this.
The snapshot establishes failed playback; it cannot identify which other sound
the player faintly heard. Do not claim that was the custom rotor.

The game's native AkSoundEngine.dll version getters return **2023.1.4.8496**.
Its SoundbanksInfo.xml declares schema 16, SoundBankVersion 150. Match these
installed values rather than old wiki examples of schema 12/bank 135.

Saved tuning after this test: rotor volume 1, pitch .8849593, movement pitch
.01499999, movement boost 5.144491 dB, response .2807811 seconds, cutoff 5000 Hz;
gatling spool volume .3699187. Preserve these for reference, but they are not a
validated mix: the Unity rotor was not playing in the diagnostic snapshot.

## Open-source comparisons

1. [Suncube Base Helicopter Controller](https://github.com/suncube/Base-Helicopter-Controller):
   `Assets/2_NewHelicopter/HelicopterController.cs` uses an AudioSource and maps
   engine force to pitch. Its `ProjectSettings/AudioManager.asset` explicitly
   enables Unity audio (`m_DisableAudio: 0`). Same playback primitive as ours,
   but a crucial difference in the host configuration. Copying its pitch logic
   cannot enable RoR2's disabled backend.
2. [FlightGear UH-1 sound definition](https://github.com/FGMEMBERS/uh-1/blob/master/Sounds/uh1-sound.xml):
   defines a looped rotor, pitch from rotor RPM, volume from filtered torque,
   separate turbine audio and view-dependent conditions. Useful design principle:
   use load chiefly for loudness/texture and RPM for pitch. Its engine and mixing
   implementation are not drop-in RoR2 code. No assets were copied.
3. [RoR2 OriginalSoundTrack](https://github.com/kylepaulsen/RoR2-Original-Sound-Track):
   uses NAudio's WaveOutEvent, bypassing Unity audio. This is an alternative output
   path, but a spatial survivor loop would need additional output, pause, volume
   and lifecycle integration. Prefer the same Wwise engine as the rest of RoR2.
4. [RoR2 soundbank loading documentation](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Loading-Assets/#loading-soundbanks):
   describes Wwise bank loading, including R2API.Sound's `.sound` discovery.
   AH64 already declares R2API.Sound in its project and package manifest.

ESF's public package and Ms Isle's source link were also checked. No usable ESF
source implementation was established, and Ms Isle's Bitbucket source could not
be read in this pass; do not cite either as verified audio implementation evidence.

## Concrete repair

1. Author a uniquely named AH64 Wwise bank with Wwise 2023.1.4.8496. Start with
   the existing CC0 WAV: one looping rotor and a stop event. No new sound asset is
   required to prove playback. Do not load a replacement Init.bnk into the game.
2. Route it to the game's SFX mix using the compatible bus/RTPC setup. Confirm
   master, SFX, pause and focus mute in-game; do not multiply them a second time
   using the old Unity-volume bridge.
3. Replace the AudioSource/AudioLowPassFilter backend with a Wwise emitter and
   a tracked playing ID. Start once per activation, stop that ID on disable,
   death and destruction; retry failed starts on a bounded cadence with errors.
4. Map temporary tuning controls to explicitly authored AH64 Wwise parameters:
   rotor gain, pitch and tone. Keep load smoothing in C#. Preserve the independent
   Wwise gatling spool emitter, which is already on the correct backend.
5. Add console diagnostics for bank-load result, runtime version, post-event
   result, playing ID and playback position advancing across two samples. A
   nonzero post-event ID alone is not proof of sustained, audible playback.
6. Update packaging to require the bank and verify it is present in the ZIP and
   installed profile. Keep the existing CC0 provenance and source WAV.

Do not patch the game's globalgamemanagers or force-enable its Unity audio.
That would modify the host game instead of repairing this mod's integration.

## Acceptance checks before more tone tuning

- Wwise bank loads successfully; event returns a nonzero playing ID; playback
  position advances and wraps without a premature end callback.
- Rotor is clearly identifiable while stationary at spawn with no firing and no
  collective input. Setting rotor volume to zero removes exactly that sound;
  restoring it brings the rotor back. This distinguishes it from vanilla audio.
- Master/SFX zero mute it; pause/resume and focus mute behave like other game SFX.
- Death, respawn and stage change leave exactly one intended loop, with no orphan.
- Local and remote aircraft behave correctly in host/client play.
- Only after those pass, tune the heavier chop and reduce temporary release UI.

## Current prerequisite and status

No Wwise authoring installation was found in either standard Audiokinetic
Program Files location. The game runtime cannot generate a bank. The required
next dependency is **Wwise authoring 2023.1.4.8496**, not Blender or a model edit.
No new audio implementation was installed in this research pass. Candidate B
remains installed but is not a successful rotor playback fix.

The repository's repeated "six-event soundbank limit" statement was not verified.
Audiokinetic's current [trial documentation](https://www.audiokinetic.com/en/products/licensing/)
describes a 200-media-asset limit instead. Check the chosen authoring version's
actual terms; the proposed one-loop/two-event bank is small either way.
