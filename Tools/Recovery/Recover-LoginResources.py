#!/usr/bin/env python3
"""Validate and prepare the source-backed login Title Spine set."""

from __future__ import annotations

import argparse
import hashlib
import io
import json
import re
import shutil
import struct
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path

try:
    from PIL import Image
except ImportError as exception:  # pragma: no cover - environment diagnostic
    raise SystemExit(
        "Pillow is required. Pass a Python 3 executable with Pillow installed."
    ) from exception


RAW_ROOT = Path(
    "03_extracted_resources/02_raw_data/Assets/02.resources/00.title/spine/background"
)
EXPORTED_ROOT = Path(
    "03_extracted_resources/01_unity_data/ExportedProject/Assets/02.resources/00.title/spine/background"
)
OUTPUT_ROOT = Path("Assets/Game/Content/UI/Login/Spine/TitleBackground")
MANIFEST_PATH = Path("Recovery/Manifests/login-resources.json")
SOURCE_IDENTITY_PATH = Path("Recovery/Manifests/source-identity.json")


@dataclass(frozen=True)
class ResourceSpec:
    raw_name: str
    exported_name: str
    output_name: str
    category: str
    transformation: str


SPECS = (
    ResourceSpec(
        "Title_Background.skel.bytes",
        "Title_Background.skel.bytes",
        "Title_Background.skel.bytes",
        "login-title-spine-skeleton",
        "byte-for-byte-copy",
    ),
    ResourceSpec(
        "Title_Background.atlas.bytes",
        "Title_Background.atlas.txt",
        "Title_Background.atlas.txt",
        "login-title-spine-atlas",
        "unity-import-extension-and-pma-flag-conversion",
    ),
    ResourceSpec(
        "Title_Background.png",
        "Title_Background.png",
        "Title_Background.png",
        "login-title-spine-texture",
        "pma-to-straight-alpha-pixel-conversion",
    ),
)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def relative(path: Path, root: Path) -> str:
    return path.relative_to(root).as_posix()


def read_guid(meta_path: Path) -> str:
    text = meta_path.read_text(encoding="utf-8-sig")
    match = re.search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$", text)
    if not match:
        raise RuntimeError(f"Unity GUID is missing from {meta_path}")
    return match.group(1)


def validate_meta(meta_path: Path, expected_guid: str) -> None:
    actual_guid = read_guid(meta_path)
    if actual_guid != expected_guid:
        raise RuntimeError(
            f"Unity GUID changed for {meta_path}: expected {expected_guid}, got {actual_guid}"
        )
    text = meta_path.read_text(encoding="utf-8-sig")
    for key in ("assetBundleName", "assetBundleVariant"):
        match = re.search(rf"(?m)^[ \t]*{key}:[ \t]*(.*?)[ \t]*$", text)
        if match and match.group(1):
            raise RuntimeError(f"{meta_path} retains a non-empty {key}")


def prepare_unity_meta(
    source_meta: Path, output_meta: Path, apply: bool, finalize: bool
) -> None:
    if finalize:
        if not output_meta.exists():
            raise RuntimeError(f"Unity meta finalization requested before import: {output_meta}")
        return

    text = source_meta.read_text(encoding="utf-8-sig")
    normalized = re.sub(
        r"(?m)^([ \t]*assetBundleName:)[ \t]*.*$", r"\1", text
    )
    normalized = re.sub(
        r"(?m)^([ \t]*assetBundleVariant:)[ \t]*.*$", r"\1", normalized
    )
    if not apply:
        return
    output_meta.parent.mkdir(parents=True, exist_ok=True)
    output_meta.write_text(normalized, encoding="utf-8", newline="\n")


def validate_source_identity(project_root: Path, source_root: Path) -> tuple[str, dict]:
    identity_path = project_root / SOURCE_IDENTITY_PATH
    identity = json.loads(identity_path.read_text(encoding="utf-8"))
    for item in identity["files"]:
        source = source_root / item["relativePath"]
        if not source.is_file():
            raise RuntimeError(f"Recovery source identity file is missing: {source}")
        if source.stat().st_size != item["bytes"] or sha256(source) != item["sha256"]:
            raise RuntimeError(f"Recovery source identity mismatch: {source}")
    return sha256(identity_path), identity


