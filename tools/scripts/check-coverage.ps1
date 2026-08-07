<#
 .SYNOPSIS
    Checks per-assembly code coverage against the project floors.

 .DESCRIPTION
    Reads merged Cobertura output and fails when a shipping assembly falls
    below its line or branch floor.

    The floors are per assembly rather than aggregate on purpose. An aggregate
    is satisfied by covering the roughly 300 DTO records in
    Azure.IIoT.OpcUa.Publisher.Models while the Stack layer stays thin, which
    is the opposite of what the number is meant to report.

    Thresholds are data, not policy baked into CI: while an assembly is being
    brought up to the target its floor sits at whatever was last measured, so
    it can only ever move up. Lower an entry only with a reason.

 .PARAMETER ReportPath
    Merged Cobertura xml, or a directory searched recursively for
    *.cobertura.xml.

 .PARAMETER ThresholdPath
    Json file mapping assembly name to { line, branch }. Defaults to
    coverage-thresholds.json beside this script.

 .PARAMETER UpdateBaseline
    Rewrites the threshold file with the measured values instead of checking.
    Used to ratchet a floor up after an assembly improves. Never lowers a
    floor.
#>
param(
    [Parameter(Mandatory = $true)][string] $ReportPath,
    [string] $ThresholdPath,
    [switch] $UpdateBaseline
)

$ErrorActionPreference = 'Stop'

if (-not $ThresholdPath) {
    $ThresholdPath = Join-Path $PSScriptRoot 'coverage-thresholds.json'
}

$reportFiles = @()
if (Test-Path $ReportPath -PathType Container) {
    $reportFiles = @(Get-ChildItem -Path $ReportPath -Recurse -Filter '*.cobertura.xml')
} elseif (Test-Path $ReportPath -PathType Leaf) {
    $reportFiles = @(Get-Item $ReportPath)
}

if ($reportFiles.Count -eq 0) {
    Write-Error "No Cobertura report found at '$ReportPath'. Coverage cannot be verified, which is a failure rather than a pass."
    exit 1
}

#
# A package appears once per test project that touched it, so the runs are
# combined as a union over (file, line): a line covered by any suite is
# covered. Summing the per-run totals instead would count every line once per
# run it appears in, inflating the denominator and scoring a line covered by
# one of three suites as a third of a line.
#
$lineHits = @{}      # package -> "file:line" -> max hits
$branchHits = @{}    # package -> "file:line" -> total
$branchCovered = @{} # package -> "file:line" -> max covered
#
# package -> report -> countable line total, used only to detect a stale build.
#
$perReportTotals = @{}

foreach ($file in $reportFiles) {
    [xml] $doc = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($package in $doc.coverage.packages.package) {
        $name = $package.name
        if (-not $name) { continue }
        if (-not $lineHits.ContainsKey($name)) {
            $lineHits[$name] = @{}
            $branchHits[$name] = @{}
            $branchCovered[$name] = @{}
            $perReportTotals[$name] = @{}
        }
        $reportTotal = 0
        foreach ($class in $package.classes.class) {
            $reportTotal += $class.lines.line.Count
        }
        if ($reportTotal -gt 0) {
            $perReportTotals[$name][$file.FullName] = $reportTotal
        }
        foreach ($class in $package.classes.class) {
            #
            # SourceLink filenames embed the commit, so the same file appears
            # under two names once reports from different commits are mixed and
            # the union counts it twice. Normalising to the repository relative
            # path makes a report mergeable regardless of which commit produced
            # it - and makes stale data show up as an unchanged total rather
            # than a doubled one.
            #
            $fileName = $class.filename
            if ($fileName -match '^https?://.*?/[0-9a-fA-F]{40}/(.+)$') {
                $fileName = $Matches[1]
            }
            $fileName = $fileName -replace '\\', '/'
            foreach ($line in $class.lines.line) {
                $key = "$fileName`:$($line.number)"
                $hits = [int] $line.hits
                if (-not $lineHits[$name].ContainsKey($key) -or
                    $lineHits[$name][$key] -lt $hits) {
                    $lineHits[$name][$key] = $hits
                }
                if ($line.'condition-coverage' -and
                    $line.'condition-coverage' -match '\((\d+)/(\d+)\)') {
                    $covered = [int] $Matches[1]
                    $total = [int] $Matches[2]
                    $branchHits[$name][$key] = $total
                    #
                    # Cobertura reports how many of a line's branches a run
                    # took, not which ones, so two runs that each took a
                    # different branch of the same two-way cannot be unioned -
                    # the best available answer is the run that took the most.
                    # That makes the branch figure a lower bound rather than an
                    # exact union. Lines do not have this problem: hit counts
                    # are per line, so max hits is a true union.
                    #
                    if (-not $branchCovered[$name].ContainsKey($key) -or
                        $branchCovered[$name][$key] -lt $covered) {
                        $branchCovered[$name][$key] = $covered
                    }
                }
            }
        }
    }
}

