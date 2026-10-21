param([switch]$Test, [switch]$UiTest, [switch]$Package)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$msbuildPath = $null
if (Test-Path -LiteralPath $vswhere) {
    $msbuildPath = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild/Current/Bin/MSBuild.exe' | Select-Object -First 1
}
if (-not $msbuildPath) { throw 'Install Visual Studio Build Tools with .NET desktop development and the .NET Framework 4.7.2 targeting pack.' }
& $msbuildPath (Join-Path $projectRoot 'Remember.csproj') /t:Build /p:Configuration=Release /nologo /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'Remember build failed.' }
if ($Test) {
    & $msbuildPath (Join-Path $projectRoot 'tests/Remember.Tests.csproj') /t:Build /p:Configuration=Release /nologo /verbosity:minimal
    if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
    & (Join-Path $projectRoot 'tests/bin/Release/Remember.Tests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
if ($UiTest) {
    & $msbuildPath (Join-Path $projectRoot 'tests/Remember.WpfTests.csproj') /t:Build /p:Configuration=Release /nologo /verbosity:minimal
    if ($LASTEXITCODE -ne 0) { throw 'WPF integration test build failed.' }
    & (Join-Path $projectRoot 'tests/bin/Wpf/Remember.WpfTests.exe') --offscreen
    if ($LASTEXITCODE -ne 0) { throw 'WPF integration tests failed.' }
    & $msbuildPath (Join-Path $projectRoot 'tests/Remember.WindowTests.csproj') /t:Build /p:Configuration=Release /nologo /verbosity:minimal
    if ($LASTEXITCODE -ne 0) { throw 'Window interaction test build failed.' }
    & (Join-Path $projectRoot 'tests/bin/Window/Remember.WindowTests.exe') --offscreen
    if ($LASTEXITCODE -ne 0) { throw 'Window interaction tests failed.' }
}
if ($Package) {
    $releaseDir = Join-Path $projectRoot 'dist/Remember'
    New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
    foreach ($name in @('Remember.exe', 'Remember.exe.config')) { Copy-Item -LiteralPath (Join-Path $projectRoot "bin/Release/$name") -Destination $releaseDir -Force }
    Copy-Item -LiteralPath (Join-Path $projectRoot 'assets/Remember.ico') -Destination $releaseDir -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'install.ps1') -Destination $releaseDir -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'uninstall.ps1') -Destination $releaseDir -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install.cmd') -Destination $releaseDir -Force
    $imageDir = Join-Path $releaseDir 'docs/images'
    New-Item -ItemType Directory -Path $imageDir -Force | Out-Null
    Copy-Item -Path (Join-Path $projectRoot 'docs/images/*.png') -Destination $imageDir -Force
    Copy-Item -LiteralPath (Join-Path $projectRoot 'docs/icon-design.md') -Destination (Join-Path $releaseDir 'docs') -Force
    foreach ($name in @('README.md', 'LICENSE')) { Copy-Item -LiteralPath (Join-Path $projectRoot $name) -Destination $releaseDir -Force }
    Compress-Archive -LiteralPath $releaseDir -DestinationPath (Join-Path $projectRoot 'dist/Remember-1.0.0-windows.zip') -Force
    $hash = (Get-FileHash -LiteralPath (Join-Path $projectRoot 'dist/Remember-1.0.0-windows.zip') -Algorithm SHA256).Hash.ToLowerInvariant()
    Set-Content -LiteralPath (Join-Path $projectRoot 'dist/SHA256SUMS.txt') -Value ($hash + ' *Remember-1.0.0-windows.zip') -Encoding Ascii
    Get-ChildItem -LiteralPath $releaseDir | Select-Object Name, Length
}
