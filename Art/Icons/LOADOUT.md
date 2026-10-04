# Attachment icon set

The ten active loadout skills use 512px PNGs embedded in AH64.dll. This lets the
new variants ship without rebuilding the model assetbundle. Passive plating keeps
its existing bundle icon.

`loadout-contact-sheet.png` shows all ten icons at 224px plus 48px HUD and 64px
loadout sizes. The four approved M230, Gatling, HE cannon and Hydra illustrations
are preserved, downsampled from their original bundle source PNGs. The six new
illustrations in `loadout-attachments-source.png` were authored with ImageGen,
using the approved Gatling/Hydra art as style references: faceted olive hardware,
three-quarter view, dark diagonal plate, thin olive border and cream highlights.

The source sheet is three columns by two rows, in this order:

| Position | Skill | Attachment silhouette |
| --- | --- | --- |
| Top left | Longbow | Open paired missile rails and squared forward frames |
| Top middle | Hellfire | Enclosed twin launch tubes with large front collars |
| Top right | Bombing Run | Short carriers, exposed finned bombs and release latches |
| Bottom left | Evasive Jink | Paired lateral vector nozzles |
| Bottom middle | Smoke Backflip | Compact aft vent/countermeasure canister |
| Bottom right | Banked Break | Paired deployed fuselage airbrake fins |

These silhouettes were checked against the attachment implementation. They are
illustrations of the attachment family, not dimensionally exact engineering views.

Run `tools/make-loadout-icons.ps1` on Windows to reproduce the embedded PNGs and
review sheet. It uses System.Drawing only and never launches Unity or the game.
`AH64LoadoutIcons.Get(skillName, fallback)` caches sprites and logs a warning once
per failed optional icon, retaining the prior bundle icon if decoding fails.

Validation: inspected the full contact sheet and its HUD-sized samples. Runtime
presentation still needs an eventual in-game visual check; this art work does not
claim a game launch or profile installation.
