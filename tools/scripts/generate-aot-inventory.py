#!/usr/bin/env python3
"""Create a deterministic Native-AOT warning inventory from restore and publish logs."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from collections import Counter
from pathlib import Path
from typing import Any


WARNING_PATTERN = re.compile(
    r"^(?P<source>.+?)(?:\((?P<line>\d+)(?:,(?P<column>\d+))?\))?\s*:\s*"
    r"(?:(?:Trim analysis|AOT analysis)\s+)?warning\s+(?P<code>[A-Z]+\d+):\s*"
    r"(?P<message>.*?)(?:\s+\[(?P<project>[^\]]+)\])?$",
)
PACKAGE_PATH_PATTERN = re.compile(
    r"[\\/]\.nuget[\\/]packages[\\/](?P<id>[^\\/]+)[\\/](?P<version>[^\\/]+)"
    r"(?P<tail>.*)$",
    re.IGNORECASE,
)
PACKAGE_WARNING_PATTERN = re.compile(
    r"Package ['\"](?P<id>[^'\"]+)['\"] (?P<version>[^\s]+)",
    re.IGNORECASE,
)
RELEVANT_CLOSURE_PACKAGES = (
    "mono.options",
    "newtonsoft",
    "avro",
    "kubernetes",
    "azure.iot.operations",
)
OWNER_ORDER = {
    "application": 0,
    "UA-.NETStandard": 1,
    "third-party": 2,
    "feed/tooling": 3,
    "unclassified": 4,
}


def parse_args() -> argparse.Namespace:
    """Parse command line arguments."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--log",
        action="append",
        default=[],
        type=Path,
        help="Restore, build, or publish log to inventory. May be specified more than once.",
    )
    parser.add_argument(
        "--assets",
        action="append",
        default=[],
        type=Path,
        help="NuGet project.assets.json file used to resolve package names and versions.",
    )
    parser.add_argument(
        "--repo-root",
        type=Path,
        default=Path.cwd(),
        help="Repository root used when normalizing source paths.",
    )
    parser.add_argument(
        "--commit",
        default="unknown",
        help="Commit represented by the inventory. The value is informational and deterministic.",
    )
    parser.add_argument("--json-out", type=Path, help="JSON report destination.")
    parser.add_argument("--markdown-out", type=Path, help="Markdown report destination.")
    parser.add_argument(
        "--baseline",
        type=Path,
        help="Committed JSON baseline used by --enforce.",
    )
    parser.add_argument(
        "--candidate-baseline",
        type=Path,
        help=(
            "Candidate JSON baseline whose application warnings must not grow beyond "
            "--baseline. Used to validate pull request baseline edits."
        ),
    )
    parser.add_argument(
        "--enforce",
        action="store_true",
        help="Fail for unclassified, feed, or new application-owned warnings.",
    )
    return parser.parse_args()


def read_package_metadata(assets_files: list[Path]) -> tuple[dict[str, tuple[str, str]], list[dict[str, Any]]]:
    """Return package lookup and the relevant Native-AOT dependency closure."""
    packages: dict[str, tuple[str, str]] = {}
    direct_packages: set[str] = set()

    for assets_file in sorted(assets_files, key=lambda path: str(path)):
        if not assets_file.is_file():
            continue
        with assets_file.open(encoding="utf-8-sig") as stream:
            assets = json.load(stream)

        for library_name, metadata in assets.get("libraries", {}).items():
            if metadata.get("type") != "package" or "/" not in library_name:
                continue
            package_id, version = library_name.rsplit("/", 1)
            packages.setdefault(package_id.casefold(), (package_id, version))

        for framework in assets.get("project", {}).get("frameworks", {}).values():
            direct_packages.update(
                package_id.casefold()
                for package_id in framework.get("dependencies", {})
            )

    closure = [
        {
            "id": package_id,
            "version": version,
            "direct": package_id.casefold() in direct_packages,
        }
        for package_id, version in packages.values()
        if any(marker in package_id.casefold() for marker in RELEVANT_CLOSURE_PACKAGES)
    ]
    closure.sort(key=lambda package: (package["id"].casefold(), package["version"]))
    return packages, closure