def validate_resource(path: Path, output_name: str) -> dict:
    payload = path.read_bytes()
    if output_name.endswith(".skel.bytes"):
        versions = sorted(set(re.findall(rb"4\.1\.\d{2}", payload)))
        if not versions:
            raise RuntimeError(f"Spine 4.1 marker was not found in {path}")
        return {"spineVersionMarkers": [item.decode("ascii") for item in versions]}
    if output_name.endswith(".atlas.txt"):
        text = payload.decode("utf-8-sig")
        first_page = next((line.strip() for line in text.splitlines() if line.strip()), "")
        if first_page != "Title_Background.png":
            raise RuntimeError(f"Atlas page mismatch in {path}: {first_page!r}")
        return {
            "atlasPage": first_page,
            "pma": re.search(r"(?m)^pma\s*:\s*true\s*$", text) is not None,
        }
    if output_name.endswith(".png"):
        if len(payload) < 24 or payload[:8] != b"\x89PNG\r\n\x1a\n":
            raise RuntimeError(f"Invalid PNG header: {path}")
        width, height = struct.unpack(">II", payload[16:24])
        if width <= 0 or height <= 0:
            raise RuntimeError(f"Invalid PNG dimensions: {path}")
        return {"width": width, "height": height}
    return {}


def ensure_guid_unique(project_root: Path, output_meta: Path, guid: str) -> None:
    assets_root = project_root / "Assets"
    collisions = []
    for candidate in assets_root.rglob("*.meta"):
        if candidate.resolve() == output_meta.resolve():
            continue
        try:
            if read_guid(candidate) == guid:
                collisions.append(relative(candidate, project_root))
        except (OSError, UnicodeError, RuntimeError):
            continue
    if collisions:
        raise RuntimeError(f"GUID {guid} collides with: {', '.join(collisions)}")


