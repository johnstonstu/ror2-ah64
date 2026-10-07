# Translating AH-64

Simplified Chinese (`zh-CN`), Russian (`ru`), and Brazilian Portuguese (`pt-BR`) ship with the mod. They are machine-translated. Corrections are welcome.

In game, AH-64 follows **Settings → Language**. A language with no entry falls back to English. The Risk of Options tuning menu stays English on purpose: `AH64BalanceReport` matches those English setting names, and this change does not translate them.

## File

`AH64Mod/Language/AH64.language` is JSON. R2API.Language loads every `*.language` file under `BepInEx/plugins` and prefers a language-specific block over the generic English fallback.

```json
{
  "en": {
    "AH64_NAME": "AH-64"
  },
  "zh-CN": {
    "AH64_NAME": "AH-64"
  },
  "ru": {},
  "pt-BR": {}
}
```

Use those codes. They match `RoR2.Language.name` (`ru` also matches the game's `RU`). Do not add any other top-level key: R2API treats every key as a language. Do not rename tokens.

`en` is the source text. `tools/pack.ps1` copies the file to `plugins/AH64.language`, next to `AH64.dll`. A manual install has to keep that file. Without it, players only get the English copy embedded in the DLL.

## What not to change

- Token names.
- `{0}`, `{1}`, … placeholders. Each language needs the same placeholders, the same number of times, as `en`. The mod fills them when the tooltip is shown. Do not write the digits into the sentence.
- Rich-text tags, copied exactly: `<style=cIsUtility>`, `<style=cIsDamage>`, `<style=cSub>`, `<color=#CCD3E0>`. Word order and the text inside tags can change; tag names and attributes must stay intact.
- Model designations: AH-64, M230, XM301, M789, Hydra-70, AGM-114. Names such as Hellfire and Longbow may use established local translations.
- Vanilla item names inside a tag should be the official translation of that item (Backup Magazine, Lysate Cell), so the tooltip matches the logbook.
- The letter `B` in `AH64_DESCEND_CONTROLLER`. That is the controller button. The keyboard key is `{0}` and comes from the player's binding.

Numbers come from `AH64StaticValues` and `AH64BombingRunStaticValues` (damage, ranges, magazine sizes, reload times, armor). Russian and Brazilian Portuguese render decimals with a comma. The descend sentence also reads the classic-controls toggle and the descend binding from config.

The embedded English text supplies every token even if the loose file is missing, malformed, or lacks an English block. A valid partial English block overrides only its nonempty entries. Recovery logs a warning and keeps the remaining embedded text.

## Altitude sentence

The character description's `{6}` is a whole sentence, not a number.

- Classic controls use `AH64_ALTITUDE_HINT_CLASSIC` as that sentence.
- Current controls use `AH64_ALTITUDE_HINT`, whose `{0}` is the descend phrase:
  - `AH64_DESCEND_CONTROLLER` — "B on controller"
  - `AH64_DESCEND_KEYBOARD` — "{0} on keyboard"
  - `AH64_DESCEND_BINDINGS` — "{0}, {1}" when both are bound
  - `AH64_DESCEND_WITH_BINDINGS` — "descend ({0})"
  - `AH64_DESCEND` — the verb alone, when neither binding is set

The logbook reads `AH64_LORE`. The game takes the body name token `AH64_NAME` and replaces `_NAME` with `_LORE`.

## Check

From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File tools/check-language.ps1
```

The check parses the file with the same parser the mod uses, requires `zh-CN`, `ru`, and `pt-BR` to have the same keys as `en`, and checks that placeholders and style/color tags match. It also verifies English recovery for missing, malformed, and incomplete loose files. `tools/pack.ps1` runs it before building the zip.

## Test in game

1. Put `AH64.language` next to `AH64.dll` in `BepInEx/plugins/JohnstonStu-AH64/`. A normal package already does this.
2. In Risk of Rain 2, open **Settings → Language** and choose the language. Restart if the menus do not refresh.
3. Character select: name, subtitle, description (including the descend sentence), every loadout's skill tooltips, and the skin names.
4. Logbook: the AH-64 lore entry.
5. In a run, let the radar acquire a target and read the chat line.
6. If you finish or fail a run, the ending quote.
7. Switch back to English. The numbers should still match the skills.

## Send a correction

Open a [translation correction](https://github.com/johnstonstu/ror2-ah64/issues/new?template=translation.yml). The current text and the suggested text are enough. A screenshot of the tooltip helps when a tag or a number looks wrong.
