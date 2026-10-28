# ==============================================================================
# Build Script for Native Windows WebView2 Desktop To-Do Widget
# Replaces 300MB Electron with ~2MB Native Windows Executable
# ==============================================================================

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

$outputDir = Join-Path $scriptDir "dist-native"
if (Test-Path $outputDir) {
    Remove-Item $outputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

Write-Host ">>> Ensuring WebView2 NuGet package..." -ForegroundColor Cyan
$extractedDir = Join-Path $scriptDir "build_native\webview2_extracted"
if (-not (Test-Path $extractedDir)) {
    New-Item -ItemType Directory -Path "build_native" -Force | Out-Null
    $nupkgUrl = "https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/1.0.4191.47/microsoft.web.webview2.1.0.4191.47.nupkg"
    $nupkgPath = Join-Path $scriptDir "build_native\webview2.zip"
    Write-Host "Downloading WebView2 SDK package..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri $nupkgUrl -OutFile $nupkgPath
    Expand-Archive -Path $nupkgPath -DestinationPath $extractedDir -Force
}

# Copy required WebView2 runtime DLLs
Write-Host ">>> Copying WebView2 libraries..." -ForegroundColor Cyan
Copy-Item (Join-Path $extractedDir "lib\net462\Microsoft.Web.WebView2.WinForms.dll") $outputDir
Copy-Item (Join-Path $extractedDir "lib\net462\Microsoft.Web.WebView2.Core.dll") $outputDir
Copy-Item (Join-Path $extractedDir "runtimes\win-x64\native\WebView2Loader.dll") $outputDir

# Copy web application assets
Write-Host ">>> Copying src and assets..." -ForegroundColor Cyan
Copy-Item (Join-Path $scriptDir "src") -Destination (Join-Path $outputDir "src") -Recurse
Copy-Item (Join-Path $scriptDir "assets") -Destination (Join-Path $outputDir "assets") -Recurse

# Compile C# Native Executable
Write-Host ">>> Compiling Native Windows Executable..." -ForegroundColor Cyan
$cscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $cscPath)) {
    throw "C# compiler not found at $cscPath"
}

$sourceFile = Join-Path $scriptDir "src-native\Program.cs"
$exeOutput = Join-Path $outputDir "DesktopToDoWidget.exe"
$iconPath = Join-Path $scriptDir "assets\icon.ico"

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
    "/reference:$outputDir\Microsoft.Web.WebView2.WinForms.dll",
    "/reference:$outputDir\Microsoft.Web.WebView2.Core.dll",
    "/out:$exeOutput",
    "$sourceFile"
)

& $cscPath $cscArgs

if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE"
}

Write-Host "`n========================================================" -ForegroundColor Green
Write-Host ">>> BUILD SUCCESSFUL!" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green

$exeSize = (Get-Item $exeOutput).Length / 1KB
$totalSize = (Get-ChildItem -Recurse $outputDir -File | Measure-Object -Property Length -Sum).Sum / 1MB

Write-Host ("DesktopToDoWidget.exe Size: {0:N2} KB" -f $exeSize) -ForegroundColor Yellow
Write-Host ("Entire Distribution Folder Size: {0:N2} MB" -f $totalSize) -ForegroundColor Yellow
Write-Host "Location: $outputDir\DesktopToDoWidget.exe" -ForegroundColor Cyan
Write-Host "========================================================`n" -ForegroundColor Green
