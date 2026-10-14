# Builds Releases\InkOverlay-win-Setup.exe with Velopack, and optionally publishes it to GitHub Releases.
#   .\build-release.ps1 -Version 1.0.1
#   .\build-release.ps1 -Version 1.0.1 -Publish
# Needs: dotnet tool install -g vpk. Publishing uses $env:GITHUB_TOKEN, or the token from `gh auth login`.
param(
    [Parameter(Mandatory)] [string] $Version,
    [switch] $Publish
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$repoUrl = 'https://github.com/Busemann-ai/InkOverlay'
$icon = 'Assets\InkOverlay.ico'

function Invoke-Checked {
    & $args[0] $args[1..($args.Length - 1)]
    if ($LASTEXITCODE -ne 0) { throw "$($args[0]) failed with exit code $LASTEXITCODE" }
}

$token = $env:GITHUB_TOKEN
if (-not $token -and (Get-Command gh -ErrorAction SilentlyContinue)) { $token = gh auth token 2>$null }

# The previous release lets vpk build a small delta update. Missing on the first release, which is fine.
if ($token) { vpk download github --repoUrl $repoUrl --token $token -o Releases }
else { vpk download github --repoUrl $repoUrl -o Releases }

if (Test-Path publish) { Remove-Item publish -Recurse -Force }
Invoke-Checked dotnet publish -c Release -r win-x64 --self-contained "-p:Version=$Version" -o publish

$pack = @('pack', '--packId', 'InkOverlay', '--packVersion', $Version, '--packDir', 'publish',
          '--mainExe', 'InkOverlay.exe', '--packTitle', 'InkOverlay', '-o', 'Releases')
if (Test-Path $icon) { $pack += @('--icon', $icon) }
Invoke-Checked vpk @pack

if ($Publish) {
    if (-not $token) { throw 'No GitHub token: set $env:GITHUB_TOKEN or run gh auth login.' }
    Invoke-Checked vpk upload github --repoUrl $repoUrl --token $token -o Releases --publish `
        --tag "v$Version" --releaseName "InkOverlay $Version"
}

Write-Host "Ferdig: Releases\InkOverlay-win-Setup.exe ($Version)"
