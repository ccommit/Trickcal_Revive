#!/usr/bin/env python3
"""Recover the 15 approved issue2 lobby resources from read-only reference data."""

from __future__ import annotations

import argparse
import hashlib
import io
import json
import os
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

try:
    from PIL import Image
except ImportError as exception:  # pragma: no cover - environment diagnostic
    raise SystemExit("Pillow is required. Pass a Python 3 executable with Pillow installed.") from exception


RAW_ASSETS_RELATIVE = Path("03_extracted_resources/02_raw_data/Assets")
SOURCE_IDENTITY_RELATIVE = Path("Recovery/Manifests/source-identity.json")
OUTPUT_SPRITES_RELATIVE = Path("Assets/Game/Content/UI/Lobby/Sprites")
OUTPUT_BACKGROUND_RELATIVE = Path("Assets/Game/Content/UI/Lobby/Background")
OUTPUT_MANIFEST_RELATIVE = Path("Recovery/Manifests/lobby-resources.json")


@dataclass(frozen=True)
class SpriteSpec:
    output_name: str
    source_relative: str
    purpose: str
    evidence_status: str = "confirmed"


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


# Currency_Elif_Alternate is deliberately absent: it was unconfirmed and not used by the lobby.
SPECS = (
    SpriteSpec("Currency_Macaron", "02.resources/99.common/sprites/currencyicons/CurrencyIcon_0007.json", "top-currency-macaron"),
    SpriteSpec("Currency_Gold", "02.resources/99.common/sprites/currencyicons/CurrencyIcon_0008.json", "top-currency-gold"),
    SpriteSpec("Currency_Elif", "02.resources/99.common/sprites/currencyicons/CurrencyIcon_0009.json", "top-currency-elif", "inferred"),
    SpriteSpec("Currency_Stamina", "02.resources/99.common/sprites/currencyicons/CurrencyIcon_0011.json", "top-currency-stamina"),
    SpriteSpec("TopMenu_ButtonBase", "02.resources/99.common/sprites/commonicons/TopMenu_ButtonBase.json", "top-menu-button-base"),
    SpriteSpec("TopMenu_IconMenu", "02.resources/99.common/sprites/commonicons/TopMenu_IconMenu.json", "top-menu-icon"),
    SpriteSpec("TopMenu_CurrencyBase", "02.resources/99.common/sprites/commonicons/TopMenu_CurrencyBase.json", "top-currency-base"),
    SpriteSpec("TopMenu_Plus", "02.resources/99.common/sprites/commonicons/TopMenu_Plus.json", "top-currency-plus"),
    SpriteSpec("MainLobby_UserInfoBase", "02.resources/01.mainlobby/sprites/mainlobby/MainLobby_UserInfoBase.json", "player-info-base"),
    SpriteSpec("MainLobby_LevelBase", "02.resources/01.mainlobby/sprites/mainlobby/MainLobby_LevelBase.json", "player-level-base"),
    SpriteSpec("MainLobby_BtnBg", "02.resources/01.mainlobby/sprites/mainlobby/MainLobby_BtnBg.json", "bottom-navigation-base"),
    SpriteSpec("Lobby_GachaButton", "02.resources/01.mainlobby/sprites/mainlobby/GachaButton.json", "bottom-navigation-recruit"),
    SpriteSpec("Lobby_HeroButton", "02.resources/01.mainlobby/sprites/mainlobby/HeroButton.json", "bottom-navigation-apostle"),
    SpriteSpec("Lobby_StoryButton", "02.resources/01.mainlobby/sprites/mainlobby/StoryButton.json", "bottom-navigation-adventure"),
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--source-root", type=Path)
    parser.add_argument("--validate-only", action="store_true")
    return parser.parse_args()


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


def resolve_source_root(project_root: Path, explicit_root: Path | None) -> Path:
    candidates: list[Path] = []
    if explicit_root is not None:
        candidates.append(explicit_root)
    environment_root = os.environ.get("TRICKCAL_REFERENCE_ROOT")
    if environment_root:
        candidates.append(Path(environment_root))
    settings_path = project_root / "UserSettings/TrickcalRecoverySettings.json"
    if settings_path.is_file():
        settings = read_json(settings_path)
        if settings.get("sourceRoot"):
            candidates.append(Path(settings["sourceRoot"]))

    for candidate in candidates:
        resolved = candidate.resolve()
        if (resolved / "00_original_apk/trickcal.apk").is_file() and (resolved / "file_manifest.csv").is_file():
            return resolved
    raise RuntimeError(
        "Recovery source was not found. Configure TRICKCAL_REFERENCE_ROOT or UserSettings/TrickcalRecoverySettings.json."
    )


def validate_source_identity(project_root: Path, source_root: Path) -> None:
    identity_path = project_root / SOURCE_IDENTITY_RELATIVE
    identity = read_json(identity_path)
    failures: list[str] = []
    for expected in identity["files"]:
        source_path = source_root / expected["relativePath"]
        if not source_path.is_file():
            failures.append(f"missing: {expected['relativePath']}")
            continue
        actual_size = source_path.stat().st_size
        actual_hash = sha256(source_path)
        if actual_size != expected["bytes"] or actual_hash != expected["sha256"]:
            failures.append(
                f"identity mismatch: {expected['relativePath']} "
                f"(bytes {actual_size}, sha256 {actual_hash})"
            )
    if failures:
        raise RuntimeError("Reference identity validation failed:\n" + "\n".join(failures))


def json_key(value: dict[str, Any]) -> str:
    return json.dumps(value, sort_keys=True, separators=(",", ":"))


def find_atlas_json(raw_root: Path, tag: str) -> Path:
    matches = [
        path
        for path in (raw_root / "02.resources/addressableresources").rglob(f"{tag}.json")
        if not path.name.startswith("_low_")
    ]
    if len(matches) != 1:
        raise RuntimeError(f"Expected exactly one atlas JSON for {tag}, found {len(matches)}")
    return matches[0]


def find_atlas_pages(raw_root: Path, tag: str) -> list[Path]:
    pages = sorted(
        path
        for path in (raw_root / "Texture2D").glob(f"*{tag}*.png")
        if "-var-" not in path.name
    )
    if not pages:
        raise RuntimeError(f"No source atlas texture found for {tag}")
    return pages


def make_meta(guid: str, pivot: dict[str, Any], pixels_per_unit: float) -> bytes:
    content = f"""fileFormatVersion: 2
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
  spritePixelsToUnits: {pixels_per_unit}
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
"""
    return content.encode("utf-8")


def prepare_sprites(project_root: Path, raw_root: Path) -> list[PreparedAsset]:
    atlas_cache: dict[str, tuple[Path, dict[str, dict[str, Any]], list[tuple[Path, Image.Image]]]] = {}
    prepared: list[PreparedAsset] = []

    for spec in SPECS:
        sprite_path = raw_root / spec.source_relative
        if not sprite_path.is_file():
            raise FileNotFoundError(sprite_path)
        sprite = read_json(sprite_path)
        atlas_tags = sprite.get("m_AtlasTags", [])
        if len(atlas_tags) != 1:
            raise RuntimeError(f"Expected one atlas tag for {sprite_path}, got {atlas_tags}")
        atlas_tag = atlas_tags[0]

        if atlas_tag not in atlas_cache:
            atlas_path = find_atlas_json(raw_root, atlas_tag)
            render_map = {
                json_key(entry["Key"]): entry["Value"]
                for entry in read_json(atlas_path)["m_RenderDataMap"]
            }
            pages = [(path, Image.open(path).convert("RGBA")) for path in find_atlas_pages(raw_root, atlas_tag)]
            atlas_cache[atlas_tag] = atlas_path, render_map, pages

        atlas_path, render_map, pages = atlas_cache[atlas_tag]
        render_data = render_map.get(json_key(sprite["m_RenderDataKey"]))
        if render_data is None:
            raise RuntimeError(f"No atlas render data for {sprite_path}")
        if render_data.get("m_SettingsRaw") not in (3, 67):
            raise RuntimeError(
                f"Unsupported packed rotation for {sprite_path}: {render_data.get('m_SettingsRaw')}"
            )

        rect = render_data["m_TextureRect"]
        x, y = round(rect["m_X"]), round(rect["m_Y"])
        width, height = round(rect["m_Width"]), round(rect["m_Height"])
        crop: Image.Image | None = None
        source_texture: Path | None = None
        for page_path, page in pages:
            if x + width > page.width or y + height > page.height:
                continue
            candidate = page.crop((x, page.height - y - height, x + width, page.height - y))
            if candidate.getbbox() is not None:
                crop = candidate
                source_texture = page_path
                break
        if crop is None or source_texture is None:
            raise RuntimeError(f"Recovered crop is empty or outside atlas for {sprite_path}")

        buffer = io.BytesIO()
        crop.save(buffer, format="PNG", optimize=True)
        output_path = project_root / OUTPUT_SPRITES_RELATIVE / f"{spec.output_name}.png"
        guid_seed = f"trickcal-recovery-lobby:{output_path.relative_to(project_root).as_posix()}"
        guid = hashlib.md5(guid_seed.encode("utf-8")).hexdigest()
        prepared.append(
            PreparedAsset(
                category="lobby-ui",
                purpose=spec.purpose,
                evidence_status=spec.evidence_status,
                primary_source=source_texture,
                supporting_sources=[("sprite-serialization", sprite_path), ("sprite-atlas-serialization", atlas_path)],
                output_path=output_path,
                output_bytes=buffer.getvalue(),
                meta_bytes=make_meta(guid, sprite["m_Pivot"], sprite["m_PixelsToUnits"]),
                transformation="crop-spriteatlas-page",
                parameters={
                    "atlasTag": atlas_tag,
                    "rect": {"x": x, "y": y, "width": width, "height": height},
                    "coordinateConvention": "Unity bottom-left converted to PNG top-left",
                    "pivot": sprite["m_Pivot"],
                    "pixelsPerUnit": sprite["m_PixelsToUnits"],
                },
                evidence="Serialized Sprite m_RenderDataKey matched the selected SpriteAtlas render map.",
            )
        )

    return prepared


def prepare_background(project_root: Path, raw_root: Path) -> PreparedAsset:
    source = raw_root / "02.resources/addressableresources/rawsprites/lobbystyle/Lobby_10001_1.png"
    if not source.is_file():
        raise FileNotFoundError(source)
    output = project_root / OUTPUT_BACKGROUND_RELATIVE / "Lobby_Default.png"
    guid_seed = f"trickcal-recovery-lobby:{output.relative_to(project_root).as_posix()}"
    guid = hashlib.md5(guid_seed.encode("utf-8")).hexdigest()
    return PreparedAsset(
        category="lobby-background",
        purpose="lobby-background",
        evidence_status="confirmed",
        primary_source=source,
        supporting_sources=[],
        output_path=output,
        output_bytes=source.read_bytes(),
        meta_bytes=make_meta(guid, {"m_X": 0.5, "m_Y": 0.5}, 100),
        transformation="byte-for-byte-copy",
        parameters={"pivot": {"m_X": 0.5, "m_Y": 0.5}, "pixelsPerUnit": 100},
        evidence="Selected raw lobby style image; output bytes must match the source exactly.",
    )


def validate_conflicts(prepared: list[PreparedAsset]) -> None:
    for asset in prepared:
        expected = ((asset.output_path, asset.output_bytes), (Path(f"{asset.output_path}.meta"), asset.meta_bytes))
        for path, content in expected:
            if path.exists() and sha256(path) != sha256_bytes(content):
                raise RuntimeError(f"Generated asset conflict; existing file was kept: {path}")


def write_outputs(prepared: list[PreparedAsset]) -> None:
    for asset in prepared:
        asset.output_path.parent.mkdir(parents=True, exist_ok=True)
        asset.output_path.write_bytes(asset.output_bytes)
        Path(f"{asset.output_path}.meta").write_bytes(asset.meta_bytes)


def source_record(source_root: Path, role: str, path: Path) -> dict[str, Any]:
    return {
        "role": role,
        "relativePath": path.relative_to(source_root).as_posix(),
        "bytes": path.stat().st_size,
        "sha256": sha256(path),
    }


def make_manifest(project_root: Path, source_root: Path, prepared: list[PreparedAsset]) -> dict[str, Any]:
    entries: list[dict[str, Any]] = []
    for asset in prepared:
        output_relative = asset.output_path.relative_to(project_root).as_posix()
        primary = source_record(source_root, "primary", asset.primary_source)
        supporting = [source_record(source_root, role, path) for role, path in asset.supporting_sources]
        common = {
            "category": asset.category,
            "purpose": asset.purpose,
            "evidenceStatus": asset.evidence_status,
            "sourceRelativePath": primary["relativePath"],
            "sourceBytes": primary["bytes"],
            "sourceSha256": primary["sha256"],
            "supportingSources": supporting,
            "transformation": asset.transformation,
            "parameters": asset.parameters,
            "evidence": asset.evidence,
        }
        entries.append(
            {
                **common,
                "outputRelativePath": output_relative,
                "outputBytes": len(asset.output_bytes),
                "outputSha256": sha256_bytes(asset.output_bytes),
                "notes": "Unity runtime resource; ready for Recovery Harness integration.",
            }
        )
        entries.append(
            {
                **common,
                "outputRelativePath": f"{output_relative}.meta",
                "outputBytes": len(asset.meta_bytes),
                "outputSha256": sha256_bytes(asset.meta_bytes),
                "transformation": "deterministic-unity-textureimporter-meta",
                "notes": "Deterministic GUID and sprite import settings generated by this script.",
            }
        )

    identity_path = project_root / SOURCE_IDENTITY_RELATIVE
    return {
        "schemaVersion": 1,
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "sourceIdentity": {
            "manifestRelativePath": SOURCE_IDENTITY_RELATIVE.as_posix(),
            "sha256": sha256(identity_path),
        },
        "entries": sorted(entries, key=lambda entry: entry["outputRelativePath"]),
    }


def main() -> None:
    args = parse_args()
    project_root = args.project_root.resolve()
    source_root = resolve_source_root(project_root, args.source_root)
    validate_source_identity(project_root, source_root)
    raw_root = source_root / RAW_ASSETS_RELATIVE
    if not raw_root.is_dir():
        raise FileNotFoundError(raw_root)

    prepared = prepare_sprites(project_root, raw_root)
    prepared.append(prepare_background(project_root, raw_root))
    if len(prepared) != 15:
        raise RuntimeError(f"Expected exactly 15 approved lobby resources, got {len(prepared)}")
    validate_conflicts(prepared)

    if args.validate_only:
        print(f"Validated source identity and planned {len(prepared)} lobby resources; no files were written.")
        return

    write_outputs(prepared)
    manifest = make_manifest(project_root, source_root, prepared)
    manifest_path = project_root / OUTPUT_MANIFEST_RELATIVE
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    manifest_path.write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    print(f"Recovered {len(prepared)} approved lobby resources: {manifest_path}")


if __name__ == "__main__":
    main()