#
# An assembly must present the same countable lines to every suite that touched
# it. When it does not, at least one test project's output folder is holding a
# stale copy, and the union quietly restores whatever that copy still contains -
# code since deleted, or excluded, or moved. This was not hypothetical: after
# three files were marked ExcludeFromCodeCoverage, two test projects were not
# rebuilt and their stale copies put 445 excluded lines back into the
# denominator, understating one assembly by seven points.
#
# It is reported as a failure rather than a warning because the resulting
# number looks entirely plausible - there is nothing else about it that says
# the build was stale.
#
$staleAssemblies = @()
#
# The suite is the first directory under the report root. Taking a fixed
# number of parents instead breaks whenever the runner nests results deeper
# than <suite>/<guid>/ - the data collector sometimes writes an extra In/
# <machine>/ pair, and the guard then names every such report "In", which is
# useless precisely when it matters.
#
$reportRoot = $null
if (Test-Path $ReportPath -PathType Container) {
    $reportRoot = (Resolve-Path $ReportPath).ProviderPath.TrimEnd([IO.Path]::DirectorySeparatorChar)
}
function Get-SuiteName([string] $reportFile) {
    if ($reportRoot) {
        $full = (Resolve-Path -LiteralPath $reportFile).ProviderPath
        if ($full.StartsWith($reportRoot, [StringComparison]::OrdinalIgnoreCase)) {
            $relative = $full.Substring($reportRoot.Length).TrimStart(
                [IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
            $first = ($relative -split '[\\/]')[0]
            if ($first -and $first -ne (Split-Path -Leaf $reportFile)) { return $first }
        }
    }
    return Split-Path -Leaf (Split-Path -Parent $reportFile)
}
foreach ($name in ($perReportTotals.Keys | Sort-Object)) {
    $totals = $perReportTotals[$name]
    $distinct = $totals.Values | Sort-Object -Unique
    if ($distinct.Count -le 1) { continue }
    #
    # One suite can emit more than one report, so collapse by suite and keep
    # the line totals seen for it. Listing the same suite eight times buries
    # the one line that differs.
    #
    $bySuite = [ordered]@{}
    foreach ($entry in ($totals.GetEnumerator() | Sort-Object Value)) {
        $suite = Get-SuiteName $entry.Key
        if (-not $bySuite.Contains($suite)) { $bySuite[$suite] = @() }
        $bySuite[$suite] += $entry.Value
    }
    $detail = ($bySuite.GetEnumerator() | ForEach-Object {
        "      {0,6} lines  {1}" -f (($_.Value | Sort-Object -Unique) -join '/'), $_.Key
    }) -join "`n"
    $staleAssemblies += "  $name reports $($distinct -join ' / ') lines across reports:`n$detail"
}

if ($staleAssemblies.Count -gt 0) {
    Write-Host ''
    Write-Host 'Stale build detected - refusing to report coverage.' -ForegroundColor Red
    Write-Host ''
    $staleAssemblies | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    Write-Host ''
    Write-Host 'The same assembly was measured with different content, so at least one'
    Write-Host 'test project is running against an out of date copy of it. Rebuild'
    Write-Host 'everything and measure again:'
    Write-Host ''
    Write-Host '    dotnet build Industrial-IoT.slnx --no-restore'
    Write-Host ''
    exit 1
}

$stats = @{}
foreach ($name in $lineHits.Keys) {
    $linesValid = $lineHits[$name].Count
    $linesCovered = 0
    foreach ($hits in $lineHits[$name].Values) {
        if ($hits -gt 0) { $linesCovered++ }
    }
    $branchesValid = 0
    foreach ($total in $branchHits[$name].Values) { $branchesValid += $total }
    $branchesCovered = 0
    foreach ($covered in $branchCovered[$name].Values) { $branchesCovered += $covered }
    $stats[$name] = [ordered]@{
        LinesCovered = $linesCovered; LinesValid = $linesValid
        BranchesCovered = $branchesCovered; BranchesValid = $branchesValid
    }
}

if ($stats.Count -eq 0) {
    Write-Error "The Cobertura report contained no packages. Check the coverage.runsettings include list."
    exit 1
}

function Get-Rate([int] $covered, [int] $valid) {
    if ($valid -le 0) { return $null }
    return [math]::Round(100.0 * $covered / $valid, 2)
}

$measured = [ordered]@{}
foreach ($name in ($stats.Keys | Sort-Object)) {
    $s = $stats[$name]
    $measured[$name] = [ordered]@{
        line = Get-Rate $s.LinesCovered $s.LinesValid
        branch = Get-Rate $s.BranchesCovered $s.BranchesValid
        lines = "$($s.LinesCovered)/$($s.LinesValid)"
        branches = "$($s.BranchesCovered)/$($s.BranchesValid)"
    }
}

Write-Host ''
Write-Host 'Coverage by assembly'
Write-Host '--------------------'
foreach ($name in $measured.Keys) {
    $m = $measured[$name]
    '{0,-42} line {1,6}% ({2,-13}) branch {3,6}% ({4})' -f `
        $name, $m.line, $m.lines, $m.branch, $m.branches | Write-Host
}
Write-Host ''

if ($UpdateBaseline) {
    #
    # A floor never exceeds the target. Ratcheting to the measured value is
    # what stops regression while an assembly is being brought up, but once it
    # is at the target, pinning the floor to a peak of 97% would fail the build
    # for deleting a covered line - which is not what the number is for.
    #
    # A floor also sits slightly below what was measured. Coverage here is not
    # perfectly reproducible: two clean runs of the same commit differed by
    # over a point on one assembly's branch coverage, because some tests race
    # over paths that a timing difference decides. A floor set to the exact
    # measurement would fail the next honest run, and a gate that cries wolf
    # gets disabled.
    #
    $targetLine = 85.0
    $targetBranch = 70.0
    $margin = 2.0
    $existing = @{}
    if (Test-Path $ThresholdPath) {
        $existing = Get-Content -LiteralPath $ThresholdPath -Raw | ConvertFrom-Json -AsHashtable
    }
    $updated = [ordered]@{}
    #
    # Seed from what is already recorded. Rewriting only from what was measured
    # would silently drop the floor for any assembly missing from this report -
    # one run against a partial result set would ungate it entirely, which is
    # the opposite of a ratchet and exactly what the absent-assembly check
    # below exists to catch.
    #
    foreach ($name in ($existing.Keys | Sort-Object)) {
        $updated[$name] = [ordered]@{
            line = $existing[$name].line; branch = $existing[$name].branch
        }
    }
    foreach ($name in $measured.Keys) {
        $line = [math]::Round([math]::Max(0.0, $measured[$name].line - $margin), 2)
        $branch = [math]::Round([math]::Max(0.0, $measured[$name].branch - $margin), 2)
        if ($existing.ContainsKey($name)) {
            # A floor only ever moves up.
            if ($existing[$name].line -gt $line) { $line = $existing[$name].line }
            if ($existing[$name].branch -gt $branch) { $branch = $existing[$name].branch }
        }
        # The cap wins over the ratchet, including over a floor recorded before
        # the cap existed.
        $updated[$name] = [ordered]@{
            line = [math]::Min($line, $targetLine)
            branch = [math]::Min($branch, $targetBranch)
        }
    }
    $updated | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $ThresholdPath -Encoding utf8
    Write-Host "Wrote floors to $ThresholdPath"
    exit 0
}

if (-not (Test-Path $ThresholdPath)) {
    Write-Error "No threshold file at '$ThresholdPath'. Run with -UpdateBaseline to create one."
    exit 1
}

$thresholds = Get-Content -LiteralPath $ThresholdPath -Raw | ConvertFrom-Json -AsHashtable
$failures = @()

foreach ($name in $thresholds.Keys) {
    if (-not $measured.Contains($name)) {
        #
        # An assembly that was measured before and is now absent is a silent
        # loss of coverage, not a pass: the usual cause is a test project
        # dropping out of the matrix.
        #
        $failures += "$name : expected in the report but absent. Did its test project stop running?"
        continue
    }
    $m = $measured[$name]
    $t = $thresholds[$name]
    if ($null -ne $m.line -and $m.line -lt $t.line) {
        $failures += ('{0} : line {1}% is below the floor of {2}%' -f $name, $m.line, $t.line)
    }
    if ($null -ne $m.branch -and $m.branch -lt $t.branch) {
        $failures += ('{0} : branch {1}% is below the floor of {2}%' -f $name, $m.branch, $t.branch)
    }
}

if ($failures.Count -gt 0) {
    Write-Host 'Coverage below floor:' -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    Write-Host ''
    Write-Host 'Add tests, or if the code is genuinely untestable mark it'
    Write-Host '[ExcludeFromCodeCoverage] with a justification.'
    exit 1
}

Write-Host 'All assemblies meet their coverage floor.' -ForegroundColor Green
exit 0
