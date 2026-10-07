#!/usr/bin/env python3
"""
Coverage gate for the JobApplicationTracker API (P1/M1e).

Merges every Cobertura report under tests/**/TestResults/ and fails the build
if the overall line-coverage percentage is below the committed floor.

The floor lives in tests/coverage-baseline.txt (a single number, e.g. "20").
Raise it deliberately when a milestone honestly moves coverage up; never lower
it to make a red build green.

Usage:
    python3 scripts/coverage-gate.py [--min 20] [--baseline tests/coverage-baseline.txt]

Exit codes: 0 = gate passed, 1 = coverage below floor, 2 = no reports found.
"""

import argparse
import glob
import sys
import xml.etree.ElementTree as ET


def main() -> int:
    parser = argparse.ArgumentParser(description="Enforce the coverage floor.")
    parser.add_argument("--min", type=float, default=None,
                        help="Minimum line-coverage %%. Defaults to tests/coverage-baseline.txt.")
    parser.add_argument("--baseline", default="tests/coverage-baseline.txt",
                        help="File holding the committed coverage floor.")
    args = parser.parse_args()

    floor = args.min
    if floor is None:
        try:
            with open(args.baseline, encoding="utf-8") as f:
                floor = float(f.read().strip())
        except (OSError, ValueError) as exc:
            print(f"coverage-gate: cannot read baseline file {args.baseline}: {exc}")
            return 2

    reports = sorted(glob.glob("tests/**/TestResults/coverage.cobertura.xml", recursive=True))
    if not reports:
        print("coverage-gate: no Cobertura reports found under tests/**/TestResults/")
        return 2

    valid = covered = 0
    for report in reports:
        tree = ET.parse(report)
        for cls in tree.getroot().iter("class"):
            filename = cls.get("filename", "")
            # Belt and braces: the MSBuild props already exclude these, but the
            # gate must stay honest even if coverage was collected differently.
            if "/Migrations/" in filename or "/obj/" in filename:
                continue
            for line in cls.iter("line"):
                if line.get("missing-branches"):
                    continue  # branch-only entry, not a line
                valid += 1
                if int(line.get("hits", "0")) > 0:
                    covered += 1

    pct = (100.0 * covered / valid) if valid else 0.0
    print(f"coverage-gate: {covered}/{valid} lines = {pct:.1f}% "
          f"(floor {floor:.0f}%, {len(reports)} report(s))")

    if pct < floor:
        print(f"coverage-gate: FAILED - {pct:.1f}% is below the {floor:.0f}% floor.")
        return 1
    print("coverage-gate: passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
