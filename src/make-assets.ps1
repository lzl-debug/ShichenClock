$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$clockRoot = Split-Path -Parent $PSScriptRoot
$sheet = New-Object System.Drawing.Bitmap 1440,1360
$canvas = [System.Drawing.Graphics]::FromImage($sheet)
$canvas.Clear([System.Drawing.ColorTranslator]::FromHtml('#E5E9EE'))
$canvas.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
for ($i=0; $i -lt 14; $i++) {
    $file = Join-Path $clockRoot ('previews\{0:00}.png' -f ($i+1))
    $img = [System.Drawing.Image]::FromFile($file)
    $canvas.DrawImage($img, [int](($i % 4)*360), [int]([Math]::Floor($i/4)*340), 360, 340)
    $img.Dispose()
}
$sheet.Save((Join-Path $clockRoot 'previews\all-styles.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$canvas.Dispose()
$sheet.Dispose()
$pair = New-Object System.Drawing.Bitmap 1440,680
$pairCanvas = [System.Drawing.Graphics]::FromImage($pair)
$pairCanvas.Clear([System.Drawing.ColorTranslator]::FromHtml('#E5E9EE'))
for ($i=0; $i -lt 2; $i++) {
    $img = [System.Drawing.Image]::FromFile((Join-Path $clockRoot ('previews\{0:00}.png' -f ($i+13))))
    $pairCanvas.DrawImage($img, [int]($i*720), 0,720,680)
    $img.Dispose()
}
$pair.Save((Join-Path $clockRoot 'previews\new-dials.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$pairCanvas.Dispose()
$pair.Dispose()
$icon = New-Object System.Drawing.Bitmap 256,256
$g = [System.Drawing.Graphics]::FromImage($icon)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)
$bg = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml('#173D7B'))
$fg = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml('#F4F7F7'))
$pen = New-Object System.Drawing.Pen $fg,5
$g.FillEllipse($bg,8,8,240,240)
$g.DrawEllipse($pen,24,24,208,208)
$font = New-Object System.Drawing.Font 'KaiTi',125,([System.Drawing.FontStyle]::Regular),([System.Drawing.GraphicsUnit]::Pixel)
$format = New-Object System.Drawing.StringFormat
$format.Alignment = [System.Drawing.StringAlignment]::Center
$format.LineAlignment = [System.Drawing.StringAlignment]::Center
$g.DrawString(([string][char]0x8FB0),$font,$fg,(New-Object System.Drawing.RectangleF 0,8,256,240),$format)
$memory = New-Object System.IO.MemoryStream
$icon.Save($memory,[System.Drawing.Imaging.ImageFormat]::Png)
$bytes=$memory.ToArray()
$stream=[System.IO.File]::Create((Join-Path $PSScriptRoot 'clock.ico'))
$writer=New-Object System.IO.BinaryWriter $stream
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]1)
$writer.Write([byte]0); $writer.Write([byte]0); $writer.Write([byte]0); $writer.Write([byte]0)
$writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$bytes.Length); $writer.Write([uint32]22); $writer.Write($bytes)
$writer.Dispose(); $memory.Dispose(); $format.Dispose(); $font.Dispose(); $pen.Dispose(); $bg.Dispose(); $fg.Dispose(); $g.Dispose(); $icon.Dispose()
Write-Output 'Contact sheet and app icon generated.'
