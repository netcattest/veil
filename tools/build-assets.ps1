$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$assets = Join-Path $root "assets"
$edge = "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
$profile = Join-Path $env:TEMP "veil-edge-render"
$svg = "file:///" + ($assets -replace "\\", "/") + "/logo.svg"
& $edge --headless=new --disable-gpu --hide-scrollbars --user-data-dir="$profile" --default-background-color=00000000 `
    --window-size=256,256 --screenshot="$assets\logo.png" $svg 2>$null | Out-Null
Start-Sleep 2
& (Join-Path $PSScriptRoot "make-icon.ps1") -Png "$assets\logo.png" -Ico "$assets\favicon.ico"
Get-Item "$assets\logo.png", "$assets\favicon.ico" | Select-Object Name, Length
