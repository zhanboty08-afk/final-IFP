$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$vswhere = Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path -LiteralPath $vswhere)) {
    throw 'Visual Studio Installer vswhere.exe was not found. Install Visual Studio 2022 with the Desktop development with C++ workload.'
}

$vsInstall = (& $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath | Select-Object -First 1)
if ([string]::IsNullOrWhiteSpace($vsInstall)) {
    throw 'The Visual C++ x64 build tools were not found. Install the Desktop development with C++ workload.'
}

$devCmd = Join-Path $vsInstall 'Common7\Tools\VsDevCmd.bat'
$env:PATH = "$(Split-Path -Parent $vswhere);$env:PATH"
$source = Join-Path $projectRoot 'BudgetPipeline\native\budget_native.c'
$outputDirectory = Join-Path $projectRoot 'BudgetPipeline\runtimes\win-x64\native'
$buildDirectory = Join-Path $projectRoot '.native-build'
$outputDll = Join-Path $outputDirectory 'budget_native.dll'
$outputLib = Join-Path $buildDirectory 'budget_native.lib'
$outputObject = Join-Path $buildDirectory 'budget_native.obj'

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $buildDirectory -Force | Out-Null

$command = 'call "{0}" -no_logo -arch=x64 -host_arch=x64 && cl /nologo /W4 /O2 /MT /LD /TC "{1}" /Fo"{2}" /link /OUT:"{3}" /IMPLIB:"{4}"' -f $devCmd, $source, $outputObject, $outputDll, $outputLib
& $env:ComSpec /d /c $command
if ($LASTEXITCODE -ne 0) {
    throw "Native C compilation failed with exit code $LASTEXITCODE."
}

Write-Output "Built native library: $outputDll"
