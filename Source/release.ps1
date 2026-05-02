#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Creates a formal release: bumps version, syncs About.xml, commits, and tags.
.DESCRIPTION
    1. Runs dotnet versionize to bump version + generate CHANGELOG.
    2. Syncs About/About.xml <modVersion> to the new version.
    3. Amends the Versionize commit to include the About.xml change.
    4. Updates the git tag to point to the amended commit.
    5. Prompts you to push.
#>

$ErrorActionPreference = "Stop"

$ScriptDir = $PSScriptRoot
if (-not $ScriptDir) { $ScriptDir = Get-Location }
$RepoRoot = Split-Path -Parent $ScriptDir
Set-Location $RepoRoot

# 1. Ensure working tree is clean
$status = git status --porcelain
if ($status) {
    throw "Working tree has uncommitted changes. Commit or stash them first."
}

# 2. Run Versionize
Write-Host "=== Running Versionize ===" -ForegroundColor Cyan
dotnet versionize --configDir "$ScriptDir\"
if ($LASTEXITCODE -ne 0) { throw "Versionize failed" }

# 3. Read new version from Directory.Build.props
Write-Host "=== Reading new version ===" -ForegroundColor Cyan
$dbpPath = Join-Path $ScriptDir "Directory.Build.props"
[xml]$dbpXml = Get-Content $dbpPath
$version = $dbpXml.Project.PropertyGroup.Version
if (-not $version) { throw "Could not read Version from Directory.Build.props" }
Write-Host "New version: $version" -ForegroundColor Green

# 4. Update About.xml
Write-Host "=== Updating About.xml ===" -ForegroundColor Cyan
$aboutPath = Join-Path $RepoRoot "About\About.xml"
[xml]$aboutXml = Get-Content $aboutPath
$aboutXml.ModMetaData.modVersion = $version
$aboutXml.Save($aboutPath)
Write-Host "About.xml updated to $version" -ForegroundColor Green

# 5. Amend commit and retag
Write-Host "=== Amending commit to include About.xml ===" -ForegroundColor Cyan
git add About/About.xml
git commit --amend --no-edit
if ($LASTEXITCODE -ne 0) { throw "Failed to amend commit" }

$tag = "v$version"
git tag -f $tag
if ($LASTEXITCODE -ne 0) { throw "Failed to update tag '$tag'" }

# 6. Done
Write-Host "=== Release v$version ready ===" -ForegroundColor Green
Write-Host ""
Write-Host "Run the following to push:" -ForegroundColor Yellow
Write-Host "  git push --follow-tags" -ForegroundColor Cyan
