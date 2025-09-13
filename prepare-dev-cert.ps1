param(
    [string]$OutputDir = (Join-Path $PSScriptRoot ".certs"),
    [string]$Password = "devcert-password"
)

$ErrorActionPreference = 'Stop'

Write-Host "Ensuring output directory exists: $OutputDir"
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

Write-Host "Ensuring ASP.NET Core HTTPS development certificate is trusted..."
dotnet dev-certs https --trust | Out-Null

$pfxPath = Join-Path $OutputDir "aspnetapp.pfx"
Write-Host "Exporting development certificate to: $pfxPath"
dotnet dev-certs https -ep "$pfxPath" -p "$Password" | Out-Null

Write-Host "Dev certificate exported."
Write-Host "PFX path: $pfxPath"
Write-Host "Use this password in your docker-compose or env: $Password"
