#!/usr/bin/env python3
"""Builds the Asset Store upload: dist/pixelizer-<version>.zip with addons/pixelizer/ as committed at HEAD.

    tools/package.py

The version is the first "## x.y.z" heading of addons/pixelizer/CHANGELOG.md. The zip's root is addons/, which
Godot's asset installer keeps as is, so the addon lands in res://addons/pixelizer/. Uncommitted changes to the addon
are refused: the zip must match a commit people can find.
"""
import re
import subprocess
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
ADDON = "addons/pixelizer"
REQUIRED = ["LICENSE", "README.md", "CREDITS.md", "CHANGELOG.md", "pixel_stage.tscn"]


def git(*args: str) -> str:
    return subprocess.run(["git", *args], cwd=ROOT, check=True, capture_output=True, text=True).stdout


def main() -> int:
    changelog = (ROOT / ADDON / "CHANGELOG.md").read_text()
    match = re.search(r"^## (\d+\.\d+\.\d+)", changelog, flags=re.M)
    if match is None:
        print("CHANGELOG.md has no '## x.y.z' heading", file=sys.stderr)
        return 1
    version = match.group(1)

    dirty = git("status", "--porcelain", "--", ADDON).strip()
    if dirty:
        print(f"{ADDON} has uncommitted changes; commit them first:\n{dirty}", file=sys.stderr)
        return 1

    out = ROOT / "dist" / f"pixelizer-{version}.zip"
    out.parent.mkdir(exist_ok=True)
    git("archive", "--format=zip", f"--output={out}", "HEAD", ADDON)

    names = zipfile.ZipFile(out).namelist()
    missing = [f for f in REQUIRED if f"{ADDON}/{f}" not in names]
    if missing:
        print(f"{out.name} lacks {', '.join(missing)}", file=sys.stderr)
        return 1
    commit = git("rev-parse", "--short", "HEAD").strip()
    files = sum(1 for n in names if not n.endswith("/"))
    print(f"{out.relative_to(ROOT)}: {files} files, version {version}, commit {commit}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