def prepare_output_bytes(source: Path, output_name: str) -> bytes:
    payload = source.read_bytes()
    if output_name.endswith(".atlas.txt"):
        converted, replacements = re.subn(
            rb"(?m)^pma\s*:\s*true\s*$", b"pma:false", payload
        )
        if replacements != 1:
            raise RuntimeError(f"Expected one PMA flag in {source}")
        return converted
    if output_name.endswith(".png"):
        with Image.open(io.BytesIO(payload)) as image:
            rgba = image.convert("RGBA")
            pixels = bytearray(rgba.tobytes())
            for offset in range(0, len(pixels), 4):
                alpha = pixels[offset + 3]
                if alpha == 0:
                    pixels[offset] = 0
                    pixels[offset + 1] = 0
                    pixels[offset + 2] = 0
                    continue
                if alpha == 255:
                    continue
                pixels[offset] = min(255, (pixels[offset] * 255 + alpha // 2) // alpha)
                pixels[offset + 1] = min(
                    255, (pixels[offset + 1] * 255 + alpha // 2) // alpha
                )
                pixels[offset + 2] = min(
                    255, (pixels[offset + 2] * 255 + alpha // 2) // alpha
                )
            converted = Image.frombytes("RGBA", rgba.size, bytes(pixels))
            stream = io.BytesIO()
            converted.save(stream, format="PNG", optimize=False, compress_level=9)
            return stream.getvalue()
    return payload


def write_prepared_output(
    source: Path, output: Path, expected: bytes, apply: bool
) -> None:
    if output.exists():
        actual = output.read_bytes()
        if actual == expected:
            return
        if actual != source.read_bytes():
            raise RuntimeError(f"Managed output differs from its expected conversion: {output}")
    if not apply:
        return
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_bytes(expected)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", required=True, type=Path)
    parser.add_argument("--source-root", required=True, type=Path)
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--finalize-unity-meta", action="store_true")
    args = parser.parse_args()

    project_root = args.project_root.resolve()
    source_root = args.source_root.resolve()
    source_identity_sha, _ = validate_source_identity(project_root, source_root)
    entries: list[dict] = []

    for spec in SPECS:
        raw = source_root / RAW_ROOT / spec.raw_name
        exported = source_root / EXPORTED_ROOT / spec.exported_name
        exported_meta = exported.with_name(exported.name + ".meta")
        output = project_root / OUTPUT_ROOT / spec.output_name
        output_meta = output.with_name(output.name + ".meta")

        for required in (raw, exported, exported_meta):
            if not required.is_file():
                raise RuntimeError(f"Required login source is missing: {required}")
        if raw.stat().st_size != exported.stat().st_size or sha256(raw) != sha256(exported):
            raise RuntimeError(f"Raw/exported source mismatch for {spec.output_name}")

        details = validate_resource(raw, spec.output_name)
        expected_output = prepare_output_bytes(raw, spec.output_name)
        if spec.output_name.endswith(".atlas.txt"):
            details["sourcePma"] = True
            details["outputPma"] = False
        if spec.output_name.endswith(".png"):
            details["sourceAlphaWorkflow"] = "premultiplied"
            details["outputAlphaWorkflow"] = "straight"
        source_guid = read_guid(exported_meta)
        ensure_guid_unique(project_root, output_meta, source_guid)
        write_prepared_output(raw, output, expected_output, args.apply)

        prepare_unity_meta(
            exported_meta, output_meta, args.apply, args.finalize_unity_meta
        )
        if output_meta.exists():
            validate_meta(output_meta, source_guid)

        output_bytes = output.stat().st_size if output.exists() else len(expected_output)
        output_sha = sha256(output) if output.exists() else hashlib.sha256(expected_output).hexdigest()
        entries.append(
            {
                "category": spec.category,
                "evidenceStatus": "confirmed",
                "sourceRelativePath": relative(raw, source_root),
                "supportingSourceRelativePath": relative(exported, source_root),
                "sourceBytes": raw.stat().st_size,
                "sourceSha256": sha256(raw),
                "transformation": spec.transformation,
                "parameters": details,
                "outputRelativePath": relative(output, project_root),
                "outputBytes": output_bytes,
                "outputSha256": output_sha,
                "notes": "Source-backed Unity runtime resource; original evidence remains read-only.",
            }
        )

        meta_source_sha = sha256(exported_meta)
        meta_output_sha = sha256(output_meta) if output_meta.exists() else meta_source_sha
        entries.append(
            {
                "category": f"{spec.category}-meta",
                "evidenceStatus": "confirmed",
                "sourceRelativePath": relative(exported_meta, source_root),
                "sourceBytes": exported_meta.stat().st_size,
                "sourceSha256": meta_source_sha,
                "transformation": (
                    "unity-normalized-meta-with-preserved-guid"
                    if meta_output_sha != meta_source_sha
                    else "byte-for-byte-meta-copy"
                ),
                "parameters": {"guid": source_guid},
                "outputRelativePath": relative(output_meta, project_root),
                "outputBytes": (
                    output_meta.stat().st_size if output_meta.exists() else exported_meta.stat().st_size
                ),
                "outputSha256": meta_output_sha,
                "notes": "Unity GUID and blank AssetBundle labels are required invariants.",
            }
        )

    manifest = {
        "schemaVersion": 1,
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "status": (
            "unity-meta-finalized"
            if args.finalize_unity_meta
            else "ready-for-unity-import"
        ),
        "productionComplete": False,
        "sourceIdentity": {
            "manifestRelativePath": SOURCE_IDENTITY_PATH.as_posix(),
            "sha256": source_identity_sha,
        },
        "entries": entries,
    }

    if args.apply:
        manifest_path = project_root / MANIFEST_PATH
        manifest_path.parent.mkdir(parents=True, exist_ok=True)
        manifest_path.write_text(
            json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
        )

    print(
        "Login Title Spine validation passed: "
        f"resources={len(SPECS)}, entries={len(entries)}, apply={args.apply}."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
