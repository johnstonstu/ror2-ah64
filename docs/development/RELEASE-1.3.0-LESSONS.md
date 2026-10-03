# AH64 1.3.0 checkpoint lessons

Keep each checkpoint's observation, cause, prevention and evidence together.
Record measured results rather than turning historical reports into current passes.
This project log does not update global assistant memories or another project.

## Bootstrap: current base and concurrent translation work

- Observation: the production checkout was clean but its remote-tracking main was stale.
- Cause: remote main advanced by a README-only commit after the earlier inspection.
- Decision/prevention: fetch only main's tracking ref, read the complete intervening diff,
  and create a new integration worktree at the reviewed SHA; do not move main or merge PR17.
- Evidence: base `fe3c6ca81958a5210f5ec193f5006dfce21c1c7e`, bootstrap `identity.json`,
  `incoming-pr17.json` and worktree/status records under `dist/bootstrap/`.

## Bootstrap: SDK first-run behavior

- Observation: the first restore printed "Installed an ASP.NET Core HTTPS development certificate."
- Cause: a new workspace-local CLI home triggered first-run initialization, and the initial
  environment omitted the dedicated certificate-generation opt-out.
- Prevention: set `DOTNET_GENERATE_ASPNET_CERTIFICATE=false` before the first SDK command,
  in addition to telemetry/first-time-experience options. This is now in the local environment helper.
- Evidence: `dist/bootstrap/restore.log` and `Set-BaselineEnvironment.ps1`.
- Limit: no certificate-store inspection or removal was performed; do not claim that the message
  proves a particular new certificate's identity or that it was rolled back.

## Bootstrap: distinguish baseline warnings from feature regressions

- Observation: unchanged Release source compiled with 22 obsolete-API warnings and zero errors.
  Feedback checks emitted intentional CS0436 Application test-double warnings; both checks passed.
- Cause: baseline uses older security/SkinDef/FOV APIs and a local browser stub in its offline checks.
- Prevention: preserve the warning baseline and compare future runs; do not rewrite the framework
  or suppress new warnings just to produce a clean-looking report.
- Evidence: `dist/bootstrap/build.log`, `baseline-warnings.txt`, `feedback.log`,
  `weapon-previews.log` and their result JSON records. Weapon checks passed 1,978 assertions.

## Test-foundation handoff: use Lightning's evidence levels

- Observation from prior read-only inspection: Lightning's launcher prints outcomes without reliably
  returning failure for incomplete/failed scenarios; its current Stormspear offline default assertion
  drifted from production. Successful solo traces also retain pop flags or classified game errors.
- Cause: scenario completion, semantic assertions, visual heuristics and historical tunables are
  different evidence, and they can drift when a kit changes.
- Prevention: AH64 test foundation requires explicit terminal status, scenario/version/hash identity,
  current assertion expectations and classified full-log/visual results. Keep solo, offline,
  physical-controller and real multiplayer acceptance separate.
- Evidence: prior inspected Lightning `Run-Autopilot.ps1`, `DevAutopilot.cs`,
  `Check-Stormspear.ps1` versus `StormspearTuning.cs`, and `artifacts/aim-cocked-v096-check`,
  `full0916`, `items0916` and `storm-d5` traces. These were not rerun by bootstrap.

## Bootstrap: compilation is not a deployable mod

- Observation: the new worktree contains a compiled/staged DLL but no generated bundle or bank.
- Cause: Unity/Wwise outputs are ignored and are not brought into a fresh Git worktree.
- Prevention: identify/build or deliberately import verified artifacts in the later test-foundation
  phase; do not launch/package from a DLL-only checkout. Preserve all-profile deployment opt-out
  and one serial game/profile/log-capture owner across projects.
- Evidence: `dist/bootstrap/artifact-hashes.json` and bootstrap handoff.

## Contracts checkpoint: separate feature intent from shared ownership

- Observation: movement entry data could be recaptured on observers, while guidance could
  accidentally claim weapon input or make all I.C.B.M. missiles converge with extra damage scaling.
- Cause: motor, aim, cosmetic attitude and projectile launch identity are distinct ownership domains;
  separate worktrees alone do not enforce them or serialize game/profile/log access.
- Decision/prevention: authority captures movement once; FlightVisuals remains the model owner.
  Latest lead missile receives native-aim designation; two existing full-payload fan extras remain
  unguided and cannot recurse. Shared registries/config/hooks belong to the integrator.
  Runtime scripts share an explicit canonical exclusive lease and verified artifact/backups protocol.
- Evidence: the contracts in `RELEASE-1.3.0-COORDINATION.md` and prior baseline source inspection
  of ServoDash, SmokeBackflip, FlightVisuals and FireHellfire. This checkpoint changes documentation
  only; it does not claim implementation or runtime verification of those contracts.
- Next improvement: compare fixed-tick semantic telemetry with human review and preserve useful
  failed scenarios as regressions, without treating a screenshot or process exit as a complete pass.
