param([string]$Version = '1.2.0')
$ErrorActionPreference = 'Stop'
$clockRoot = $PSScriptRoot
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Version must be major.minor.patch.' }
$buildOutput = Join-Path $clockRoot 'artifacts\build'
& (Join-Path $clockRoot 'build.ps1') -OutDir $buildOutput
$packageName = 'ShichenClock-v' + $Version + '-windows'
$stageParent = Join-Path $clockRoot ('artifacts\' + $packageName)
$stage = Join-Path $stageParent 'ShichenClock'
if (Test-Path -LiteralPath $stageParent) { throw ('Package staging directory already exists: ' + $stageParent) }
New-Item -ItemType Directory -Path $stage -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $buildOutput 'ShichenClock.exe') -Destination $stage
# Explicit allowlist: personal settings, test output, source backups and user data stay local.
$includeFiles = @('README.md', '使用说明.md', '快速开始.txt', '风格预览.html', '动态表盘预览.html', '易象预览.html', '卦象设计依据.md')
foreach ($name in $includeFiles) { Copy-Item -LiteralPath (Join-Path $clockRoot $name) -Destination $stage }
Copy-Item -LiteralPath (Join-Path $clockRoot 'previews') -Destination $stage -Recurse
$archive = Join-Path $clockRoot ('artifacts\' + $packageName + '.zip')
Compress-Archive -LiteralPath $stage -DestinationPath $archive -CompressionLevel Optimal
$digest = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText(($archive + '.sha256'), ($digest + '  ' + [IO.Path]::GetFileName($archive) + "`n"), [Text.Encoding]::ASCII)
Write-Output $archive