def normalize_source(source: str, repo_root: Path, package: dict[str, str] | None) -> str:
    """Normalize a compiler source or assembly path across Windows and Linux."""
    normalized = source.strip().replace("\\", "/")
    package_match = PACKAGE_PATH_PATTERN.search(source)
    if package_match:
        package_id = package_match.group("id")
        version = package_match.group("version")
        assembly = Path(package_match.group("tail").replace("\\", "/")).name
        return f"package:{package_id}/{version}/{assembly or Path(normalized).name}"

    lower = normalized.casefold()
    for marker in ("src/", "external/ua-.netstandard/", "tools/"):
        index = lower.find(marker)
        if index >= 0:
            return normalized[index:]

    try:
        return Path(source).resolve().relative_to(repo_root.resolve()).as_posix()
    except (OSError, ValueError):
        return normalized


def get_package(source: str, message: str, package_lookup: dict[str, tuple[str, str]]) -> dict[str, str] | None:
    """Find the package that emitted a diagnostic when the log exposes it."""
    source_match = PACKAGE_PATH_PATTERN.search(source)
    if source_match:
        package_id = source_match.group("id")
        known_package = package_lookup.get(package_id.casefold())
        return {
            "id": known_package[0] if known_package else package_id,
            "version": known_package[1] if known_package else source_match.group("version"),
        }

    message_match = PACKAGE_WARNING_PATTERN.search(message)
    if message_match:
        package_id = message_match.group("id")
        version = message_match.group("version")
        known_package = package_lookup.get(package_id.casefold())
        return {
            "id": known_package[0] if known_package else package_id,
            "version": known_package[1] if known_package else version,
        }
    return None


def classify_warning(
    code: str,
    source: str,
    message: str,
    package: dict[str, str] | None,
) -> tuple[str, str, str, str]:
    """Classify ownership and define the tracking disposition for a warning."""
    message_lower = message.casefold()
    source_lower = source.casefold().replace("\\", "/")

    if code == "NU1900" or (
        code.startswith("NU")
        and (
            "azure artifacts" in message_lower
            or "pkgs.dev.azure.com" in message_lower
            or "service index" in message_lower
        )
    ):
        return (
            "feed/tooling",
            "fail: restore/feed defect; do not allow as an AOT baseline exception",
            "TODO-AOT-FEED",
            "immediate",
        )

    if package is not None or code.startswith("NU"):
        return (
            "third-party",
            "dependency diagnostic; remediate by updating or replacing the package",
            "TODO-AOT-DEPENDENCY",
            "2026-12-31",
        )

    if "external/ua-.netstandard/" in source_lower:
        return (
            "UA-.NETStandard",
            "upstream UA-.NETStandard diagnostic; track upstream remediation",
            "TODO-AOT-UA-NETSTANDARD",
            "2026-12-31",
        )

    if source_lower.startswith("src/") or "/src/" in source_lower:
        if code.startswith("IL"):
            return (
                "application",
                "temporary Native-AOT baseline; resolve in aot-final-cleanup",
                "TODO-AOT-FINAL-CLEANUP",
                "2026-12-31",
            )
        return (
            "application",
            "pre-existing compiler/analyzer diagnostic; outside this AOT inventory scope",
            "TODO-AOT-APPLICATION",
            "2026-12-31",
        )

    if source_lower.startswith("tools/") or "/tools/" in source_lower:
        return (
            "feed/tooling",
            "tooling diagnostic; remediate in build tooling",
            "TODO-AOT-TOOLING",
            "2026-12-31",
        )

    return (
        "unclassified",
        "classification required before this warning can be allowed",
        "TODO-AOT-CLASSIFY",
        "immediate",
    )


def warning_key(
    code: str,
    source: str,
    line: int | None,
    column: int | None,
    message: str,
) -> str:
    """Return a stable warning identity independent of diagnostic log location."""
    material = "\n".join((code, source, str(line or ""), str(column or ""), message))
    return hashlib.sha256(material.encode("utf-8")).hexdigest()[:20]


