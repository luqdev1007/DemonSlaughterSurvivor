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
| Wine Barrel 01 (James Ray Cock) | Poly Haven | https://polyhaven.com/a/wine_barrel_01 | CC0 1.0 | `Art/Environment/PolyHaven/wine_barrel_01/` — barrels along the walls | 2026-10-20 |
| Wooden Barrels 01 (James Ray Cock) | Poly Haven | https://polyhaven.com/a/wooden_barrels_01 | CC0 1.0 | `Art/Environment/PolyHaven/wooden_barrels_01/` — barrels and broken barrel staves (siege debris) | 2026-10-20 |
| Wooden Crate 01 (James Ray Cock) | Poly Haven | https://polyhaven.com/a/wooden_crate_01 | CC0 1.0 | `Art/Environment/PolyHaven/wooden_crate_01/` — small crates | 2026-10-20 |
| Wooden Crate 02 (James Ray Cock, Jurita Burger) | Poly Haven | https://polyhaven.com/a/wooden_crate_02 | CC0 1.0 | `Art/Environment/PolyHaven/wooden_crate_02/` — crates | 2026-10-20 |
| Wooden Bucket 01 (James Ray Cock) | Poly Haven | https://polyhaven.com/a/wooden_bucket_01 | CC0 1.0 | `Art/Environment/PolyHaven/wooden_bucket_01/` — buckets at the well / fire | 2026-10-20 |
| Kite Shield (Ulan Cabanilla) | Poly Haven | https://polyhaven.com/a/kite_shield | CC0 1.0 | `Art/Environment/PolyHaven/kite_shield/` — dropped shields | 2026-10-20 |
| Stone Fire Pit (Sebastian Platen) | Poly Haven | https://polyhaven.com/a/stone_fire_pit | CC0 1.0 | `Art/Environment/PolyHaven/stone_fire_pit/` — smouldering hearths | 2026-10-20 |
| Rock Moss Set 01 (Kless Gyzen) | Poly Haven | https://polyhaven.com/a/rock_moss_set_01 | CC0 1.0 | `Art/Environment/PolyHaven/rock_moss_set_01/` — rubble of the collapsed wall | 2026-10-20 |
| Rock Moss Set 02 (Kless Gyzen) | Poly Haven | https://polyhaven.com/a/rock_moss_set_02 | CC0 1.0 | `Art/Environment/PolyHaven/rock_moss_set_02/` — rubble of the collapsed wall | 2026-10-20 |
| Shrub 02 (Rico Cilliers) | Poly Haven | https://polyhaven.com/a/shrub_02 | CC0 1.0 | `Art/Environment/PolyHaven/shrub_02/` — bushes along the walls | 2026-10-20 |
| Shrub 03 (Rico Cilliers) | Poly Haven | https://polyhaven.com/a/shrub_03 | CC0 1.0 | `Art/Environment/PolyHaven/shrub_03/` — small shrubs | 2026-10-20 |
| Shrub 04 (Rico Cilliers) | Poly Haven | https://polyhaven.com/a/shrub_04 | CC0 1.0 | `Art/Environment/PolyHaven/shrub_04/` — small shrubs | 2026-10-20 |
| Fern 02 (Rob Tuytel, Rico Cilliers) | Poly Haven | https://polyhaven.com/a/fern_02 | CC0 1.0 | `Art/Environment/PolyHaven/fern_02/` — ferns in the shade of the walls | 2026-10-20 |
| Weed Plant 02 (Rob Tuytel, Rico Cilliers) | Poly Haven | https://polyhaven.com/a/weed_plant_02 | CC0 1.0 | `Art/Environment/PolyHaven/weed_plant_02/` — weeds in the cracks | 2026-10-20 |
| Large Castle Door (Tina) | Poly Haven | https://polyhaven.com/a/large_castle_door | CC0 1.0 | `Art/Environment/PolyHaven/large_castle_door/` — doors of the lean-tos / gate | 2026-10-20 |
| Burned Ground 01 (Rob Tuytel) | Poly Haven | https://polyhaven.com/a/burned_ground_01 | CC0 1.0 | `Art/Textures/T_Scorch.png` — scorch marks (1K albedo, 512 px, our noisy radial alpha) | 2026-10-20 |

Notes
- Poly Haven: all assets are CC0, no attribution required (https://polyhaven.com/license). Authors are kept here anyway.
- Texture files are the 2K JPG maps from Poly Haven. `*_Mask.png` is built from their AO, roughness and
  displacement maps (R = metallic 0, G = AO, B = height, A = 1 − roughness) for the URP terrain mask map, 1024 px.
- Models are the 1K FBX of Poly Haven (rocks 2K). `*_mos_*.png` packs their ARM map for URP Lit:
  R = metallic, G = AO, A = 1 − roughness, used as both Metallic and Occlusion map. Plant `*_diff_*.png` carry
  the Poly Haven alpha map in their alpha channel (alpha clip). Materials `M_*.mat` are ours, URP Lit.
