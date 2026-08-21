#!/usr/bin/env python3
"""Recover the minimum Deck/CommonUI sprites used by PartySetup.

The reference tree is read-only evidence. This script crops serialized Unity
Sprite entries from their canonical SpriteAtlas pages into generated Unity
import copies, with deterministic GUIDs and a SHA-256 manifest.
"""

from __future__ import annotations

import argparse
import hashlib
import importlib.util
import io
import json
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

sys.dont_write_bytecode = True


def load_shared(project_root: Path):
    helper_path = project_root / "Tools/Recovery/Recover-InvasionUI.py"
    spec = importlib.util.spec_from_file_location("recover_invasion_ui", helper_path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Cannot load recovery helpers: {helper_path}")
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


OUTPUT_RELATIVE = Path("Assets/Game/Content/UI/PartySetup")
MANIFEST_RELATIVE = Path("Recovery/Manifests/party-setup-ui.json")

SPRITE_GROUPS = {
    "Deck": {
        "directory": "02.resources/03.deck/sprites",
        "names": [
            "Deck_PositionBg",
            "Deck_PositionTile",
            "Deck_ListDim",
            "Deck_IconClearDeck",
            "Deck_AutoForbiddenIcon",
            "Deck_Icon_Skip",
            "Deck_Artifact",
            "Deck_WhiteBox",
            "Graduate_Skill_Icon",
        ],
    },
    "CommonUI": {
        "directory": "02.resources/99.common/sprites/commonui",
        "names": [
            "Common_MajorBtn_002",
            "Common_MajorBtn_010",
            "Common_MajorBtn_014",
            "Common_MinorBtn_3030_001",
            "Common_MinorBtn_3030_011",
            "CommonHalf_Top_4040",
            "CommonBox_6060",
        ],
    },
}

BACKGROUND_ATLAS_DIR = "02.resources/addressableresources/background/atlases"
BACKGROUND_SPRITE_DIR = "02.resources/addressableresources/background/stage/bg_stage1_1"
BACKGROUND_SPRITES = [f"BG_Stage1_1_{index}" for index in range(1, 6)]


def deterministic_guid(project_root: Path, output_path: Path) -> str:
    relative = output_path.relative_to(project_root).as_posix()
    return hashlib.md5(f"trickcal-recovery-party-setup-ui:{relative}".encode("utf-8")).hexdigest()


def crop_sprite(shared: Any, project_root: Path, raw_root: Path, atlas: Any,
                atlas_tag: str, sprite_dir: str, name: str,
                allowed_settings: tuple[int, ...] = (3, 67)):
    sprite_path = raw_root / sprite_dir / f"{name}.json"
    if not sprite_path.is_file():
        raise FileNotFoundError(sprite_path)
    sprite = shared.read_json(sprite_path)
    if sprite.get("m_AtlasTags", []) != [atlas_tag]:
        raise RuntimeError(f"Unexpected atlas tags for {name}: {sprite.get('m_AtlasTags')}")

    render_data = atlas.render_map.get(shared.json_key(sprite["m_RenderDataKey"]))
    if render_data is None:
        raise RuntimeError(f"No atlas render data for {name}")
    if render_data.get("m_SettingsRaw") not in allowed_settings:
        raise RuntimeError(f"Unsupported packing for {name}: {render_data.get('m_SettingsRaw')}")

    texture_key = shared.json_key(render_data["m_Texture"])
    page = atlas.pages[texture_key]
    rect = render_data["m_TextureRect"]
    x, y = round(rect["m_X"]), round(rect["m_Y"])
    width, height = round(rect["m_Width"]), round(rect["m_Height"])
    if x < 0 or y < 0 or x + width > page.width or y + height > page.height:
        raise RuntimeError(f"Rect outside page for {name}")

    cropped = page.crop((x, page.height - y - height, x + width, page.height - y))
    if cropped.getbbox() is None:
        raise RuntimeError(f"Empty crop for {name}")
    output = io.BytesIO()
    cropped.save(output, format="PNG", optimize=True)

    output_path = project_root / OUTPUT_RELATIVE / f"{name}.png"
    return shared.PreparedAsset(
        category="party-setup-ui",
        purpose=name,
        evidence_status="confirmed",
        primary_source=raw_root / shared.TEXTURE2D_DIR / atlas.page_names[texture_key],
        supporting_sources=[("sprite-serialization", sprite_path)],
        output_path=output_path,
        output_bytes=output.getvalue(),
        meta_bytes=shared.make_texture_meta(
            deterministic_guid(project_root, output_path),
            sprite["m_Pivot"],
            sprite["m_PixelsToUnits"],
            sprite.get("m_Border"),
        ),
        transformation="crop-spriteatlas-page",
        parameters={
            "atlasTag": atlas_tag,
            "atlasPageFileName": atlas.page_names[texture_key],
            "rect": {"x": x, "y": y, "width": width, "height": height},
            "settingsRaw": render_data.get("m_SettingsRaw"),
            "coordinateConvention": "Unity bottom-left converted to PNG top-left",
            "pivot": sprite["m_Pivot"],
            "pixelsPerUnit": sprite["m_PixelsToUnits"],
            "border": sprite.get("m_Border"),
        },
        evidence=(
            f"Serialized Sprite m_RenderDataKey matched the {atlas_tag} atlas render map; "
            "native border and pivot retained in the generated importer meta."
        ),
    )


def make_manifest(shared: Any, project_root: Path, source_root: Path, prepared: list[Any]) -> dict[str, Any]:
    entries = []
    for asset in prepared:
        output_relative = asset.output_path.relative_to(project_root).as_posix()
        primary = shared.source_record(source_root, "primary", asset.primary_source)
        common = {
            "category": asset.category,
            "purpose": asset.purpose,
            "evidenceStatus": asset.evidence_status,
            "sourceRelativePath": primary["relativePath"],
            "sourceBytes": primary["bytes"],
            "sourceSha256": primary["sha256"],
            "supportingSources": [
                shared.source_record(source_root, role, path)
                for role, path in asset.supporting_sources
            ],
            "parameters": asset.parameters,
            "evidence": asset.evidence,
        }
        entries.append({
            **common,
            "outputRelativePath": output_relative,
            "outputBytes": len(asset.output_bytes),
            "outputSha256": shared.sha256_bytes(asset.output_bytes),
            "transformation": asset.transformation,
            "notes": "Generated Unity runtime UI asset for the party-setup screen.",
        })
        entries.append({
            **common,
            "outputRelativePath": f"{output_relative}.meta",
            "outputBytes": len(asset.meta_bytes),
            "outputSha256": shared.sha256_bytes(asset.meta_bytes),
            "transformation": "deterministic-unity-importer-meta",
            "notes": "Deterministic GUID and original border/pivot import settings.",
        })

    manifest_path = project_root / MANIFEST_RELATIVE
    generated_utc = datetime.now(timezone.utc).isoformat()
    if manifest_path.is_file():
        generated_utc = shared.read_json(manifest_path).get("generatedUtc", generated_utc)
    identity_path = project_root / shared.SOURCE_IDENTITY_RELATIVE
    return {
        "schemaVersion": 1,
        "generatedUtc": generated_utc,
        "scope": "issue10-party-setup-ui",
        "sourceIdentity": {
            "manifestRelativePath": shared.SOURCE_IDENTITY_RELATIVE.as_posix(),
            "sha256": shared.sha256(identity_path),
        },
        "entries": sorted(entries, key=lambda entry: entry["outputRelativePath"]),
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--source-root", type=Path)
    parser.add_argument("--validate-only", action="store_true")
    parser.add_argument("--apply", action="store_true")
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    if args.validate_only == args.apply:
        raise RuntimeError("Choose exactly one of --validate-only or --apply")

    project_root = args.project_root.resolve()
    shared = load_shared(project_root)
    source_root = shared.resolve_source_root(project_root, args.source_root)
    shared.validate_source_identity(project_root, source_root)
    raw_root = source_root / shared.RAW_ASSETS_RELATIVE

    prepared = []
    for atlas_tag, group in SPRITE_GROUPS.items():
        atlas = shared.load_atlas_context(raw_root, atlas_tag)
        prepared.extend(
            crop_sprite(shared, project_root, raw_root, atlas, atlas_tag, group["directory"], name)
            for name in group["names"]
        )
    background_atlas = shared.load_atlas_context(raw_root, "BG_Stage1_1", BACKGROUND_ATLAS_DIR)
    prepared.extend(
        crop_sprite(
            shared,
            project_root,
            raw_root,
            background_atlas,
            "BG_Stage1_1",
            BACKGROUND_SPRITE_DIR,
            name,
            allowed_settings=(1,),
        )
        for name in BACKGROUND_SPRITES
    )
    shared.validate_conflicts(prepared)

    if args.validate_only:
        print(f"Validated and planned {len(prepared)} party-setup resources; no files written.")
        for asset in prepared:
            print(f"  {asset.parameters['atlasTag']:8} {asset.output_path.name}")
        return

    shared.write_outputs(prepared)
    manifest = make_manifest(shared, project_root, source_root, prepared)
    manifest_path = project_root / MANIFEST_RELATIVE
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    manifest_path.write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    print(f"Recovered {len(prepared)} party-setup resources -> {manifest_path}")


if __name__ == "__main__":
    main()
