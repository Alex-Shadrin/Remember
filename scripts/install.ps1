param([switch]$NoLaunch)
$ErrorActionPreference = 'Stop'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs/Remember'
$sourceExe = Join-Path $PSScriptRoot 'Remember.exe'
if (-not (Test-Path -LiteralPath $sourceExe)) { throw 'Extract the release ZIP first, then run install.ps1 from the Remember folder.' }
$running = Get-Process -Name Remember -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq (Join-Path $installDir 'Remember.exe') }
if ($running) { throw 'Exit Remember from its tray menu before installing an update.' }
New-Item -ItemType Directory -Path $installDir -Force | Out-Null
foreach ($name in @('Remember.exe', 'Remember.exe.config', 'Remember.ico', 'uninstall.ps1', 'README.md', 'LICENSE')) {
    $source = Join-Path $PSScriptRoot $name
    if ((Test-Path -LiteralPath $source) -and ([IO.Path]::GetFullPath($source) -ne [IO.Path]::GetFullPath((Join-Path $installDir $name)))) { Copy-Item -LiteralPath $source -Destination $installDir -Force }
}
$target = Join-Path $installDir 'Remember.exe'
$iconPath = Join-Path $installDir 'Remember.ico'
if (-not (Test-Path -LiteralPath $iconPath)) { $iconPath = $target }
$shell = New-Object -ComObject WScript.Shell
foreach ($folder in @([Environment]::GetFolderPath('Desktop'), [Environment]::GetFolderPath('Programs'))) {
    $shortcut = $shell.CreateShortcut((Join-Path $folder 'Remember.lnk'))
    $shortcut.TargetPath = $target
    $shortcut.WorkingDirectory = $installDir
    $shortcut.IconLocation = "$iconPath,0"
    $shortcut.Save()
}
$runKey = 'HKCU:/Software/Microsoft/Windows/CurrentVersion/Run'
if (-not (Test-Path -Path $runKey)) { New-Item -Path $runKey -Force | Out-Null }
New-ItemProperty -Path $runKey -Name Remember -Value ('"' + $target + '" --background') -PropertyType String -Force | Out-Null
Write-Host "Remember installed: $target"
Write-Host 'Startup enabled. Your reminders are stored separately in %APPDATA%/Remember.'
if (-not $NoLaunch) { Start-Process -FilePath $target -WindowStyle Hidden }
