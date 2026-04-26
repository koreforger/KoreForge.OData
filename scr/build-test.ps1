<#
.SYNOPSIS
    Build and test KoreForge.OData.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot\..

Write-Host '── Restore ──' -ForegroundColor Cyan
dotnet restore KoreForge.OData.slnx
if ($LASTEXITCODE -ne 0) { throw 'Restore failed' }

Write-Host '── Build ──' -ForegroundColor Cyan
dotnet build KoreForge.OData.slnx -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

Write-Host '── Test ──' -ForegroundColor Cyan
dotnet test KoreForge.OData.slnx -c $Configuration --no-build --verbosity normal
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }

Write-Host '✓ All done.' -ForegroundColor Green
Pop-Location
