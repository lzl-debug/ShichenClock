$ErrorActionPreference = 'Stop'
$clockRoot = $PSScriptRoot
$frameworkRoot = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $frameworkRoot 'csc.exe'
if (!(Test-Path -LiteralPath $compiler)) {
    $frameworkRoot = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
    $compiler = Join-Path $frameworkRoot 'csc.exe'
}
if (!(Test-Path -LiteralPath $compiler)) { throw 'Please enable .NET Framework 4.x on Windows.' }
$references = @('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll','System.Xaml.dll')
$compileArgs = @('/nologo','/target:winexe','/optimize+','/platform:anycpu',('/out:' + (Join-Path $clockRoot 'ShichenClock.exe')),('/win32manifest:' + (Join-Path $clockRoot 'src\app.manifest')))
foreach ($reference in $references) { $compileArgs += '/reference:' + (Join-Path $frameworkRoot $reference) }
if (Test-Path -LiteralPath (Join-Path $clockRoot 'src\clock.ico')) { $compileArgs += '/win32icon:' + (Join-Path $clockRoot 'src\clock.ico') }
$compileArgs += (Get-ChildItem -LiteralPath (Join-Path $clockRoot 'src') -Filter '*.cs').FullName
& $compiler $compileArgs
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Write-Output (Join-Path $clockRoot 'ShichenClock.exe')
