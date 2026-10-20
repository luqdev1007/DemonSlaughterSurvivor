# Third-party assets in `_Project/Art`

Every external asset that lives under `Assets/_Project/Art` and was not made for this project.
One row per source asset. Packs from the Asset Store that stay in their own `Assets/<Pack>` folders
are listed in `docs/recons/058_art-direction-recon.md` §1, not here.

| Asset | Source | Link | License | Where in the project | Added |
|---|---|---|---|---|---|
| Medieval Blocks 03 (Rob Tuytel) | Poly Haven | https://polyhaven.com/a/medieval_blocks_03 | CC0 1.0 | `Art/Terrain/Courtyard/Ground_Slabs_*` — courtyard slabs | 2026-10-20 |
| Forest Ground 04 (Rob Tuytel, Rico Cilliers) | Poly Haven | https://polyhaven.com/a/forest_ground_04 | CC0 1.0 | `Art/Terrain/Courtyard/Ground_Dirt_*` — trodden dirt paths | 2026-10-20 |
| Grass Path 2 (Rob Tuytel) | Poly Haven | https://polyhaven.com/a/grass_path_2 | CC0 1.0 | `Art/Terrain/Courtyard/Ground_GrassPath_*` — patchy grass between stone and walls | 2026-10-20 |
| Leafy Grass (Charlotte Baglioni) | Poly Haven | https://polyhaven.com/a/leafy_grass | CC0 1.0 | `Art/Terrain/Courtyard/Ground_Grass_*` — grass along the walls | 2026-10-20 |

Notes
- Poly Haven: all assets are CC0, no attribution required (https://polyhaven.com/license). Authors are kept here anyway.
- Texture files are the 2K JPG maps from Poly Haven. `*_Mask.png` is built from their AO, roughness and
  displacement maps (R = metallic 0, G = AO, B = height, A = 1 − roughness) for the URP terrain mask map, 1024 px.
