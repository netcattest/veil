$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$version = "15.0.26"
$sha256 = "C1988C701DE2F24F7D098D1D6D7F507B58F27740D097F29C246E0524D639B252"
$name = "tor-expert-bundle-windows-x86_64-$version.tar.gz"
$url = "https://dist.torproject.org/torbrowser/$version/$name"
$dir = Join-Path $root "tor-bundle"
$archive = Join-Path $dir $name

New-Item -ItemType Directory -Force $dir | Out-Null
if (-not (Test-Path $archive)) {
    Write-Host "Baixando $url"
    curl.exe -fL --retry 3 -o $archive $url
    curl.exe -fL --retry 3 -o "$archive.asc" "$url.asc"
}
$hash = (Get-FileHash $archive -Algorithm SHA256).Hash
if ($hash -ne $sha256) { throw "SHA-256 inesperado: $hash" }

$x = Join-Path $dir "x"
if (Test-Path $x) { Remove-Item $x -Recurse -Force }
New-Item -ItemType Directory $x | Out-Null
& "$env:WINDIR\System32\tar.exe" -xzf $archive -C $x
if ($LASTEXITCODE -ne 0 -or -not (Test-Path (Join-Path $x "tor\tor.exe"))) { throw "Falha ao extrair o Tor Expert Bundle" }
Write-Host "Tor Expert Bundle $version pronto em $x"
