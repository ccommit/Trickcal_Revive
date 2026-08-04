# Character resource validation

- Status: `ready-for-integration`
- Production complete: `False`
- Registry: 50 (apostles 30, monster variants 20)
- Unique Spine sets: 64
- Generated assets: SkeletonData 64, Atlas 64, Material 64
- Captures: 80/80
- Failures: 0
- Rendering: Linear, PMA atlases 64, known risks 1

## Evidence boundary

- confirmed: raw and Unity-exported file hashes, preserved GUIDs, Spine 4.1 import, catalog references
- inferred: battle voice membership selected by approved base-directory and battle-name rule
- unconfirmed: original FX mapping/timing and production gameplay transitions

## Rendering risks

- All selected atlases declare pma:true while the project uses Linear Color Space. The Spine runtime warns that PMA atlas textures are not fully supported in Linear; captures contain populated meshes without black/white quads, but original color parity remains unconfirmed.

## Deferred from issue2

- Production scenes and gameplay state transitions
- Original FX mapping and timing
- Android device validation
