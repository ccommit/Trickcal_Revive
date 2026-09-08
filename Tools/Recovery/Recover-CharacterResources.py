#!/usr/bin/env python3
"""Prepare the approved issue2 character assets from read-only reference data."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import struct
from collections import Counter
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Iterable


RAW_ASSETS_RELATIVE = Path("03_extracted_resources/02_raw_data/Assets")
EXPORTED_ASSETS_RELATIVE = Path(
    "03_extracted_resources/01_unity_data/ExportedProject/Assets"
)
CONTENT_RELATIVE = Path("02.resources/addressableresources")
CATALOG_RELATIVE = Path("Recovery/Catalogs/character-roster.json")
SOURCE_IDENTITY_RELATIVE = Path("Recovery/Manifests/source-identity.json")
OUTPUT_ROOT_RELATIVE = Path("Assets/Game/Content/Characters")
OUTPUT_MANIFEST_RELATIVE = Path("Recovery/Manifests/character-resources.json")


@dataclass(frozen=True)
class CharacterSpec:
    key: str
    base_name: str
    kind: str
    standing_skin: str | None = None


@dataclass
class PreparedFile:
    category: str
    purpose: str
    character_key: str
    evidence_status: str
    primary_source: Path
    supporting_sources: tuple[tuple[str, Path], ...]
    output_path: Path
    output_bytes: bytes
    transformation: str
    parameters: dict[str, Any]
    evidence: str


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--project-root", type=Path, default=Path(__file__).resolve().parents[2]
    )
    parser.add_argument("--source-root", type=Path)
    parser.add_argument("--target", action="append", default=[])
    parser.add_argument("--finalize-unity-meta", action="store_true")
    parser.add_argument("--apply", action="store_true")
    return parser.parse_args()


def read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def sha256_bytes(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def resolve_source_root(project_root: Path, explicit_root: Path | None) -> Path:
    candidates: list[Path] = []
    if explicit_root is not None:
        candidates.append(explicit_root)
    environment_root = os.environ.get("TRICKCAL_REFERENCE_ROOT")
    if environment_root:
        candidates.append(Path(environment_root))
    settings_path = project_root / "UserSettings/TrickcalRecoverySettings.json"
    if settings_path.is_file():
        source_setting = read_json(settings_path).get("sourceRoot")
        if source_setting:
            candidates.append(Path(source_setting))

    for candidate in candidates:
        resolved = candidate.resolve()
        if (
            (resolved / "00_original_apk/trickcal.apk").is_file()
            and (resolved / "file_manifest.csv").is_file()
        ):
            return resolved
    raise RuntimeError(
        "Recovery source was not found. Configure TRICKCAL_REFERENCE_ROOT or "
        "UserSettings/TrickcalRecoverySettings.json."
    )


def validate_source_identity(project_root: Path, source_root: Path) -> None:
    identity = read_json(project_root / SOURCE_IDENTITY_RELATIVE)
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


def load_catalog(project_root: Path) -> tuple[dict[str, Any], list[CharacterSpec]]:
    catalog_path = project_root / CATALOG_RELATIVE
    catalog = read_json(catalog_path)
    apostles = [
        CharacterSpec(
            key=entry["key"],
            base_name=entry["baseName"],
            kind="apostle",
            standing_skin=entry.get("standingSkin"),
        )
        for entry in catalog["apostles"]
    ]
    monsters = [
        CharacterSpec(entry["key"], entry["baseName"], "monster")
        for entry in catalog["monsters"]
    ]
    if len(apostles) != 30 or len(monsters) != 4:
        raise RuntimeError(
            f"Catalog boundary changed: expected 30 apostles and 4 monsters, "
            f"found {len(apostles)} and {len(monsters)}."
        )
    keys = [character.key for character in apostles + monsters]
    if len(keys) != len(set(keys)):
        raise RuntimeError("Character keys must be unique.")
    variants = catalog["monsterDisplayVariants"]
    if variants != ["Cool", "Gloomy", "Jolly", "Mad", "Naive"]:
        raise RuntimeError("Monster display variants do not match the approved five variants.")
    return catalog, apostles + monsters


def select_characters(
    characters: list[CharacterSpec], requested: Iterable[str]
) -> tuple[list[CharacterSpec], bool]:
    normalized = {
        item.strip().lower()
        for value in requested
        for item in value.split(",")
        if item.strip()
    }
    if not normalized or normalized == {"all"}:
        return characters, True
    known = {character.key for character in characters}
    unknown = sorted(normalized - known)
    if unknown:
        raise RuntimeError(f"Unknown character target(s): {', '.join(unknown)}")
    return [character for character in characters if character.key in normalized], False


def sanitize_meta(path: Path) -> bytes:
    text = path.read_text(encoding="utf-8-sig")
    text = re.sub(r"(?m)^(\s*assetBundleName:)\s*.*$", r"\1", text)
    text = re.sub(r"(?m)^(\s*assetBundleVariant:)\s*.*$", r"\1", text)
    return (text.rstrip() + "\n").encode("utf-8")


def assert_same_bytes(raw_path: Path, exported_path: Path) -> None:
    if not raw_path.is_file():
        raise FileNotFoundError(raw_path)
    if not exported_path.is_file():
        raise FileNotFoundError(exported_path)
    if raw_path.stat().st_size != exported_path.stat().st_size or sha256(raw_path) != sha256(
        exported_path
    ):
        raise RuntimeError(
            "Raw and Unity-exported reference copies differ: "
            f"{raw_path} <> {exported_path}"
        )


def add_reference_copy(
    prepared: list[PreparedFile],
    *,
    category: str,
    purpose: str,
    character_key: str,
    evidence_status: str,
    raw_path: Path,
    exported_path: Path,
    output_path: Path,
    supporting_sources: tuple[tuple[str, Path], ...] = (),
    transformation: str = "byte-for-byte-copy",
    parameters: dict[str, Any] | None = None,
    evidence: str,
) -> None:
    assert_same_bytes(raw_path, exported_path)
    meta_path = Path(f"{exported_path}.meta")
    if not meta_path.is_file():
        raise FileNotFoundError(meta_path)
    prepared.append(
        PreparedFile(
            category=category,
            purpose=purpose,
            character_key=character_key,
            evidence_status=evidence_status,
            primary_source=raw_path,
            supporting_sources=(
                ("unity-exported-copy", exported_path),
                *supporting_sources,
            ),
            output_path=output_path,
            output_bytes=raw_path.read_bytes(),
            transformation=transformation,
            parameters=parameters or {},
            evidence=evidence,
        )
    )
    prepared.append(
        PreparedFile(
            category=f"{category}-meta",
            purpose=purpose,
            character_key=character_key,
            evidence_status="confirmed",
            primary_source=meta_path,
            supporting_sources=(("asset-primary-source", raw_path),),
            output_path=Path(f"{output_path}.meta"),
            output_bytes=sanitize_meta(meta_path),
            transformation="copy-unity-meta-and-clear-assetbundle-labels",
            parameters={"guidPreserved": True, "assetBundleLabelsCleared": True},
            evidence="Unity-exported importer settings and GUID were preserved; obsolete asset-bundle labels were cleared.",
        )
    )


def png_dimensions(path: Path) -> tuple[int, int]:
    data = path.read_bytes()[:24]
    if len(data) != 24 or data[:8] != b"\x89PNG\r\n\x1a\n" or data[12:16] != b"IHDR":
        raise RuntimeError(f"Invalid PNG header: {path}")
    return struct.unpack(">II", data[16:24])


def prepare_sprite(
    prepared: list[PreparedFile],
    *,
    raw_content: Path,
    exported_content: Path,
    source_relative: Path,
    output_path: Path,
    category: str,
    purpose: str,
    character_key: str,
) -> None:
    raw_png = raw_content / source_relative
    raw_json = raw_png.with_suffix(".json")
    exported_png = exported_content / source_relative
    if not raw_json.is_file():
        raise FileNotFoundError(raw_json)
    serialization = read_json(raw_json)
    if serialization.get("m_AtlasTags"):
        raise RuntimeError(f"Expected a direct Sprite PNG, found atlas tags: {raw_json}")
    width, height = png_dimensions(raw_png)
    rect = serialization.get("m_Rect", {})
    if round(rect.get("m_Width", -1)) != width or round(rect.get("m_Height", -1)) != height:
        raise RuntimeError(f"Sprite serialization dimensions do not match PNG: {raw_json}")
    add_reference_copy(
        prepared,
        category=category,
        purpose=purpose,
        character_key=character_key,
        evidence_status="confirmed",
        raw_path=raw_png,
        exported_path=exported_png,
        output_path=output_path,
        supporting_sources=(("sprite-serialization", raw_json),),
        parameters={
            "width": width,
            "height": height,
            "pivot": serialization["m_Pivot"],
            "pixelsPerUnit": serialization["m_PixelsToUnits"],
        },
        evidence="Direct PNG, Sprite serialization, and Unity-exported importer settings agree.",
    )


def prepare_apostle_presentation(
    prepared: list[PreparedFile],
    project_root: Path,
    raw_content: Path,
    exported_content: Path,
    character: CharacterSpec,
) -> None:
    output = project_root / OUTPUT_ROOT_RELATIVE / "Apostles" / character.key / "Presentation"
    specs = (
        (
            Path("rawsprites/dialogue/herohead") / f"{character.base_name}.png",
            output / "Portrait.png",
            "character-portrait",
            "portrait",
        ),
        (
            Path("rawsprites/heroicons") / f"{character.base_name}.png",
            output / "RosterIcon.png",
            "character-roster-icon",
            "roster-icon",
        ),
        (
            Path("rawsprites/skillicons") / f"Icon_AdmissionSkill_{character.base_name}.png",
            output / "AdmissionSkill.png",
            "character-skill-icon",
            "admission-skill-icon",
        ),
        (
            Path("rawsprites/skillicons") / f"Icon_GraduateSkill_{character.base_name}.png",
            output / "GraduateSkill.png",
            "character-skill-icon",
            "graduate-skill-icon",
        ),
    )
    for relative, destination, category, purpose in specs:
        prepare_sprite(
            prepared,
            raw_content=raw_content,
            exported_content=exported_content,
            source_relative=relative,
            output_path=destination,
            category=category,
            purpose=purpose,
            character_key=character.key,
        )


def prepare_monster_presentation(
    prepared: list[PreparedFile],
    project_root: Path,
    raw_content: Path,
    exported_content: Path,
    character: CharacterSpec,
    variants: list[str],
) -> None:
    output = project_root / OUTPUT_ROOT_RELATIVE / "Monsters" / character.key / "Presentation"
    for variant in variants:
        source_name = f"Icon_{character.base_name}{variant}.png"
        prepare_sprite(
            prepared,
            raw_content=raw_content,
            exported_content=exported_content,
            source_relative=Path("rawsprites/monster") / source_name,
            output_path=output / f"{variant}.png",
            category="monster-roster-icon",
            purpose=f"registry-variant-{variant.lower()}",
            character_key=character.key,
        )


def spine_version(path: Path) -> str:
    match = re.search(rb"4\.1\.\d+", path.read_bytes()[:512])
    if not match:
        raise RuntimeError(f"Spine 4.1 export marker was not found: {path}")
    return match.group(0).decode("ascii")


def atlas_page_names(content: bytes) -> list[str]:
    text = content.decode("utf-8-sig")
    pages: list[str] = []
    expect_page = True
    for line in text.splitlines():
        stripped = line.strip()
        if not stripped:
            expect_page = True
            continue
        if expect_page:
            pages.append(stripped)
            expect_page = False
    return pages


def prepare_spine_set(
    prepared: list[PreparedFile],
    project_root: Path,
    raw_content: Path,
    exported_content: Path,
    character: CharacterSpec,
    presentation: str,
) -> None:
    kind_segment = "hero" if character.kind == "apostle" else "monster"
    if presentation == "Standing":
        source_dir = Path("spine/standing") / character.key
    else:
        source_dir = Path("spine/ingame") / kind_segment / character.key
    raw_dir = raw_content / source_dir
    exported_dir = exported_content / source_dir
    output_kind = "Apostles" if character.kind == "apostle" else "Monsters"
    output_dir = (
        project_root
        / OUTPUT_ROOT_RELATIVE
        / output_kind
        / character.key
        / "Spine"
        / presentation
    )

    skeleton_raw = raw_dir / f"{character.base_name}.skel.bytes"
    atlas_raw = raw_dir / f"{character.base_name}.atlas.bytes"
    texture_raw = raw_dir / f"{character.base_name}.png"
    skeleton_exported = exported_dir / f"{character.base_name}.skel.bytes"
    atlas_exported = exported_dir / f"{character.base_name}.atlas.txt"
    texture_exported = exported_dir / f"{character.base_name}.png"
    version = spine_version(skeleton_raw)
    pages = atlas_page_names(atlas_raw.read_bytes())
    if pages != [texture_raw.name]:
        raise RuntimeError(
            f"Spine atlas page mismatch for {character.key}/{presentation}: {pages}"
        )

    common = {
        "characterKey": character.key,
        "presentation": presentation,
        "spineVersion": version,
        "atlasPages": pages,
        "standingSkin": character.standing_skin,
    }
    add_reference_copy(
        prepared,
        category="spine-skeleton",
        purpose=f"{presentation.lower()}-skeleton",
        character_key=character.key,
        evidence_status="confirmed",
        raw_path=skeleton_raw,
        exported_path=skeleton_exported,
        output_path=output_dir / skeleton_raw.name,
        parameters=common,
        evidence="Raw skeleton bytes match the Unity-exported copy and declare a Spine 4.1 version.",
    )
    add_reference_copy(
        prepared,
        category="spine-atlas",
        purpose=f"{presentation.lower()}-atlas",
        character_key=character.key,
        evidence_status="confirmed",
        raw_path=atlas_raw,
        exported_path=atlas_exported,
        output_path=output_dir / f"{character.base_name}.atlas.txt",
        transformation="byte-for-byte-copy-and-unity-import-extension",
        parameters=common,
        evidence="Raw atlas bytes match the Unity-exported .atlas.txt copy and name the selected texture page.",
    )
    add_reference_copy(
        prepared,
        category="spine-texture",
        purpose=f"{presentation.lower()}-texture",
        character_key=character.key,
        evidence_status="confirmed",
        raw_path=texture_raw,
        exported_path=texture_exported,
        output_path=output_dir / texture_raw.name,
        parameters=common,
        evidence="Raw texture bytes match the Unity-exported copy selected by the atlas page.",
    )


def prepare_audio_directory(
    prepared: list[PreparedFile],
    *,
    project_root: Path,
    raw_content: Path,
    exported_content: Path,
    character: CharacterSpec,
    source_relative: Path,
    output_relative: Path,
    category: str,
    purpose: str,
    include_pattern: re.Pattern[str] | None,
    excluded_pattern: re.Pattern[str] | None,
    evidence_status: str,
) -> int:
    raw_dir = raw_content / source_relative
    exported_dir = exported_content / source_relative
    if not raw_dir.is_dir():
        raise FileNotFoundError(raw_dir)
    selected = sorted(
        (
            path
            for path in raw_dir.glob("*.ogg")
            if (include_pattern is None or include_pattern.search(path.name))
            and (excluded_pattern is None or not excluded_pattern.search(path.name))
        ),
        key=lambda path: path.name,
    )
    if not selected:
        raise RuntimeError(f"No approved audio selected from {raw_dir}")
    for raw_path in selected:
        if raw_path.read_bytes()[:4] != b"OggS":
            raise RuntimeError(f"Invalid Ogg header: {raw_path}")
        output_kind = "Apostles" if character.kind == "apostle" else "Monsters"
        destination = (
            project_root
            / OUTPUT_ROOT_RELATIVE
            / output_kind
            / character.key
            / output_relative
            / raw_path.name
        )
        add_reference_copy(
            prepared,
            category=category,
            purpose=purpose,
            character_key=character.key,
            evidence_status=evidence_status,
            raw_path=raw_path,
            exported_path=exported_dir / raw_path.name,
            output_path=destination,
            parameters={
                "selection": "catalog-regex-and-base-character-directory",
                "ordering": "ordinal-file-name",
            },
            evidence=(
                "File is in the selected base character audio directory and matches the approved battle naming boundary."
            ),
        )
    return len(selected)


def prepare_character(
    prepared: list[PreparedFile],
    counts: Counter[str],
    *,
    project_root: Path,
    raw_content: Path,
    exported_content: Path,
    catalog: dict[str, Any],
    character: CharacterSpec,
) -> None:
    if character.kind == "apostle":
        prepare_apostle_presentation(
            prepared, project_root, raw_content, exported_content, character
        )
    else:
        prepare_monster_presentation(
            prepared,
            project_root,
            raw_content,
            exported_content,
            character,
            catalog["monsterDisplayVariants"],
        )
    counts[f"{character.kind}-presentation"] += 4 if character.kind == "apostle" else 5

    prepare_spine_set(
        prepared,
        project_root,
        raw_content,
        exported_content,
        character,
        "InGame",
    )
    counts[f"{character.kind}-spine-set"] += 1
    if character.kind == "apostle":
        prepare_spine_set(
            prepared,
            project_root,
            raw_content,
            exported_content,
            character,
            "Standing",
        )
        counts["apostle-spine-set"] += 1

    excluded = re.compile(catalog["audioSelection"]["excludedNameRegex"], re.IGNORECASE)
    if character.kind == "apostle":
        voice_pattern = re.compile(
            catalog["audioSelection"]["apostleBattleVoiceNameRegex"], re.IGNORECASE
        )
        counts["apostle-battle-voice"] += prepare_audio_directory(
            prepared,
            project_root=project_root,
            raw_content=raw_content,
            exported_content=exported_content,
            character=character,
            source_relative=Path("audio/kor/voice/hero") / character.key,
            output_relative=Path("Audio/Voice/Battle"),
            category="battle-voice-ko",
            purpose="battle-voice",
            include_pattern=voice_pattern,
            excluded_pattern=excluded,
            evidence_status="inferred",
        )
        counts["apostle-battle-sfx"] += prepare_audio_directory(
            prepared,
            project_root=project_root,
            raw_content=raw_content,
            exported_content=exported_content,
            character=character,
            source_relative=Path("audio/sfx/hero") / character.key,
            output_relative=Path("Audio/Sfx"),
            category="battle-sfx",
            purpose="battle-sfx",
            include_pattern=None,
            excluded_pattern=excluded,
            evidence_status="confirmed",
        )
    else:
        counts["monster-battle-voice"] += prepare_audio_directory(
            prepared,
            project_root=project_root,
            raw_content=raw_content,
            exported_content=exported_content,
            character=character,
            source_relative=Path("audio/kor/voice/monster") / character.key,
            output_relative=Path("Audio/Voice/Battle"),
            category="battle-voice-ko",
            purpose="battle-voice",
            include_pattern=None,
            excluded_pattern=excluded,
            evidence_status="confirmed",
        )
        counts["monster-battle-sfx"] += prepare_audio_directory(
            prepared,
            project_root=project_root,
            raw_content=raw_content,
            exported_content=exported_content,
            character=character,
            source_relative=Path("audio/sfx/monster") / character.key,
            output_relative=Path("Audio/Sfx"),
            category="battle-sfx",
            purpose="battle-sfx",
            include_pattern=None,
            excluded_pattern=excluded,
            evidence_status="confirmed",
        )


def load_recorded_output_hashes(project_root: Path) -> dict[str, set[str]]:
    recorded: dict[str, set[str]] = {}
    manifest_root = project_root / "Recovery/Manifests"
    for path in manifest_root.glob("character-resources*.json"):
        try:
            manifest = read_json(path)
        except (OSError, json.JSONDecodeError):
            continue
        for entry in manifest.get("entries", []):
            relative = entry.get("outputRelativePath")
            output_hash = entry.get("outputSha256")
            if relative and output_hash:
                recorded.setdefault(relative, set()).add(output_hash)
    return recorded


def meta_guid(content: bytes) -> str | None:
    match = re.search(rb"(?m)^guid:\s*([0-9a-f]{32})\s*$", content)
    return match.group(1).decode("ascii") if match else None


def meta_importer(content: bytes) -> str | None:
    match = re.search(
        rb"(?m)^(TextureImporter|AudioImporter|TextScriptImporter):\s*$", content
    )
    return match.group(1).decode("ascii") if match else None


def accept_unity_normalized_meta(item: PreparedFile, actual: bytes) -> None:
    expected_guid = meta_guid(item.output_bytes)
    actual_guid = meta_guid(actual)
    expected_importer = meta_importer(item.output_bytes)
    actual_importer = meta_importer(actual)
    if (
        expected_guid is None
        or actual_guid != expected_guid
        or expected_importer is None
        or actual_importer != expected_importer
    ):
        raise RuntimeError(
            f"Unity-normalized meta changed GUID or importer type: {item.output_path}"
        )
    text = actual.decode("utf-8-sig")
    bundle_match = re.search(r"(?m)^[ \t]*assetBundleName:[ \t]*(.*)$", text)
    if bundle_match and bundle_match.group(1).strip():
        raise RuntimeError(f"AssetBundle label remains in finalized meta: {item.output_path}")
    item.output_bytes = actual
    item.transformation = "copy-reference-meta-clear-bundle-labels-and-unity-normalize"
    item.parameters = {
        **item.parameters,
        "unityNormalized": True,
    }
    item.evidence = (
        item.evidence
        + " Unity 6000.3.16f1 normalized the serialized importer fields without changing the GUID or importer type."
    )


def validate_output_paths(
    prepared: list[PreparedFile],
    project_root: Path,
    finalize_unity_meta: bool,
) -> None:
    output_paths = [item.output_path for item in prepared]
    if len(output_paths) != len(set(output_paths)):
        duplicates = [
            str(path)
            for path, count in Counter(output_paths).items()
            if count > 1
        ]
        raise RuntimeError("Duplicate output path(s): " + ", ".join(duplicates))

    existing_guids: dict[str, Path] = {}
    selected_meta_paths = {path for path in output_paths if path.suffix == ".meta"}
    assets_root = project_root / "Assets"
    for path in assets_root.rglob("*.meta"):
        if path in selected_meta_paths:
            continue
        match = re.search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$", path.read_text(encoding="utf-8-sig"))
        if match:
            existing_guids[match.group(1)] = path

    planned_guids: dict[str, Path] = {}
    for item in prepared:
        if item.output_path.suffix != ".meta":
            continue
        guid = meta_guid(item.output_bytes)
        if guid is None:
            raise RuntimeError(f"Unity meta is missing a GUID: {item.primary_source}")
        collision = planned_guids.get(guid) or existing_guids.get(guid)
        if collision and collision != item.output_path:
            raise RuntimeError(
                f"GUID collision {guid}: {collision} <> {item.output_path}"
            )
        planned_guids[guid] = item.output_path

    recorded_hashes = load_recorded_output_hashes(project_root)
    for item in prepared:
        if not item.output_path.exists():
            continue
        actual = item.output_path.read_bytes()
        actual_hash = sha256_bytes(actual)
        if actual_hash == sha256_bytes(item.output_bytes):
            continue
        relative = item.output_path.relative_to(project_root).as_posix()
        if item.output_path.suffix == ".meta" and actual_hash in recorded_hashes.get(
            relative, set()
        ):
            item.output_bytes = actual
            continue
        if item.output_path.suffix == ".meta" and finalize_unity_meta:
            accept_unity_normalized_meta(item, actual)
            continue
        raise RuntimeError(
            f"Prepared asset conflict; existing file was kept: {item.output_path}"
        )


def write_outputs(prepared: list[PreparedFile]) -> None:
    for item in prepared:
        item.output_path.parent.mkdir(parents=True, exist_ok=True)
        item.output_path.write_bytes(item.output_bytes)


def source_record(source_root: Path, role: str, path: Path) -> dict[str, Any]:
    return {
        "role": role,
        "relativePath": path.relative_to(source_root).as_posix(),
        "bytes": path.stat().st_size,
        "sha256": sha256(path),
    }


def make_manifest(
    project_root: Path,
    source_root: Path,
    prepared: list[PreparedFile],
    selected: list[CharacterSpec],
    counts: Counter[str],
    full_scope: bool,
) -> dict[str, Any]:
    entries: list[dict[str, Any]] = []
    for item in prepared:
        primary = source_record(source_root, "primary", item.primary_source)
        entries.append(
            {
                "category": item.category,
                "purpose": item.purpose,
                "evidenceStatus": item.evidence_status,
                "sourceRelativePath": primary["relativePath"],
                "sourceBytes": primary["bytes"],
                "sourceSha256": primary["sha256"],
                "supportingSources": [
                    source_record(source_root, role, path)
                    for role, path in item.supporting_sources
                ],
                "outputRelativePath": item.output_path.relative_to(project_root).as_posix(),
                "outputBytes": len(item.output_bytes),
                "outputSha256": sha256_bytes(item.output_bytes),
                "transformation": item.transformation,
                "parameters": item.parameters,
                "evidence": item.evidence,
                "notes": (
                    "Runtime resource for the Recovery Harness; not a production scene or gameplay implementation."
                ),
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
        "selection": {
            "fullScope": full_scope,
            "characterKeys": [character.key for character in selected],
            "counts": dict(sorted(counts.items())),
            "resourceFileCount": len(prepared) // 2,
            "manifestEntryCount": len(prepared),
        },
        "entries": sorted(entries, key=lambda entry: entry["outputRelativePath"]),
    }


def manifest_path(project_root: Path, selected: list[CharacterSpec], full_scope: bool) -> Path:
    if full_scope:
        return project_root / OUTPUT_MANIFEST_RELATIVE
    suffix = "-".join(character.key for character in selected)
    return project_root / "Recovery/Manifests" / f"character-resources-pilot-{suffix}.json"


def main() -> None:
    args = parse_args()
    project_root = args.project_root.resolve()
    source_root = resolve_source_root(project_root, args.source_root)
    validate_source_identity(project_root, source_root)
    catalog, characters = load_catalog(project_root)
    selected, full_scope = select_characters(characters, args.target)
    raw_content = source_root / RAW_ASSETS_RELATIVE / CONTENT_RELATIVE
    exported_content = source_root / EXPORTED_ASSETS_RELATIVE / CONTENT_RELATIVE
    if not raw_content.is_dir() or not exported_content.is_dir():
        raise RuntimeError("Required raw or Unity-exported reference content root is missing.")

    prepared: list[PreparedFile] = []
    counts: Counter[str] = Counter()
    for character in selected:
        prepare_character(
            prepared,
            counts,
            project_root=project_root,
            raw_content=raw_content,
            exported_content=exported_content,
            catalog=catalog,
            character=character,
        )
    validate_output_paths(prepared, project_root, args.finalize_unity_meta)

    summary = (
        f"Validated {len(selected)} characters, {len(prepared) // 2} resource files, "
        f"and {len(prepared) // 2} preserved Unity metas."
    )
    if not args.apply:
        print(summary + " No files were written.")
        print(json.dumps(dict(sorted(counts.items())), ensure_ascii=False, indent=2))
        return

    write_outputs(prepared)
    manifest = make_manifest(
        project_root, source_root, prepared, selected, counts, full_scope
    )
    output_manifest = manifest_path(project_root, selected, full_scope)
    output_manifest.parent.mkdir(parents=True, exist_ok=True)
    output_manifest.write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    print(summary + f" Manifest: {output_manifest}")


if __name__ == "__main__":
    main()
