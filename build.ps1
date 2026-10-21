$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$bundle = Join-Path $here "tor-bundle\x"
$assets = Join-Path $here "assets"
$out = Join-Path $here "build"
New-Item -ItemType Directory -Force $out | Out-Null
if (-not (Test-Path (Join-Path $bundle "tor\tor.exe"))) { throw "Tor nao encontrado em $bundle. Rode tools/fetch-tor.ps1 primeiro." }
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

function New-Zip($source, $zip) {
    if (Test-Path $zip) { Remove-Item $zip }
    $archive = [System.IO.Compression.ZipFile]::Open($zip, "Create")
    Get-ChildItem $source -Recurse -File | ForEach-Object {
        $rel = $_.FullName.Substring($source.Length + 1).Replace("\", "/")
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $_.FullName, $rel, "Optimal") | Out-Null
    }
    $archive.Dispose()
}

New-Zip $bundle "$out\tor.zip"
New-Zip "$assets\flags" "$out\flags.zip"

$fw = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
& "$fw\csc.exe" /nologo /codepage:65001 /target:winexe /platform:x64 /optimize+ `
    /out:"$here\Veil.exe" `
    /win32icon:"$assets\favicon.ico" `
    /lib:"$fw\WPF" `
    /resource:"$out\tor.zip",tor.zip `
    /resource:"$out\flags.zip",flags.zip `
    /resource:"$assets\countries.json",countries.json `
    /resource:"$assets\favicon.ico",favicon.ico `
    /resource:"$here\src\ui.xaml",ui.xaml `
    /resource:"$here\src\tray.xaml",tray.xaml `
    /r:System.dll /r:System.Core.dll /r:System.Xaml.dll /r:WindowsBase.dll /r:PresentationCore.dll /r:PresentationFramework.dll `
    /r:System.Drawing.dll /r:System.Windows.Forms.dll `
    /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:System.Web.Extensions.dll `
    "$here\src\Veil.cs"
if ($LASTEXITCODE -ne 0) { throw "Falha na compilacao" }

& "$fw\csc.exe" /nologo /codepage:65001 /target:winexe /platform:x64 /optimize+ `
    /out:"$here\Veil-Setup.exe" `
    /win32icon:"$assets\favicon.ico" `
    /lib:"$fw\WPF" `
    /resource:"$here\Veil.exe",Veil.exe `
    /resource:"$here\src\setup.xaml",setup.xaml `
    /resource:"$assets\logo.png",logo.png `
    /r:System.dll /r:System.Core.dll /r:System.Xaml.dll /r:WindowsBase.dll /r:PresentationCore.dll /r:PresentationFramework.dll `
    "$here\src\Setup.cs"
if ($LASTEXITCODE -ne 0) { throw "Falha na compilacao do instalador" }
Get-Item "$here\Veil.exe", "$here\Veil-Setup.exe" | Select-Object Name, Length
