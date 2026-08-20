#!/usr/bin/env python3
"""Recover the invasion stage-select ("스테이지 리스트") UI from the read-only reference.

Two source shapes, both deterministic:
  * ATLAS sprites  -> `02.resources/04.stage/sprites/stage/<Name>.json` carry
    m_AtlasTags=["Stage"] + m_RenderDataKey. The pixels live packed in the
    multi-page `Stage` SpriteAtlas; we crop from the correct page.
  * RAW sprites    -> `rawsprites/stagelist/<Name>.png` (+ .json for pivot).
    Byte-for-byte copy.

The `Stage` atlas is MULTI-PAGE (2048 + 1024). Each render-map entry names its
page by m_Texture.m_PathID; we bind PathID -> page file by which canonical page
the entry's rects actually fit inside (deterministic, needs no page metadata).

Reuses the issue8 lobby-chrome recovery conventions (deterministic .meta GUID,
hash manifest). Originals are never modified.
"""

from __future__ import annotations

import argparse
import hashlib
import io
import json
import os
import re
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

try:
    from PIL import Image
except ImportError as exception:  # pragma: no cover
    raise SystemExit("Pillow is required: python -m pip install --user Pillow") from exception


RAW_ASSETS_RELATIVE = Path("03_extracted_resources/02_raw_data/Assets")
SOURCE_IDENTITY_RELATIVE = Path("Recovery/Manifests/source-identity.json")
OUTPUT_SPRITES_RELATIVE = Path("Assets/Game/Content/UI/StageSelect")
OUTPUT_MANIFEST_RELATIVE = Path("Recovery/Manifests/stage-select-ui.json")

STAGE_SPRITE_DIR = "02.resources/04.stage/sprites/stage"
COMMON_ICON_SPRITE_DIR = "02.resources/99.common/sprites/commonicons"
COMMON_UI_SPRITE_DIR = "02.resources/99.common/sprites/commonui"
STATE_ICON_SPRITE_DIR = "02.resources/02.ingame/sprites/stateicons"
STAGELIST_RAW_DIR = "02.resources/addressableresources/rawsprites/stagelist"
ATLAS_DIR = "02.resources/addressableresources/atlases"
TEXTURE2D_DIR = "Texture2D"

# --- what to recover -------------------------------------------------------
# Core set to render the stage-select screen + its stage-info popup. Elements
# not yet needed (NEW badge, world-nav arrows) are intentionally omitted; add
# them here when the view demands them rather than recovering the whole atlas.
ATLAS_SPRITES = [
    # stage node tiles. Color is a world-theme variant, not a progress-state color.
    "StageTile_Blue_Clear", "StageTile_Blue_UnClear",
    "StageTile_Yellow_Clear", "StageTile_Yellow_UnClear",
    "StageTile_Green_Clear", "StageTile_Green_UnClear",
    "StageTile_Brown_Clear", "StageTile_Brown_UnClear",
    "StageTile_Story",
    "Stage_TileLock",
    # node difficulty badge / stars / reward track
    "Stage_TileLevel_Easy",
    "StageIconBorder",
    "StageReward_Star", "StageReward_ItemBg", "StageReward_Value", "Stage_RewardChack",
    # world nav / recommended-power bubble
    "Icon_WorldCompass", "Stage_WorldMapBtn",
    "StageList_QuickBattle_SpeechBubble",
    # difficulty tabs 순한맛/매운맛/핵불맛 = Mild/Hot/Fire
    "WorldListTitleBase_Mild", "WorldListTitleBase_Hot", "WorldListTitleBase_Fire",
    "Stage_LevelButton_Normal_On", "Stage_LevelButton_Normal_Off",
    "Stage_LevelButton_Hard_On", "Stage_LevelButton_Hard_Off",
    "Stage_LevelButton_VeryHard_On", "Stage_LevelButton_VeryHard_Off",
    # stage-info popup (출현몬스터 / 추천성격 / 보상)
    "Stage_Level_Easy", "Stage_MonsterInfoBg", "Border_MonsterInfo", "MonsterDescDeco",
    "BossSlotBg", "BossSlotBorder", "Btn_Battle_Deck",
    # 노드 장식(타워/깃발/맵데코 후보) — 원본 타일 재현용
    "Object_1", "Object_2", "Object_3", "Stage_Garland", "StageTile_MapDeco", "Chap_StampPerf",
]

