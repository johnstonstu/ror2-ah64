# Release checks

Main may contain an unreleased 1.1 candidate. A build or merge does not authorize
a public mod release.

1. Review the complete diff, including new source, assets and Unity `.meta` files.
   Check for credentials, private configs, obsolete handoffs and generated outputs.
2. Keep `AH64Plugin.MODVERSION` and `Build/manifest.json` identical using plain
   `major.minor.patch`. Reconcile both READMEs and the changelog with final behavior.
3. Build the Unity bundle in 2021.3.33f1 if its inputs changed, build the Wwise bank,
   and compile with `/p:AH64DeployToProfiles=false`.
4. Run language, feedback, and weapon-preview checks. Package without bypassing
   freshness checks. Inspect the exact ZIP allowlist and hashes. `AH64.language`
   ships next to the DLL; the packager runs the language check itself.
5. Install the ZIP into a fresh profile with the game closed. Test required
   dependencies alone, then optional Risk of Options. Check defaults, loadouts,
   skins, movement, audio lifecycle, feedback and host/client behavior. Preserve
   existing profiles and saved tuning.
6. Merge reviewed source before publishing so README images and the feedback form
   exist on `main`. Do not describe simulations as live multiplayer validation.
7. Obtain explicit approval naming the version before uploading to Thunderstore.
   Change the unreleased changelog heading when preparing that approved release.

The package README is `Build/README.md`. Its top links route players to bug reports,
balance feedback and Discussions; `website_url` routes to the repository. Verify
those destinations after merge, then use the validated ZIP for the store update.

Normal play should have no automatic playback-position polling or successful-skin
messages. Retain actionable warnings/errors. Use `ah64_audio_status` when diagnosing
audio; logs from the game and other mods are not suppressed.
