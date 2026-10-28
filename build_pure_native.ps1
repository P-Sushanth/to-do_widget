# ==============================================================================
# Build Script for Pure Native Windows To-Do Widget
# Zero Chromium - Zero WebView2 - Single Standalone .exe (~180 KB)
# ==============================================================================

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

$outputDir = Join-Path $scriptDir "dist-pure-native"
if (Test-Path $outputDir) {
    Remove-Item $outputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

$cscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $cscPath)) {
    throw "C# compiler not found at $cscPath"
}

$sourceFile = Join-Path $scriptDir "src-pure-native\Program.cs"
$exeOutput = Join-Path $outputDir "DesktopToDoWidget-PureNative.exe"
$iconPath = Join-Path $scriptDir "assets\icon.ico"

Write-Host ">>> Compiling Pure Native Windows Widget..." -ForegroundColor Cyan

$cscArgs = @(
    "/nologo",
    "/target:winexe",
    "/optimize+",
    "/platform:x64",
    "/win32icon:$iconPath",
    "/reference:System.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.Web.Extensions.dll",
    "/out:$exeOutput",
    "$sourceFile"
)

& $cscPath $cscArgs

if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE"
}

# Copy assets folder for tray icon lookup if needed
$assetsDir = Join-Path $outputDir "assets"
New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null
Copy-Item (Join-Path $scriptDir "assets\icon.ico") (Join-Path $assetsDir "icon.ico")

$exeSize = (Get-Item $exeOutput).Length / 1KB

Write-Host "`n========================================================" -ForegroundColor Green
Write-Host ">>> PURE NATIVE BUILD SUCCESSFUL!" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
Write-Host ("Executable Size: {0:N2} KB" -f $exeSize) -ForegroundColor Yellow
Write-Host "Output: $exeOutput" -ForegroundColor Cyan
Write-Host "========================================================`n" -ForegroundColor Green