def parse_logs(
    logs: list[Path],
    repo_root: Path,
    package_lookup: dict[str, tuple[str, str]],
) -> list[dict[str, Any]]:
    """Parse, normalize, deduplicate, and classify diagnostics from log files."""
    warnings: dict[str, dict[str, Any]] = {}
    for log_file in sorted(logs, key=lambda path: str(path)):
        if not log_file.is_file():
            raise FileNotFoundError(f"Log file does not exist: {log_file}")
        for text in log_file.read_text(encoding="utf-8", errors="replace").splitlines():
            match = WARNING_PATTERN.match(text)
            if match is None:
                continue

            source = match.group("source").strip()
            code = match.group("code")
            message = match.group("message").strip()
            package = get_package(source, message, package_lookup)
            normalized_source = normalize_source(source, repo_root, package)
            line = int(match.group("line")) if match.group("line") else None
            column = int(match.group("column")) if match.group("column") else None
            owner, disposition, tracking_issue, expiry = classify_warning(
                code,
                normalized_source,
                message,
                package,
            )
            key = warning_key(code, normalized_source, line, column, message)
            warning = warnings.setdefault(
                key,
                {
                    "key": key,
                    "code": code,
                    "source_or_assembly": normalized_source,
                    "line": line,
                    "column": column,
                    "owner": owner,
                    "package": package,
                    "disposition": disposition,
                    "tracking_issue": tracking_issue,
                    "expiry": expiry,
                    "message": message,
                    "occurrences": [],
                    "count": 0,
                },
            )
            warning["count"] += 1
            warning["occurrences"].append(log_file.name)

    for warning in warnings.values():
        warning["occurrences"] = sorted(set(warning["occurrences"]))

    return sorted(
        warnings.values(),
        key=lambda warning: (
            OWNER_ORDER[warning["owner"]],
            warning["code"],
            warning["source_or_assembly"],
            warning["line"] if warning["line"] is not None else -1,
            warning["column"] if warning["column"] is not None else -1,
            warning["message"],
        ),
    )


def create_report(
    commit: str,
    warnings: list[dict[str, Any]],
    package_closure: list[dict[str, Any]],
) -> dict[str, Any]:
    """Create the JSON report structure."""
    by_owner = Counter(warning["owner"] for warning in warnings)
    aot_by_owner = Counter(
        warning["owner"] for warning in warnings if warning["code"].startswith("IL")
    )
    return {
        "schema_version": 1,
        "commit": commit,
        "summary": {
            "unique_warnings": len(warnings),
            "warning_occurrences": sum(warning["count"] for warning in warnings),
            "by_owner": dict(sorted(by_owner.items())),
            "aot_by_owner": dict(sorted(aot_by_owner.items())),
            "unclassified": by_owner["unclassified"],
        },
        "package_closure": package_closure,
        "warnings": warnings,
    }


def markdown_escape(value: str) -> str:
    """Escape a table cell."""
    return value.replace("|", "\\|").replace("\n", " ")


def create_markdown(report: dict[str, Any]) -> str:
    """Render a deterministic Markdown representation of the JSON report."""
    summary = report["summary"]
    lines = [
        "# Publisher.Module Native-AOT warning baseline",
        "",
        f"Commit: `{report['commit']}`",
        "",
        f"- Unique warnings: {summary['unique_warnings']}",
        f"- Warning occurrences: {summary['warning_occurrences']}",
        f"- Unclassified warnings: {summary['unclassified']}",
        "",
        "## Warnings",
        "",
        "| Code | Source / assembly | Owner | Package / version | Disposition | Tracking issue | Expiry | Count | Message |",
        "| --- | --- | --- | --- | --- | --- | --- | ---: | --- |",
    ]
    for warning in report["warnings"]:
        package = warning["package"]
        package_text = (
            f"{package['id']} / {package['version']}" if package is not None else ""
        )
        location = warning["source_or_assembly"]
        if warning["line"] is not None:
            location += f":{warning['line']}"
            if warning["column"] is not None:
                location += f":{warning['column']}"
        lines.append(
            "| "
            + " | ".join(
                markdown_escape(value)
                for value in (
                    warning["code"],
                    location,
                    warning["owner"],
                    package_text,
                    warning["disposition"],
                    warning["tracking_issue"],
                    warning["expiry"],
                    str(warning["count"]),
                    warning["message"],
                )
            )
            + " |"
        )

    lines.extend(
        [
            "",
            "## Relevant dependency closure",
            "",
            "| Package | Version | Direct reference |",
            "| --- | --- | --- |",
        ]
    )
    for package in report["package_closure"]:
        lines.append(
            f"| {package['id']} | {package['version']} | "
            f"{'yes' if package['direct'] else 'no'} |"
        )
    lines.append("")
    return "\n".join(lines)


