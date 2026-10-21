$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$projectRoot = Split-Path $PSScriptRoot -Parent
$assetDir = Join-Path $projectRoot 'assets'
$previewDir = Join-Path $projectRoot 'docs/images'
New-Item -ItemType Directory -Path $previewDir -Force | Out-Null
# Artwork is generated once; packaging only resizes it and preserves its alpha.
$source = [Drawing.Image]::FromFile((Join-Path $assetDir 'Remember.png'))
$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$frames = @()
try {
    if ($source.Width -ne $source.Height) { throw 'Remember.png must be square.' }
    foreach ($size in $sizes) {
        $bitmap = [Drawing.Bitmap]::new($size, $size, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [Drawing.Graphics]::FromImage($bitmap)
        $memory = New-Object IO.MemoryStream
        try {
            $graphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
            $graphics.CompositingQuality = [Drawing.Drawing2D.CompositingQuality]::HighQuality
            $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.Clear([Drawing.Color]::Transparent)
            $graphics.DrawImage($source, [Drawing.Rectangle]::new(0, 0, $size, $size))
            $bitmap.Save($memory, [Drawing.Imaging.ImageFormat]::Png)
            $frames += [pscustomobject]@{ Size = $size; Bytes = $memory.ToArray() }
            if ($size -eq 256) { $bitmap.Save((Join-Path $previewDir 'app-icon.png'), [Drawing.Imaging.ImageFormat]::Png) }
            if ($size -eq 16) { $bitmap.Save((Join-Path $previewDir 'tray-icon.png'), [Drawing.Imaging.ImageFormat]::Png) }
        } finally { $memory.Dispose(); $graphics.Dispose(); $bitmap.Dispose() }
    }
} finally { $source.Dispose() }
# Windows ICO directory with PNG-encoded 32-bit frames, including high-DPI sizes.
$stream = [IO.File]::Create((Join-Path $assetDir 'Remember.ico'))
$writer = New-Object IO.BinaryWriter $stream
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
    $offset = 6 + 16 * $frames.Count
    foreach ($frame in $frames) {
        $dimension = if ($frame.Size -eq 256) { 0 } else { $frame.Size }
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
        $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$frame.Bytes.Length); $writer.Write([uint32]$offset)
        $offset += $frame.Bytes.Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]]$frame.Bytes) }
} finally { $writer.Dispose() }
Write-Host ('Remember.ico: ' + ($sizes -join ', ') + ' px')
