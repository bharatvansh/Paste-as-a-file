$ErrorActionPreference = "Stop"

Write-Host "Compiling PasteIt Solution (Release Mode)..." -ForegroundColor Cyan
dotnet build .\PasteIt.sln -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet build failed. Installer will not be built."
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
& $InnoSetupCompiler .\PasteIt.iss

if ($LASTEXITCODE -ne 0) {
    Write-Error "Inno Setup failed to build the installer."
    exit 1
}

Write-Host ""
Write-Host "SUCCESS! The installer has been created at:" -ForegroundColor Green
Write-Host "$PSScriptRoot\Output\PasteIt_Setup.exe" -ForegroundColor Green