def write_report(path: Path, content: str) -> None:
    """Write a report with normalized LF newlines."""
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


def load_baseline(baseline_path: Path, description: str) -> tuple[dict[str, Any] | None, list[str]]:
    """Load a baseline and return actionable failures for missing or malformed data."""
    if not baseline_path.is_file():
        return None, [f"{description} does not exist: {baseline_path}"]

    try:
        with baseline_path.open(encoding="utf-8") as stream:
            baseline = json.load(stream)
    except (OSError, json.JSONDecodeError) as exception:
        return None, [f"{description} is malformed: {baseline_path} ({exception})"]

    if not isinstance(baseline, dict) or not isinstance(baseline.get("warnings"), list):
        return None, [f"{description} is malformed: {baseline_path} has no warnings list"]

    for index, warning in enumerate(baseline["warnings"]):
        if (
            not isinstance(warning, dict)
            or not isinstance(warning.get("key"), str)
            or not isinstance(warning.get("owner"), str)
        ):
            return None, [
                f"{description} is malformed: {baseline_path} warning {index} has no key or owner"
            ]
    return baseline, []


def enforce_policy(
    report: dict[str, Any],
    baseline_path: Path,
    candidate_baseline_path: Path | None,
) -> list[str]:
    """Return policy failures for the report compared with its committed baseline."""
    baseline, failures = load_baseline(baseline_path, "Protected AOT baseline")
    if baseline is None:
        return failures

    baseline_keys = {warning["key"] for warning in baseline.get("warnings", [])}
    if candidate_baseline_path is not None:
        candidate, candidate_failures = load_baseline(
            candidate_baseline_path,
            "Candidate AOT baseline",
        )
        if candidate is None:
            return candidate_failures

        protected_application_keys = {
            warning["key"] for warning in baseline["warnings"] if warning["owner"] == "application"
        }
        candidate_application_keys = {
            warning["key"]
            for warning in candidate["warnings"]
            if warning["owner"] == "application"
        }
        for key in sorted(candidate_application_keys - protected_application_keys):
            failures.append(
                "Application warning baseline grows outside the protected base: "
                f"{key}"
            )

    for warning in report["warnings"]:
        if warning["owner"] == "unclassified":
            failures.append(
                f"Unclassified warning {warning['code']} at {warning['source_or_assembly']}"
            )
        elif warning["owner"] == "feed/tooling":
            failures.append(
                f"Restore/feed warning {warning['code']} at {warning['source_or_assembly']}"
            )
        elif warning["owner"] == "application" and warning["key"] not in baseline_keys:
            failures.append(
                f"New application warning {warning['code']} at {warning['source_or_assembly']}"
            )
    return failures


def main() -> int:
    """Generate the inventory and optionally enforce the CI policy."""
    args = parse_args()
    if not args.log:
        print("At least one --log argument is required.", file=sys.stderr)
        return 2

    package_lookup, package_closure = read_package_metadata(args.assets)
    warnings = parse_logs(args.log, args.repo_root, package_lookup)
    report = create_report(args.commit, warnings, package_closure)

    if args.json_out is not None:
        write_report(args.json_out, json.dumps(report, indent=2, sort_keys=True) + "\n")
    if args.markdown_out is not None:
        write_report(args.markdown_out, create_markdown(report))

    if args.enforce:
        if args.baseline is None:
            print("--baseline is required with --enforce.", file=sys.stderr)
            return 2
        failures = enforce_policy(report, args.baseline, args.candidate_baseline)
        if failures:
            print("Native-AOT warning policy failed:", file=sys.stderr)
            for failure in failures:
                print(f"- {failure}", file=sys.stderr)
            return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