COMMON_ICON_SPRITES = [
    "Star_Clear",
    "Star_Easy_Off",
    "CommonButton_Close_1",
    "Common_Icon_Search",
    "CommonIcon_Enemy",
]

COMMON_UI_SPRITES = [
    # 월드 전환 후보. 화면 캡처와 원본 비율을 대조해 Stretch를 실제 View에 사용한다.
    "Common_Arrow_3",
    "Common_Arrow_Stretch",
    "CommonCircle_1",
]

STATE_ICON_SPRITES = [
    # 현재 1챕터 fixture가 순환 사용하는 추천 성격 5종.
    "Common_UnitPersonality_Naive",
    "Common_UnitPersonality_Mad",
    "Common_UnitPersonality_Jolly",
    "Common_UnitPersonality_Gloomy",
    "Common_UnitPersonality_Cool",
]

# raw byte-for-byte copies. ch1 tile theme assumed 'fairy' (Trickcal ch1 = 요정
# 마을); flagged inferred until matched against a chapter->theme table.
RAW_SPRITES = [
    ("Worldlist_background", "map-background", "confirmed"),
    ("Bg_BattleList", "map-background-alt", "confirmed"),
    ("StagePopup_HeaderDeco", "popup-header-deco", "confirmed"),
    ("Stage_fairy_a", "ch1-theme-tile", "inferred"),
    ("Stage_fairy_b", "ch1-theme-tile", "inferred"),
    ("Stage_fairy_c", "world-1-map-background", "confirmed"),
    ("Stage_fairy_d", "ch1-theme-tile", "inferred"),
    ("Stage_fairy_e", "ch1-theme-tile", "inferred"),
    # 첨부된 침략_스테이지_1~10 캡처와 원본 raw sprite의 지형을 직접 대조한 매핑.
    ("Stage_furry_a", "world-2-map-background", "confirmed"),
    ("Stage_elf_d", "world-3-map-background", "confirmed"),
    ("Stage_soul_a", "world-4-map-background", "confirmed"),
    ("Stage_ghost_a", "world-5-map-background", "confirmed"),
    ("Stage_furry_b", "world-6-map-background", "confirmed"),
    ("Stage_ghost_c", "world-7-map-background", "confirmed"),
    ("Stage_ghost_b", "world-8-map-background", "confirmed"),
    ("Stage_elf_a", "world-9-map-background", "confirmed"),
    ("Stage_elf_c", "world-10-map-background", "confirmed"),
]

# DeliaBattle 아틀라스(98.independent/deliabattle/atlassed)의 raw 스프라이트 —
# 별/스테이지 번호 배너를 이 이미지로 매핑(사용자 지정).


@dataclass
class PreparedAsset:
    category: str
    purpose: str
    evidence_status: str
    primary_source: Path
    supporting_sources: list[tuple[str, Path]]
    output_path: Path
    output_bytes: bytes
    meta_bytes: bytes
    transformation: str
    parameters: dict[str, Any]
    evidence: str


# --- shared helpers (mirror Recover-LobbyUIChrome.py) ----------------------

