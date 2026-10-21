$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$assetDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'assets'
New-Item -ItemType Directory -Path $assetDir -Force | Out-Null
$bitmap = New-Object Drawing.Bitmap 64,64
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([Drawing.Color]::Transparent)
$bg = New-Object Drawing.SolidBrush ([Drawing.ColorTranslator]::FromHtml('#24354B'))
$accent = New-Object Drawing.Pen ([Drawing.ColorTranslator]::FromHtml('#79AFFF')),4
$path = New-Object Drawing.Drawing2D.GraphicsPath
$path.AddArc(2,2,24,24,180,90); $path.AddArc(38,2,24,24,270,90); $path.AddArc(38,38,24,24,0,90); $path.AddArc(2,38,24,24,90,90); $path.CloseFigure()
$graphics.FillPath($bg,$path)
$graphics.DrawRectangle($accent,16,19,32,31)
$graphics.DrawLine($accent,16,29,48,29)
$graphics.DrawLine($accent,24,14,24,23); $graphics.DrawLine($accent,40,14,40,23)
$dot = New-Object Drawing.SolidBrush ([Drawing.ColorTranslator]::FromHtml('#79AFFF'))
$graphics.FillEllipse($dot,27,35,10,10)
$iconHandle = $bitmap.GetHicon()
$icon = [Drawing.Icon]::FromHandle($iconHandle)
$stream = [IO.File]::Create((Join-Path $assetDir 'Remember.ico'))
$icon.Save($stream); $stream.Dispose()
$bitmap.Save((Join-Path $assetDir 'Remember.png'), [Drawing.Imaging.ImageFormat]::Png)
$icon.Dispose(); $dot.Dispose(); $accent.Dispose(); $bg.Dispose(); $path.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public static class RememberIconCleanup { [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr handle); }'
[RememberIconCleanup]::DestroyIcon($iconHandle) | Out-Null
