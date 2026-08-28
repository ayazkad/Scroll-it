$ErrorActionPreference = "Stop"

$frameworkDir = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
if (-not (Test-Path $frameworkDir)) {
    $frameworkDir = "C:\Windows\Microsoft.NET\Framework\v4.0.30319"
}

$csc = Join-Path $frameworkDir "csc.exe"
$wpfDir = Join-Path $frameworkDir "WPF"

if (-not (Test-Path $csc)) {
    Write-Error "Compilateur C# (csc.exe) introuvable."
    exit 1
}

$outputDir = Join-Path $PSScriptRoot "bin"
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

$iconFile = Join-Path $PSScriptRoot "src\scroll-it.ico"

$references = @(
    "System.dll",
    "System.Core.dll",
    "System.Drawing.dll",
    "System.Windows.Forms.dll",
    "System.Xaml.dll",
    "System.Runtime.Serialization.dll",
    (Join-Path $wpfDir "PresentationCore.dll"),
    (Join-Path $wpfDir "PresentationFramework.dll"),
    (Join-Path $wpfDir "WindowsBase.dll")
)
$refArgs = $references | ForEach-Object { "/r:$_" }

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  Compilation du projet Scroll-It                 " -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

# Stop running instances if any to release file locks
Get-Process -Name "Scroll-it", "Scroll-it-Portable", "scroll-it" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 200

# 1. Compile Portable Application (Scroll-it-Portable.exe)
Write-Host "[1/2] Compilation de Scroll-it-Portable.exe..." -ForegroundColor Yellow
$mainSources = @(
    (Join-Path $PSScriptRoot "src\AssemblyInfo.cs"),
    (Join-Path $PSScriptRoot "src\Engine\Localization.cs"),
    (Join-Path $PSScriptRoot "src\Engine\Win32.cs"),
    (Join-Path $PSScriptRoot "src\Engine\SettingsManager.cs"),
    (Join-Path $PSScriptRoot "src\Engine\WebKitMomentumScroller.cs"),
    (Join-Path $PSScriptRoot "src\Engine\ScrollPhysics.cs"),
    (Join-Path $PSScriptRoot "src\Engine\MouseHook.cs"),
    (Join-Path $PSScriptRoot "src\UI\Styles.cs"),
    (Join-Path $PSScriptRoot "src\UI\TrayManager.cs"),
    (Join-Path $PSScriptRoot "src\UI\MainWindow.cs"),
    (Join-Path $PSScriptRoot "src\Program.cs")
)
$portableExe = Join-Path $outputDir "Scroll-it-Portable.exe"
$params1 = @("/target:winexe", "/optimize+", "/platform:anycpu", "/win32icon:$iconFile", "/out:$portableExe") + $refArgs + $mainSources
& $csc $params1
if ($LASTEXITCODE -ne 0) { Write-Error "Échec de la compilation de Scroll-it-Portable.exe"; exit 1 }

# 2. Compile Unified Setup & Uninstaller Wizard (Scroll-it-Setup.exe) - Standalone Self-Contained
Write-Host "[2/2] Compilation de Scroll-it-Setup.exe (Installateur autonome tout-en-un)..." -ForegroundColor Yellow
$setupSources = @(
    (Join-Path $PSScriptRoot "src\Engine\Localization.cs"),
    (Join-Path $PSScriptRoot "src\Setup\UninstallWindow.cs"),
    (Join-Path $PSScriptRoot "src\Setup\SetupWindow.cs"),
    (Join-Path $PSScriptRoot "src\Setup\SetupProgram.cs")
)
$setupExe = Join-Path $outputDir "Scroll-it-Setup.exe"
$embeddedResources = @(
    "/resource:$portableExe,Scroll-it.exe",
    "/resource:$iconFile,scroll-it.ico"
)
$params2 = @("/target:winexe", "/optimize+", "/platform:anycpu", "/win32icon:$iconFile", "/out:$setupExe") + $embeddedResources + $refArgs + $setupSources
& $csc $params2
if ($LASTEXITCODE -ne 0) { Write-Error "Échec de la compilation de Scroll-it-Setup.exe"; exit 1 }

# Clean any redundant/temporary files in bin
$tempMain = Join-Path $outputDir "Scroll-it.exe"
if (Test-Path $tempMain) { Remove-Item $tempMain -Force -ErrorAction SilentlyContinue }
$oldUninstall = Join-Path $outputDir "Uninstall.exe"
if (Test-Path $oldUninstall) { Remove-Item $oldUninstall -Force -ErrorAction SilentlyContinue }

Write-Host ""
Write-Host "==================================================" -ForegroundColor Green
Write-Host "  Compilation réussie ! Fichiers finaux dans /bin: " -ForegroundColor Green
Write-Host "  1. bin\Scroll-it-Setup.exe    (Installateur autonome tout-en-un 100% autonome)" -ForegroundColor Yellow
Write-Host "  2. bin\Scroll-it-Portable.exe (Version Portable sans installation)            " -ForegroundColor Yellow
Write-Host "==================================================" -ForegroundColor Green
