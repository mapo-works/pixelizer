#!/usr/bin/env python3
"""Copies the Pixelizer addon into Godot projects, or refreshes the copy they have.

    tools/install.py <project> [<project> ...]

Each project gets addons/pixelizer replaced by this repo's (the copy is never edited there) and the project
settings the look needs added to its project.godot when it doesn't set them already. Settings it does set are left
alone, so a project can still choose otherwise.
"""
import re
import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
ADDON = ROOT / "addons" / "pixelizer"
IGNORE = shutil.ignore_patterns(".godot", "*.tmp", "__pycache__")

# section -> key -> value: what the look needs (see addons/pixelizer/README.md).
SETTINGS = {
    "rendering": {
        "lights_and_shadows/directional_shadow/soft_shadow_filter_quality": "0",
        "textures/decals/filter": "0",
    },
}


def add_settings(project: Path) -> list[str]:
    """Adds the SETTINGS keys project.godot lacks; returns the ones added."""
    text = project.read_text()
    added = []
    for section, values in SETTINGS.items():
        header = re.search(rf"^\[{section}\]\s*$", text, flags=re.M)
        if header is None:
            text = text.rstrip("\n") + f"\n\n[{section}]\n"
            header = re.search(rf"^\[{section}\]\s*$", text, flags=re.M)
        end = re.search(r"^\[", text[header.end():], flags=re.M)
        body_end = header.end() + (end.start() if end else len(text) - header.end())
        body = text[header.end():body_end]
        missing = [f"{k}={v}" for k, v in values.items() if not re.search(rf"^{re.escape(k)}=", body, flags=re.M)]
        if not missing:
            continue
        body = body.rstrip("\n") + "\n" + "\n".join(missing) + "\n\n"
        if not body.startswith("\n"):
            body = "\n" + body
        text = text[:header.end()] + body + text[body_end:].lstrip("\n")
        added += [f"[{section}] {m}" for m in missing]
    project.write_text(text.rstrip("\n") + "\n")
    return added


def install(dest: Path) -> int:
    project = dest / "project.godot"
    if not project.exists():
        print(f"{dest}: no project.godot", file=sys.stderr)
        return 1
    if dest.resolve() == ROOT:
        print(f"{dest}: that's the master copy", file=sys.stderr)
        return 1
    target = dest / "addons" / "pixelizer"
    if target.exists():
        shutil.rmtree(target)
    shutil.copytree(ADDON, target, ignore=IGNORE)
    added = add_settings(project)
    print(f"{dest}: addons/pixelizer copied" + (", added " + ", ".join(added) if added else ""))
    return 0


def main() -> int:
    if len(sys.argv) < 2 or sys.argv[1] in ("-h", "--help"):
        print(__doc__)
        return 0 if len(sys.argv) >= 2 else 1
    return max(install(Path(arg)) for arg in sys.argv[1:])


if __name__ == "__main__":
    sys.exit(main())
