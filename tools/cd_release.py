#!/usr/bin/env python3
"""EF CD release helper - detect version bumps and assemble release payloads.

Continuous Delivery model:
  * A package is released ONLY when its package.json version changed vs. the
    previous release tag for that package (human bump = human confirmation).
  * Tag naming is per-package: <pkg-name>-v<version>  e.g. cn.efefef.ui-v0.2.0
  * Release notes are generated from git commit subjects since the previous tag.

The script is read-only with respect to git; it prints JSON describing what to
publish. The workflow consumes that JSON to create GitHub releases. No secrets,
no network calls, no Unity required.

Commands:
  detect   - print JSON: {"releases":[{name,version,tag,notes,previous_tag}]}
  suggest  - print human-readable nudges: packages with commits since last tag
             but no version bump (non-blocking reminder to bump in the panel)
"""

import argparse
import json
import os
import re
import subprocess
import sys

PKG_REL = os.path.join("EF_Unity", "Packages")
PKG_PREFIX = "cn.efefef."


def git(*args, check=True):
    r = subprocess.run(("git",) + args, capture_output=True, text=True)
    if check and r.returncode != 0:
        raise RuntimeError("git %s failed: %s" % (" ".join(args), r.stderr.strip()))
    return r.stdout.strip()


def repo_root():
    return git("rev-parse", "--show-toplevel")


def package_dirs(root):
    base = os.path.join(root, PKG_REL)
    out = []
    for name in sorted(os.listdir(base)):
        if name.startswith(PKG_PREFIX) and os.path.isfile(os.path.join(base, name, "package.json")):
            out.append(name)
    return out


def read_version(root, name, ref=None):
    # git paths must always use forward slashes, even on Windows
    path = "%s/%s/package.json" % (PKG_REL.replace(os.sep, "/"), name)
    if ref:
        try:
            txt = git("show", "%s:%s" % (ref, path))
        except RuntimeError:
            return None
    else:
        full = os.path.join(root, path)
        if not os.path.isfile(full):
            return None
        with open(full, "r", encoding="utf-8-sig") as f:
            txt = f.read()
    try:
        return json.loads(txt).get("version")
    except Exception:
        return None


def latest_tag(root, name):
    # tags look like cn.efefef.ui-v0.1.0
    pattern = "%s-v*" % name
    tags = git("tag", "--list", pattern, "--sort=-v:refname").splitlines()
    return tags[0] if tags else None


def commits_since(root, name, since_ref):
    path = "%s/%s" % (PKG_REL.replace(os.sep, "/"), name)
    rng = "%s..HEAD" % since_ref if since_ref else "HEAD"
    out = git("log", "--no-merges", "--pretty=format:%s", rng, "--", path, check=False)
    return [l.strip() for l in out.splitlines() if l.strip()]


def build_notes(name, version, previous_tag, commits):
    lines = ["## %s %s" % (name, version), ""]
    if previous_tag:
        lines.append("Changes since `%s`:" % previous_tag)
    else:
        lines.append("Initial release.")
    lines.append("")
    if commits:
        for c in commits[:40]:
            lines.append("- %s" % c)
        if len(commits) > 40:
            lines.append("- ...and %d more commits" % (len(commits) - 40))
    else:
        lines.append("- (no commit subjects found)")
    lines.append("")
    rel = PKG_REL.replace(os.sep, "/")
    lines.append("Install (pinned to this release, GitHub source):")
    lines.append("")
    lines.append("```json")
    lines.append('"%s": "https://github.com/HiWenHao/EFramework.git?path=%s/%s&ref=%s-v%s"'
                 % (name, rel, name, name, version))
    lines.append("```")
    lines.append("")
    lines.append("Latest via Gitee mirror (no tag): "
                 "`https://gitee.com/AlvinCN/EFramework.git?path=%s/%s`" % (rel, name))
    return "\n".join(lines)


def detect(root):
    releases = []
    for name in package_dirs(root):
        cur = read_version(root, name)
        if not cur:
            continue
        prev_tag = latest_tag(root, name)
        # previous version = version recorded in the previous release tag's package.json
        prev_ver = read_version(root, name, ref=prev_tag) if prev_tag else None
        if prev_ver == cur:
            continue  # version unchanged -> no release (this is the CD trigger rule)
        commits = commits_since(root, name, prev_tag)
        tag = "%s-v%s" % (name, cur)
        releases.append({
            "name": name,
            "version": cur,
            "previous_version": prev_ver,
            "previous_tag": prev_tag,
            "tag": tag,
            "notes": build_notes(name, cur, prev_tag, commits),
        })
    return {"releases": releases}


def suggest(root):
    lines = []
    for name in package_dirs(root):
        cur = read_version(root, name)
        if not cur:
            continue
        prev_tag = latest_tag(root, name)
        commits = commits_since(root, name, prev_tag)
        if not commits:
            continue
        # version already has a tag matching current -> intentional release already done
        if prev_tag and read_version(root, name, ref=prev_tag) == cur:
            continue
        lines.append("  %s: version %s unchanged, %d commit(s) since %s" %
                     (name, cur, len(commits), prev_tag or "repo start"))
    return lines


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=["detect", "suggest"])
    parser.add_argument("--repo-root", default=None)
    args = parser.parse_args()

    root = os.path.abspath(args.repo_root or repo_root())
    if args.command == "detect":
        print(json.dumps(detect(root), ensure_ascii=False, indent=2))
    elif args.command == "suggest":
        lines = suggest(root)
        if lines:
            print("Packages with unreleased commits (consider bumping version in EF.Packages panel):")
            print("\n".join(lines))
        else:
            print("No unreleased commits detected.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
