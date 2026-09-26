# Primary icon artwork — 2026-09-26

User selected the original illustrated olive helicopter/chin-gun icon as the style
reference and rejected head-on procedural bore diagrams. The original reference
is now texAH64GatlingIcon.png, matching its rotary barrel cluster. Built-in image_gen
produced two edits; all final PNGs live in AH64UnityProject/Assets/AH64/Bundle/Icons.
No CLI fallback was used. Existing Unity meta GUIDs are preserved.

M230 prompt: Preserve the illustrated low-poly faceted military helicopter chin-turret
scene, olive body, blue-black canopy, three-quarter perspective, dark diagonal-striped
plate, fine olive border, warm polygonal muzzle flash. Replace only the six-barrel
rotary gun with an M230-style single slender barrel chain gun, clear open muzzle,
rectangular mechanical breech and ammunition feed under the nose. Preserve composition,
upper-left lighting and warm orange flash at lower-right. No schematic, text or watermark.

HE cannon prompt: Preserve the same reference style and composition; replace only
the rotary gun with a powerful single-barrel HE autocannon, thick short reinforced
barrel, large open muzzle, angular sleeve and recoiling breech. Visually heavier
and stubbier than M230. One large bore, no rotary cluster. No text or watermark.

The legacy AH64GatlingIconBaker entry points now import/validate these authored PNGs;
building cannot silently restore the rejected procedural icons.
