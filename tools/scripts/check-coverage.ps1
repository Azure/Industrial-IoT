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
$branchHits = @{}    # package -> "file:line" -> [covered, total]

foreach ($file in $reportFiles) {
    [xml] $doc = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($package in $doc.coverage.packages.package) {
        $name = $package.name
        if (-not $name) { continue }
        if (-not $lineHits.ContainsKey($name)) {
            $lineHits[$name] = @{}
            $branchHits[$name] = @{}
        }
        foreach ($class in $package.classes.class) {
            $fileName = $class.filename
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
                    if (-not $branchHits[$name].ContainsKey($key) -or
                        $branchHits[$name][$key][0] -lt $covered) {
                        $branchHits[$name][$key] = @($covered, $total)
                    }
                }
            }
        }
    }
}

$stats = @{}
foreach ($name in $lineHits.Keys) {
    $linesValid = $lineHits[$name].Count
    $linesCovered = 0
    foreach ($hits in $lineHits[$name].Values) {
        if ($hits -gt 0) { $linesCovered++ }
    }
    $branchesValid = 0
    $branchesCovered = 0
    foreach ($pair in $branchHits[$name].Values) {
        $branchesCovered += $pair[0]
        $branchesValid += $pair[1]
    }
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
    $existing = @{}
    if (Test-Path $ThresholdPath) {
        $existing = Get-Content -LiteralPath $ThresholdPath -Raw | ConvertFrom-Json -AsHashtable
    }
    $updated = [ordered]@{}
    foreach ($name in $measured.Keys) {
        $line = $measured[$name].line
        $branch = $measured[$name].branch
        if ($existing.ContainsKey($name)) {
            # A floor only ever moves up.
            if ($existing[$name].line -gt $line) { $line = $existing[$name].line }
            if ($existing[$name].branch -gt $branch) { $branch = $existing[$name].branch }
        }
        $updated[$name] = [ordered]@{ line = $line; branch = $branch }
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
