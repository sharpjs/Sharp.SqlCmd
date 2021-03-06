<#
.SYNOPSIS
    Sets the version for an automated build.

.DESCRIPTION
    This script merges version information from the source code, branch name, and build counter.

    The resulting version is published as follows:
    * In Local.props, to set assembly and nupkg versions
    * As console output, to set the TeamCity build number

    Code Version  -Branch    -Counter  =>  Version             FileVersion
    ============  =========  ========      ==================  ===========
    1.2.3-local   (none)       (none)  =>  1.2.3-local         1.2.3.43210 (time-based)
    1.2.3-local   stuff           789  =>  1.2.3-stuff-b789    1.2.3.789
    1.2.3-local   456             789  =>  1.2.3-pr456-b789    1.2.3.789
    1.2.3-local   v1.2.3-rc       789  =>  1.2.3-rc            1.2.3.789
    1.2.3-local   v2.3.4-rc       789  =>  *ERROR*             *ERROR*

    Copyright (C) 2019 Jeffrey Sharp

    Permission to use, copy, modify, and distribute this software for any
    purpose with or without fee is hereby granted, provided that the above
    copyright notice and this permission notice appear in all copies.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
    WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
    MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
    ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
    WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
    ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
    OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
#>
param (
    # Name of the branch or tag.  Default: "local".
    [Parameter(Position=1)]
    [ValidateNotNullOrEmpty()]
    [string] $Branch = "local",

    # Build counter.  Default: hours since 2018-01-01 00:00:00 UTC.
    [Parameter(Position=2)]
    [ValidateRange(1, [long]::MaxValue)]
    [long] $Counter = ([DateTime]::Now - [DateTime]::new(2018, 1, 1)).TotalHours
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$PullRequestRegex = [regex] '(?nx)
    ^
    ( pull/ )?
    (?<Number> [1-9][0-9]* )
    $
'

# SemVer 1.0 + restriction that tag parts must start with alpha char
# https://semver.org/spec/v1.0.0.html
$VersionRegex = [regex] '(?nx)
    ^
    ( release/ | v )?
    (?<VersionFull>
        (?<Version>
            ( 0 | [1-9][0-9]* ) \.
            ( 0 | [1-9][0-9]* ) \.
            ( 0 | [1-9][0-9]* )
        )
        # Pre-release tag
        (
            - ( [a-zA-Z][0-9a-zA-Z]* )?
        )*
    )
    $
'

# Get code's version string
$PropsPath    = Join-Path $PSScriptRoot General.props
$PropsXml     = [xml] (Get-Content -LiteralPath $PropsPath -Raw -Encoding UTF8)
$PropsVersion = $PropsXml.Project.PropertyGroup[0].Version

# Parse code's version string
if ($PropsVersion -match $VersionRegex) {
    $Version     = [version] $Matches.Version     # 1.2.3
    $VersionFull = [string]  $Matches.VersionFull # 1.2.3-beta4
}
else {
    throw "General.props: Version property value '$PropsVersion' is not a valid version."
}

# Merge branch name and build counter into code version, producing a full
# version (= TC build number) that satisfies these requirements:
#   - is unique
#   - is a valid NuGet/SemVer1 version string
#   - has a branch label
if ($Branch -match $VersionRegex) {
    # Branch name contains a version string (ex: a release scenario)
    $BranchVersion     = [version] $Matches.Version     # 1.2.3
    $BranchVersionFull = [string]  $Matches.VersionFull # 1.2.3-beta4
    $VersionIsTagged   = "true"

    # Verify branch/code versions have equal numbers
    if ($BranchVersion -ne $Version) {
        throw "Branch version ($BranchVersion) does not match code version ($Version)."
    }

    # Use branch version verbatim
    $VersionFull = $BranchVersionFull
}
else {
    # Branch name is not a version string
    $VersionIsTagged = "false"

    # Start with code version numbers (1.2.3)
    $VersionFull = $Version.ToString()

    # Then add the branch name (1.2.3-foo)
    if ($Branch) {
        $Branch = $Branch -replace '^(?:pull/)?([1-9][0-9]*)$', 'pr$1' # Pull requests
        $Branch = $Branch -replace '[^0-9a-zA-Z-]+',            '-'    # Invalid version chars
        $Branch = $Branch -replace '(?<=^|-)([0-9])',           'n$1'  # Tag parts starting with number
        $VersionFull += "-$Branch"
    }

    # Then add the build counter (1.2.3-foo-b789)
    if ($Counter) {
        $VersionFull += "-b$Counter"
    }
}

# File version components are 16-bit numbers
$Counter = $Counter -band 0xFFFF

# For .NET-Core-style projects
Set-Content Local.props -Encoding UTF8 -Value $(
    "<Project>"
    ""
    "  <!-- Generated by Set-Version.ps1 -->"
    ""
    "  <PropertyGroup>"
    "    <Version>$VersionFull</Version>"
    "    <FileVersion>$Version.$Counter</FileVersion>"
    "    <VersionIsTagged>$VersionIsTagged</VersionIsTagged>"
    "  </PropertyGroup>"
    ""
    "</Project>"
)

# Tell TeamCity the new build number
Write-Output "##teamcity[buildNumber '$VersionFull']"
