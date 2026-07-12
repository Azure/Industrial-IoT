#!/usr/bin/env python3
"""Tests for the deterministic Native-AOT warning inventory generator."""

from __future__ import annotations

import json
import shutil
import subprocess
import sys
import unittest
from pathlib import Path


SCRIPT = Path(__file__).parents[1] / "generate-aot-inventory.py"
TEST_DIRECTORY = Path(__file__).parent / ".aot-inventory-test-output"


class GenerateAotInventoryTests(unittest.TestCase):
    """Exercise log parsing and policy enforcement without external dependencies."""

    @classmethod
    def setUpClass(cls) -> None:
        shutil.rmtree(TEST_DIRECTORY, ignore_errors=True)
        TEST_DIRECTORY.mkdir(parents=True)
        cls.assets = TEST_DIRECTORY / "project.assets.json"
        cls.assets.write_text(
            json.dumps(
                {
                    "libraries": {
                        "Mono.Options/6.12.0.148": {"type": "package"},
                        "Newtonsoft.Json/13.0.3": {"type": "package"},
                    },
                    "project": {
                        "frameworks": {
                            "net10.0": {"dependencies": {"Mono.Options": "[6.12.0.148]"}}
                        }
                    },
                }
            ),
            encoding="utf-8",
        )

    @classmethod
    def tearDownClass(cls) -> None:
        shutil.rmtree(TEST_DIRECTORY, ignore_errors=True)

    def run_generator(self, log: Path, output_name: str, *extra: str) -> subprocess.CompletedProcess[str]:
        """Run the script as CI invokes it."""
        return subprocess.run(
            [
                sys.executable,
                str(SCRIPT),
                "--repo-root",
                str(TEST_DIRECTORY),
                "--assets",
                str(self.assets),
                "--log",
                str(log),
                "--commit",
                "test-commit",
                "--json-out",
                str(TEST_DIRECTORY / f"{output_name}.json"),
                "--markdown-out",
                str(TEST_DIRECTORY / f"{output_name}.md"),
                *extra,
            ],
            check=False,
            capture_output=True,
            text=True,
        )

    def test_deduplicates_and_classifies_package_diagnostics(self) -> None:
        """Duplicate warnings aggregate while package and source ownership remain distinct."""
        log = TEST_DIRECTORY / "duplicates.log"
        log.write_text(
            "\n".join(
                (
                    "D:\\repo\\src\\App\\Program.cs(10,2): Trim analysis warning IL2070: "
                    "Application reflection warning [D:\\repo\\src\\App\\App.csproj]",
                    "D:\\repo\\src\\App\\Program.cs(10,2): Trim analysis warning IL2070: "
                    "Application reflection warning [D:\\repo\\src\\App\\App.csproj]",
                    "C:\\Users\\builder\\.nuget\\packages\\mono.options\\6.12.0.148\\lib\\"
                    "netstandard2.0\\Mono.Options.dll : warning IL2104: Assembly 'Mono.Options' "
                    "produced trim warnings.",
                    "D:\\repo\\src\\App\\App.csproj : warning NU1903: Package 'Newtonsoft.Json' "
                    "13.0.3 has a known vulnerability.",
                )
            )
            + "\n",
            encoding="utf-8",
        )

        result = self.run_generator(log, "duplicates")

        self.assertEqual(result.returncode, 0, result.stderr)
        report = json.loads((TEST_DIRECTORY / "duplicates.json").read_text(encoding="utf-8"))
        self.assertEqual(report["summary"]["unique_warnings"], 3)
        self.assertEqual(report["summary"]["warning_occurrences"], 4)
        self.assertEqual(report["summary"]["by_owner"]["application"], 1)
        self.assertEqual(report["summary"]["by_owner"]["third-party"], 2)
        package_warning = next(
            warning for warning in report["warnings"] if warning["code"] == "IL2104"
        )
        self.assertEqual(package_warning["package"], {"id": "Mono.Options", "version": "6.12.0.148"})
        self.assertEqual(package_warning["count"], 1)
        self.assertIn("| IL2104 |", (TEST_DIRECTORY / "duplicates.md").read_text(encoding="utf-8"))

    def test_handles_a_log_without_warnings(self) -> None:
        """A no-warning publish still produces valid, empty deterministic reports."""
        log = TEST_DIRECTORY / "clean.log"
        log.write_text("Build succeeded.\n", encoding="utf-8")

        result = self.run_generator(log, "clean")

        self.assertEqual(result.returncode, 0, result.stderr)
        report = json.loads((TEST_DIRECTORY / "clean.json").read_text(encoding="utf-8"))
        self.assertEqual(report["summary"]["unique_warnings"], 0)
        self.assertIn("## Warnings", (TEST_DIRECTORY / "clean.md").read_text(encoding="utf-8"))

    def test_policy_rejects_new_application_warning(self) -> None:
        """A changed PR baseline cannot authorize a new application warning."""
        baseline_log = TEST_DIRECTORY / "baseline.log"
        baseline_log.write_text(
            "D:\\repo\\src\\App\\Program.cs(10,2): Trim analysis warning IL2070: "
            "Known warning [D:\\repo\\src\\App\\App.csproj]\n",
            encoding="utf-8",
        )
        baseline_result = self.run_generator(baseline_log, "baseline")
        self.assertEqual(baseline_result.returncode, 0, baseline_result.stderr)

        changed_log = TEST_DIRECTORY / "changed.log"
        changed_log.write_text(
            "D:\\repo\\src\\App\\Program.cs(11,2): Trim analysis warning IL2070: "
            "New warning [D:\\repo\\src\\App\\App.csproj]\n",
            encoding="utf-8",
        )
        result = self.run_generator(
            changed_log,
            "changed",
            "--baseline",
            str(TEST_DIRECTORY / "baseline.json"),
            "--candidate-baseline",
            str(TEST_DIRECTORY / "changed.json"),
            "--enforce",
        )

        self.assertEqual(result.returncode, 1)
        self.assertIn("Application warning baseline grows outside the protected base", result.stderr)
        self.assertIn("New application warning IL2070", result.stderr)

    def test_policy_reports_malformed_protected_baseline(self) -> None:
        """A malformed protected baseline produces a clear policy failure."""
        log = TEST_DIRECTORY / "known.log"
        log.write_text(
            "D:\\repo\\src\\App\\Program.cs(10,2): Trim analysis warning IL2070: "
            "Known warning [D:\\repo\\src\\App\\App.csproj]\n",
            encoding="utf-8",
        )
        malformed_baseline = TEST_DIRECTORY / "malformed.json"
        malformed_baseline.write_text("{not valid JSON", encoding="utf-8")

        result = self.run_generator(
            log,
            "malformed-output",
            "--baseline",
            str(malformed_baseline),
            "--enforce",
        )

        self.assertEqual(result.returncode, 1)
        self.assertIn("Protected AOT baseline is malformed", result.stderr)

    def test_policy_allows_application_baseline_to_shrink(self) -> None:
        """Removing fixed application warnings from a PR baseline remains allowed."""
        baseline_log = TEST_DIRECTORY / "shrink-baseline.log"
        baseline_log.write_text(
            "D:\\repo\\src\\App\\Program.cs(10,2): Trim analysis warning IL2070: "
            "Known warning [D:\\repo\\src\\App\\App.csproj]\n",
            encoding="utf-8",
        )
        baseline_result = self.run_generator(baseline_log, "shrink-protected")
        self.assertEqual(baseline_result.returncode, 0, baseline_result.stderr)

        clean_log = TEST_DIRECTORY / "shrink-clean.log"
        clean_log.write_text("Build succeeded.\n", encoding="utf-8")
        candidate_result = self.run_generator(clean_log, "shrink-candidate")
        self.assertEqual(candidate_result.returncode, 0, candidate_result.stderr)

        result = self.run_generator(
            baseline_log,
            "shrink-enforcement",
            "--baseline",
            str(TEST_DIRECTORY / "shrink-protected.json"),
            "--candidate-baseline",
            str(TEST_DIRECTORY / "shrink-candidate.json"),
            "--enforce",
        )

        self.assertEqual(result.returncode, 0, result.stderr)

    def test_workflow_materializes_the_pull_request_base_baseline(self) -> None:
        """The CI command must use the protected pull-request base rather than PR JSON."""
        workflow = Path(__file__).parents[3] / ".github" / "workflows" / "ci.yml"
        content = workflow.read_text(encoding="utf-8")

        self.assertIn("BASE_SHA: ${{ github.event.pull_request.base.sha }}", content)
        self.assertIn('git show "${BASE_SHA}:${baseline_source}"', content)
        self.assertIn("--candidate-baseline tools/aot/publisher-module-nativeaot-baseline.json", content)


if __name__ == "__main__":
    unittest.main(verbosity=2)
