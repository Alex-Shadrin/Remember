$ErrorActionPreference = 'Stop'
$installDir = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'Programs/Remember'))
$expectedExe = Join-Path $installDir 'Remember.exe'
if (Get-Process -Name Remember -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $expectedExe }) { throw 'Exit Remember from its tray menu before uninstalling.' }
Remove-ItemProperty -Path 'HKCU:/Software/Microsoft/Windows/CurrentVersion/Run' -Name Remember -ErrorAction SilentlyContinue
foreach ($folder in @([Environment]::GetFolderPath('Desktop'), [Environment]::GetFolderPath('Programs'))) {
    $shortcutPath = Join-Path $folder 'Remember.lnk'
    if (Test-Path -LiteralPath $shortcutPath) { Remove-Item -LiteralPath $shortcutPath }
}
# Deliberately remove only known app files; user reminders and backups stay in AppData.
foreach ($name in @('Remember.exe', 'Remember.exe.config', 'uninstall.ps1', 'README.md', 'LICENSE')) {
    $file = Join-Path $installDir $name
    if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file -Force }
}
if ((Test-Path -LiteralPath $installDir) -and @(Get-ChildItem -LiteralPath $installDir -Force).Count -eq 0) { Remove-Item -LiteralPath $installDir }
Write-Host 'Remember uninstalled. Reminders and backups remain in %APPDATA%/Remember.'
