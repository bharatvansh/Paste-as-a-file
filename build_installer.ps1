$ErrorActionPreference = "Stop"

[xml]$BuildProps = Get-Content (Join-Path $PSScriptRoot "Directory.Build.props")
$AppVersion = $BuildProps.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($AppVersion)) {
    Write-Error "Version was not found in Directory.Build.props."
    exit 1
}

$BundledFfmpegPath = Join-Path $PSScriptRoot "ThirdParty\FFmpeg\ffmpeg.exe"
if (-not (Test-Path $BundledFfmpegPath)) {
    Write-Error "Bundled FFmpeg was not found at $BundledFfmpegPath. Add ffmpeg.exe there before building the installer."
    exit 1
}

$BundledFfmpegLicensePath = Join-Path $PSScriptRoot "ThirdParty\FFmpeg\LICENSE.txt"
if (-not (Test-Path $BundledFfmpegLicensePath)) {
    Write-Error "Bundled FFmpeg license file was not found at $BundledFfmpegLicensePath. Add the bundled LICENSE there before building the installer."
    exit 1
}

$DotnetCommand = $null
$DotnetCandidate = Get-Command dotnet.exe -ErrorAction SilentlyContinue
if ($DotnetCandidate) {
    $DotnetCommand = $DotnetCandidate.Source
}

if (-not $DotnetCommand) {
    $FallbackDotnet = "C:\Program Files\dotnet\dotnet.exe"
    if (Test-Path $FallbackDotnet) {
        $DotnetCommand = $FallbackDotnet
    }
}

if (-not $DotnetCommand) {
    Write-Error "dotnet.exe was not found on PATH or at C:\Program Files\dotnet\dotnet.exe."
    exit 1
}

$InstallerArtifactsRoot = Join-Path $PSScriptRoot ".installer-artifacts"
$BaseOutputPath = Join-Path $InstallerArtifactsRoot "bin\"
$ServicePublishDir = Join-Path $InstallerArtifactsRoot "service-publish\"
$UiPublishDir = Join-Path $InstallerArtifactsRoot "ui-publish\"

if (Test-Path $InstallerArtifactsRoot) {
    Remove-Item $InstallerArtifactsRoot -Recurse -Force -ErrorAction SilentlyContinue
}

New-Item -ItemType Directory -Path $InstallerArtifactsRoot -Force | Out-Null

Write-Host "Building shell extension (net48)..." -ForegroundColor Cyan
Push-Location $PSScriptRoot

& $DotnetCommand build .\PasteItExtension\PasteItExtension.csproj -c Release -f net48

if ($LASTEXITCODE -ne 0) {
    Pop-Location
    Write-Error "Shell extension build failed. Installer will not be built."
    exit 1
}

Write-Host "Publishing PasteIt service (net8.0-windows, win-x64, self-contained)..." -ForegroundColor Cyan
& $DotnetCommand publish .\PasteIt\PasteIt.csproj -c Release -r win-x64 --self-contained true "-p:BaseOutputPath=$BaseOutputPath" "-p:PublishDir=$ServicePublishDir"

if ($LASTEXITCODE -ne 0) {
    Pop-Location
    Write-Error "PasteIt publish failed. Installer will not be built."
    exit 1
}

Write-Host "Publishing PasteIt UI (net8.0-windows, win-x64, self-contained)..." -ForegroundColor Cyan
& $DotnetCommand publish .\PasteIt.UI\PasteIt.UI.csproj -c Release -r win-x64 --self-contained true "-p:BaseOutputPath=$BaseOutputPath" "-p:PublishDir=$UiPublishDir"

if ($LASTEXITCODE -ne 0) {
    Pop-Location
    Write-Error "PasteIt UI publish failed. Installer will not be built."
    exit 1
}

$InnoSetupCompiler = $null
$IsccCommand = Get-Command ISCC.exe -ErrorAction SilentlyContinue

if ($IsccCommand) {
    $InnoSetupCompiler = $IsccCommand.Source
}

if (-not $InnoSetupCompiler) {
    $FallbackCompiler = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (Test-Path $FallbackCompiler) {
        $InnoSetupCompiler = $FallbackCompiler
    }
}

if (-not $InnoSetupCompiler) {
    Write-Error "Inno Setup compiler (ISCC.exe) was not found on PATH or at C:\Program Files (x86)\Inno Setup 6\ISCC.exe. Please install Inno Setup 6."
    exit 1
}

Write-Host ""
Write-Host "Building PasteIt_Setup.exe using Inno Setup..." -ForegroundColor Cyan

$ServicePublishDirForInno = [System.IO.Path]::GetFullPath($ServicePublishDir).TrimEnd('\')
$UiPublishDirForInno = [System.IO.Path]::GetFullPath($UiPublishDir).TrimEnd('\')

& $InnoSetupCompiler "/DAppVersion=$AppVersion" "/DServicePublishDir=$ServicePublishDirForInno" "/DUiPublishDir=$UiPublishDirForInno" .\PasteIt.iss

if ($LASTEXITCODE -ne 0) {
    Pop-Location
    Write-Error "Inno Setup failed to build the installer."
    exit 1
}

Pop-Location

Write-Host ""
Write-Host "SUCCESS! The installer has been created at:" -ForegroundColor Green
Write-Host "$PSScriptRoot\Output\PasteIt_Setup.exe" -ForegroundColor Green
