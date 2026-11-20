#!/usr/bin/env python3
"""EF L1 static gate - pure-Python checks that need no Unity install.

Checks (all read-only, zero external dependencies):
  1. package.json / Editorlanguages.json are valid JSON and package names match folders
  2. core dependency version consistency across cn.efefef.* packages (blocking)
  3. [Manager] naming convention: class carrying [Manager] must end with "Manager"
  4. no `using System.Linq` in any Runtime .cs (Editor Linq is allowed by project policy)

Exit code 0 = all blocking checks pass; 1 = at least one blocking failure.
Run: python tools/l1_static_gate.py [--repo-root PATH]
"""

import argparse
import json
import os
import re
import sys

PKG_REL_PATH = os.path.join("EF_Unity", "Packages")
PKG_PREFIX = "cn.efefef."
CORE_PKG = "cn.efefef.core"
SKIP_DIRS = {"DotNet~", "obj", "Library", "Temp", ".git"}


class Result:
    def __init__(self):
        self.errors = []   # blocking
        self.warnings = []  # non-blocking

    def error(self, msg):
        self.errors.append(msg)

    def warn(self, msg):
        self.warnings.append(msg)


def iter_files(root, suffix):
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]
        for fn in filenames:
            if fn.endswith(suffix):
                yield os.path.join(dirpath, fn)


def rel(repo_root, path):
    return os.path.relpath(path, repo_root).replace(os.sep, "/")


def load_json(path):
    with open(path, "r", encoding="utf-8-sig") as f:
        return json.load(f)


def check_json(pkg_root, result):
    for path in iter_files(pkg_root, ".json"):
        try:
            load_json(path)
        except Exception as exc:
            result.error("JSON invalid: %s (%s)" % (rel(pkg_root, path), exc))
    # package.json name must match its folder
    for name in sorted(os.listdir(pkg_root)):
        folder = os.path.join(pkg_root, name)
        manifest = os.path.join(folder, "package.json")
        if not name.startswith(PKG_PREFIX) or not os.path.isfile(manifest):
            continue
        try:
            data = load_json(manifest)
        except Exception:
            continue  # already reported above
        if data.get("name") != name:
            result.error("package.json name mismatch: %s declares name=%r" %
                         (rel(pkg_root, manifest), data.get("name")))


def check_core_version(pkg_root, result):
    core_manifest = os.path.join(pkg_root, CORE_PKG, "package.json")
    if not os.path.isfile(core_manifest):
        result.error("core package.json missing: %s" % rel(pkg_root, core_manifest))
        return
    core_ver = load_json(core_manifest).get("version")
    for name in sorted(os.listdir(pkg_root)):
        manifest = os.path.join(pkg_root, name, "package.json")
        if not name.startswith(PKG_PREFIX) or not os.path.isfile(manifest):
            continue
        dep = load_json(manifest).get("dependencies", {}).get(CORE_PKG)
        if dep is not None and dep != core_ver:
            result.error("core version drift: %s depends on %s@%s, core is %s" %
                         (name, CORE_PKG, dep, core_ver))


def check_manager_naming(pkg_root, result):
    attr_re = re.compile(r"\[Manager(?:\s*\(|\])")
    class_re = re.compile(r"\bclass\s+(\w+)")
    for path in iter_files(pkg_root, ".cs"):
        with open(path, "r", encoding="utf-8-sig", errors="replace") as f:
            lines = f.readlines()
        for i, line in enumerate(lines):
            if not attr_re.search(line):
                continue
            # find the class declaration on the same line after the attribute,
            # or on a following line (allow other attributes in between)
            found = None
            tail = line[line.find("[Manager"):]
            tail = tail[tail.find("]") + 1:] if "]" in tail else ""
            m = class_re.search(tail)
            if m:
                found = m.group(1)
            else:
                for j in range(i + 1, min(i + 6, len(lines))):
                    m = class_re.search(lines[j])
                    if m:
                        found = m.group(1)
                        break
            if found is None:
                result.warn("[Manager] with no class found within 5 lines: %s:%d" %
                            (rel(pkg_root, path), i + 1))
            elif not found.endswith("Manager"):
                result.error("[Manager] naming violation: %s:%d class %s must end with 'Manager'" %
                             (rel(pkg_root, path), i + 1, found))


def check_runtime_linq(pkg_root, result):
    linq_re = re.compile(r"^\s*using\s+System\.Linq\s*;")
    for path in iter_files(pkg_root, ".cs"):
        # only Runtime assemblies: path segment .../Runtime/...
        parts = path.replace(os.sep, "/").split("/")
        if "Runtime" not in parts:
            continue
        with open(path, "r", encoding="utf-8-sig", errors="replace") as f:
            for i, line in enumerate(f, 1):
                if linq_re.match(line):
                    result.error("Runtime Linq forbidden: %s:%d" % (rel(pkg_root, path), i))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", default=None,
                        help="repo root (default: parent of this script's directory)")
    args = parser.parse_args()
    repo_root = os.path.abspath(args.repo_root or
                                os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
    pkg_root = os.path.join(repo_root, PKG_REL_PATH)
    if not os.path.isdir(pkg_root):
        print("ERROR: packages folder not found: %s" % pkg_root)
        return 1

    result = Result()
    check_json(pkg_root, result)
    check_core_version(pkg_root, result)
    check_manager_naming(pkg_root, result)
    check_runtime_linq(pkg_root, result)

    for w in result.warnings:
        print("WARN  " + w)
    for e in result.errors:
        print("ERROR " + e)

    print("\n%d error(s), %d warning(s)." % (len(result.errors), len(result.warnings)))
    return 1 if result.errors else 0


if __name__ == "__main__":
    sys.exit(main())