def sha256_bytes(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def json_key(value: dict[str, Any]) -> str:
    return json.dumps(value, sort_keys=True, separators=(",", ":"))


def resolve_source_root(project_root: Path, explicit_root: Path | None) -> Path:
    candidates: list[Path] = []
    if explicit_root is not None:
        candidates.append(explicit_root)
    env = os.environ.get("TRICKCAL_REFERENCE_ROOT")
    if env:
        candidates.append(Path(env))
    settings_path = project_root / "UserSettings/TrickcalRecoverySettings.json"
    if settings_path.is_file():
        settings = read_json(settings_path)
        if settings.get("sourceRoot"):
            candidates.append(Path(settings["sourceRoot"]))
    for candidate in candidates:
        resolved = candidate.resolve()
        if (resolved / "00_original_apk/trickcal.apk").is_file() and (resolved / "file_manifest.csv").is_file():
            return resolved
    raise RuntimeError("Recovery source not found. Pass --source-root or set TRICKCAL_REFERENCE_ROOT.")


def validate_source_identity(project_root: Path, source_root: Path) -> None:
    identity = read_json(project_root / SOURCE_IDENTITY_RELATIVE)
    failures = []
    for expected in identity["files"]:
        p = source_root / expected["relativePath"]
        if not p.is_file():
            failures.append(f"missing: {expected['relativePath']}")
        elif p.stat().st_size != expected["bytes"] or sha256(p) != expected["sha256"]:
            failures.append(f"identity mismatch: {expected['relativePath']}")
    if failures:
        raise RuntimeError("Reference identity validation failed:\n" + "\n".join(failures))


def deterministic_guid(project_root: Path, output_path: Path) -> str:
    seed = f"trickcal-recovery-stage-select-ui:{output_path.relative_to(project_root).as_posix()}"
    return hashlib.md5(seed.encode("utf-8")).hexdigest()


def make_texture_meta(guid: str, pivot: dict[str, Any], ppu: float, border: dict[str, Any] | None) -> bytes:
    border = border or {"m_X": 0, "m_Y": 0, "m_Z": 0, "m_W": 0}
    return f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  streamingMipmaps: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 9
  spritePivot: {{x: {pivot['m_X']}, y: {pivot['m_Y']}}}
  spritePixelsToUnits: {ppu}
  spriteBorder: {{x: {border['m_X']}, y: {border['m_Y']}, z: {border['m_Z']}, w: {border['m_W']}}}
  alphaUsage: 1
  alphaIsTransparency: 1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  cookieLightType: 0
  platformSettings: []
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
""".encode("utf-8")


# --- multi-page Stage atlas -------------------------------------------------

@dataclass
class AtlasContext:
    render_map: dict[str, dict[str, Any]]          # json_key(Key) -> Value
    pages: dict[str, Image.Image]                   # texture-pathid -> page image
    page_names: dict[str, str]                      # texture-pathid -> page filename


def load_atlas_context(raw_root: Path, tag: str, atlas_dir: str = ATLAS_DIR) -> AtlasContext:
    atlas_matches = [
        p for p in (raw_root / atlas_dir).glob(f"{tag}.json") if not p.name.startswith("_low_")
    ]
    if len(atlas_matches) != 1:
        raise RuntimeError(f"Expected one atlas json for {tag}, found {len(atlas_matches)}")
    atlas = read_json(atlas_matches[0])
    entries = atlas["m_RenderDataMap"]
    render_map = {json_key(e["Key"]): e["Value"] for e in entries}

    # canonical pages for this tag: Texture2D/*-<tag>-*.png without -var-
    page_files = sorted(
        p for p in (raw_root / TEXTURE2D_DIR).glob(f"*-{tag}-*.png") if "-var-" not in p.name
    )
    if not page_files:
        raise RuntimeError(f"No canonical atlas page for {tag}")
    page_size = {}
    for p in page_files:
        m = re.search(r"-(\d+)x(\d+)-", p.name)
        if not m:
            raise RuntimeError(f"Cannot parse page size from {p.name}")
        page_size[p] = (int(m.group(1)), int(m.group(2)))

    # bind each texture-pathid to the tightest page its rects fit inside
    extents: dict[str, tuple[int, int]] = {}
    for value in render_map.values():
        tex = json_key(value["m_Texture"])
        rect = value["m_TextureRect"]
        mx = round(rect["m_X"]) + round(rect["m_Width"])
        my = round(rect["m_Y"]) + round(rect["m_Height"])
        cur = extents.get(tex, (0, 0))
        extents[tex] = (max(cur[0], mx), max(cur[1], my))

    pages: dict[str, Image.Image] = {}
    page_names: dict[str, str] = {}
    for tex, (need_w, need_h) in extents.items():
        fit = sorted(
            (p for p, (w, h) in page_size.items() if w >= need_w and h >= need_h),
            key=lambda p: page_size[p][0] * page_size[p][1],
        )
        if not fit:
            raise RuntimeError(f"No page fits texture {tex} (needs {need_w}x{need_h})")
        chosen = fit[0]
        pages[tex] = Image.open(chosen).convert("RGBA")
        page_names[tex] = chosen.name
    return AtlasContext(render_map, pages, page_names)


def crop_atlas_sprite(
    project_root: Path,
    raw_root: Path,
    ctx: AtlasContext,
    name: str,
    sprite_dir: str = STAGE_SPRITE_DIR,
    atlas_tag: str = "Stage",
) -> PreparedAsset:
    sprite_path = raw_root / sprite_dir / f"{name}.json"
    if not sprite_path.is_file():
        raise FileNotFoundError(sprite_path)
    sprite = read_json(sprite_path)
    tags = sprite.get("m_AtlasTags", [])
    if tags != [atlas_tag]:
        raise RuntimeError(f"Unexpected atlas tags for {name}: {tags}")
    value = ctx.render_map.get(json_key(sprite["m_RenderDataKey"]))
    if value is None:
        raise RuntimeError(f"No atlas render data for {name}")
    if value.get("m_SettingsRaw") not in (3, 67):
        raise RuntimeError(f"Unsupported packing for {name}: {value.get('m_SettingsRaw')}")
    tex = json_key(value["m_Texture"])
    page = ctx.pages[tex]
    rect = value["m_TextureRect"]
    x, y = round(rect["m_X"]), round(rect["m_Y"])
    w, h = round(rect["m_Width"]), round(rect["m_Height"])
    if x < 0 or y < 0 or x + w > page.width or y + h > page.height:
        raise RuntimeError(f"Rect outside page for {name}")
    # Unity texture space is bottom-left; PIL is top-left.
    crop = page.crop((x, page.height - y - h, x + w, page.height - y))
    if crop.getbbox() is None:
        raise RuntimeError(f"Empty crop for {name}")
    buffer = io.BytesIO()
    crop.save(buffer, format="PNG", optimize=True)
    output_path = project_root / OUTPUT_SPRITES_RELATIVE / f"{name}.png"
    return PreparedAsset(
        category="stage-select-ui",
        purpose=name,
        evidence_status="confirmed",
        primary_source=raw_root / TEXTURE2D_DIR / ctx.page_names[tex],
        supporting_sources=[("sprite-serialization", sprite_path)],
        output_path=output_path,
        output_bytes=buffer.getvalue(),
        meta_bytes=make_texture_meta(
            deterministic_guid(project_root, output_path),
            sprite["m_Pivot"], sprite["m_PixelsToUnits"], sprite.get("m_Border"),
        ),
        transformation="crop-multipage-spriteatlas-page",
        parameters={
            "atlasTag": atlas_tag,
            "atlasPageFileName": ctx.page_names[tex],
            "rect": {"x": x, "y": y, "width": w, "height": h},
            "settingsRaw": value.get("m_SettingsRaw"),
            "coordinateConvention": "Unity bottom-left converted to PNG top-left",
            "pivot": sprite["m_Pivot"],
            "pixelsPerUnit": sprite["m_PixelsToUnits"],
        },
        evidence=f"Serialized Sprite m_RenderDataKey matched the {atlas_tag} atlas render map; page bound by rect-fit.",
    )


def copy_raw_sprite(project_root: Path, raw_root: Path, rel_dir: str, name: str, purpose: str, status: str) -> PreparedAsset:
    source = raw_root / rel_dir / f"{name}.png"
    serialization = source.with_suffix(".json")
    if not source.is_file():
        raise FileNotFoundError(source)
    sprite = read_json(serialization) if serialization.is_file() else {}
    pivot = sprite.get("m_Pivot", {"m_X": 0.5, "m_Y": 0.5})
    ppu = sprite.get("m_PixelsToUnits", 100.0)
    output_path = project_root / OUTPUT_SPRITES_RELATIVE / source.name
    return PreparedAsset(
        category="stage-select-ui",
        purpose=purpose,
        evidence_status=status,
        primary_source=source,
        supporting_sources=([("sprite-serialization", serialization)] if serialization.is_file() else []),
        output_path=output_path,
        output_bytes=source.read_bytes(),
        meta_bytes=make_texture_meta(deterministic_guid(project_root, output_path), pivot, ppu, sprite.get("m_Border")),
        transformation="byte-for-byte-copy",
        parameters={"pivot": pivot, "pixelsPerUnit": ppu},
        evidence=f"Raw stagelist sprite ({status}).",
    )


def validate_conflicts(prepared: list[PreparedAsset]) -> None:
    for asset in prepared:
        for path, content in ((asset.output_path, asset.output_bytes),
                              (Path(f"{asset.output_path}.meta"), asset.meta_bytes)):
            if path.exists() and sha256(path) != sha256_bytes(content):
                raise RuntimeError(f"Generated asset conflict; existing file kept: {path}")


def write_outputs(prepared: list[PreparedAsset]) -> None:
    for asset in prepared:
        asset.output_path.parent.mkdir(parents=True, exist_ok=True)
        asset.output_path.write_bytes(asset.output_bytes)
        Path(f"{asset.output_path}.meta").write_bytes(asset.meta_bytes)


def source_record(source_root: Path, role: str, path: Path) -> dict[str, Any]:
    return {"role": role, "relativePath": path.relative_to(source_root).as_posix(),
            "bytes": path.stat().st_size, "sha256": sha256(path)}


def make_manifest(project_root: Path, source_root: Path, prepared: list[PreparedAsset]) -> dict[str, Any]:
    entries = []
    for asset in prepared:
        out_rel = asset.output_path.relative_to(project_root).as_posix()
        primary = source_record(source_root, "primary", asset.primary_source)
        common = {
            "category": asset.category, "purpose": asset.purpose,
            "evidenceStatus": asset.evidence_status,
            "sourceRelativePath": primary["relativePath"], "sourceBytes": primary["bytes"],
            "sourceSha256": primary["sha256"],
            "supportingSources": [source_record(source_root, r, p) for r, p in asset.supporting_sources],
            "parameters": asset.parameters, "evidence": asset.evidence,
        }
        entries.append({**common, "outputRelativePath": out_rel, "outputBytes": len(asset.output_bytes),
                        "outputSha256": sha256_bytes(asset.output_bytes),
                        "transformation": asset.transformation,
                        "notes": "Unity runtime UI asset for the invasion stage-select screen."})
        entries.append({**common, "outputRelativePath": f"{out_rel}.meta", "outputBytes": len(asset.meta_bytes),
                        "outputSha256": sha256_bytes(asset.meta_bytes),
                        "transformation": "deterministic-unity-importer-meta",
                        "notes": "Deterministic GUID and import settings generated by this script."})
    identity_path = project_root / SOURCE_IDENTITY_RELATIVE
    manifest_path = project_root / OUTPUT_MANIFEST_RELATIVE
    generated_utc = datetime.now(timezone.utc).isoformat()
    if manifest_path.is_file():
        generated_utc = read_json(manifest_path).get("generatedUtc", generated_utc)
    return {
        "schemaVersion": 1, "generatedUtc": generated_utc, "scope": "issue10-stage-select-ui",
        "sourceIdentity": {"manifestRelativePath": SOURCE_IDENTITY_RELATIVE.as_posix(),
                           "sha256": sha256(identity_path)},
        "entries": sorted(entries, key=lambda e: e["outputRelativePath"]),
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--source-root", type=Path)
    parser.add_argument("--validate-only", action="store_true")
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    project_root = args.project_root.resolve()
    source_root = resolve_source_root(project_root, args.source_root)
    validate_source_identity(project_root, source_root)
    raw_root = source_root / RAW_ASSETS_RELATIVE
    if not raw_root.is_dir():
        raise FileNotFoundError(raw_root)

    ctx = load_atlas_context(raw_root, "Stage")
    common_icons_ctx = load_atlas_context(raw_root, "CommonIcons")
    common_ui_ctx = load_atlas_context(raw_root, "CommonUI")
    state_icons_ctx = load_atlas_context(raw_root, "StateIcons")
    prepared: list[PreparedAsset] = [crop_atlas_sprite(project_root, raw_root, ctx, n) for n in ATLAS_SPRITES]
    prepared += [
        crop_atlas_sprite(
            project_root,
            raw_root,
            common_ui_ctx,
            name,
            COMMON_UI_SPRITE_DIR,
            "CommonUI",
        )
        for name in COMMON_UI_SPRITES
    ]
    prepared += [
        crop_atlas_sprite(
            project_root,
            raw_root,
            common_icons_ctx,
            name,
            COMMON_ICON_SPRITE_DIR,
            "CommonIcons",
        )
        for name in COMMON_ICON_SPRITES
    ]
    prepared += [
        crop_atlas_sprite(
            project_root,
            raw_root,
            state_icons_ctx,
            name,
            STATE_ICON_SPRITE_DIR,
            "StateIcons",
        )
        for name in STATE_ICON_SPRITES
    ]
    prepared += [copy_raw_sprite(project_root, raw_root, STAGELIST_RAW_DIR, n, purpose, status) for n, purpose, status in RAW_SPRITES]
    validate_conflicts(prepared)

    if args.validate_only:
        print(f"Validated and planned {len(prepared)} stage-select resources; no files written.")
        for a in prepared:
            print(f"  {a.evidence_status:9} {a.output_path.name}")
        return

    write_outputs(prepared)
    manifest = make_manifest(project_root, source_root, prepared)
    manifest_path = project_root / OUTPUT_MANIFEST_RELATIVE
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
                             encoding="utf-8", newline="\n")
    print(f"Recovered {len(prepared)} stage-select resources -> {manifest_path}")


if __name__ == "__main__":
    main()
