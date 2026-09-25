$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$addinProject = Join-Path $root 'TestAddins.csproj'
$helperProject = Join-Path $root '..\DeploymentAdditional\DeploymentAdditional.csproj'
$addinOutput = Join-Path $root 'bin\Release\net10.0-windows'
$helperOutput = Join-Path $root '..\DeploymentAdditional\bin\Release\net10.0-windows'
$stage = Join-Path $root 'bin\tbAddin-staging'
$package = Join-Path $root 'bin\Toolbox.Addin.TestAddins.tbAddin'

if (Test-Path $stage) {
    Remove-Item $stage -Recurse -Force
}
New-Item -ItemType Directory -Path $stage | Out-Null
New-Item -ItemType Directory -Path (Join-Path $stage 'ToolboxServerAddinsFolder') | Out-Null
New-Item -ItemType Directory -Path (Join-Path $stage 'DeploymentAdditional') | Out-Null

& dotnet build $addinProject -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& dotnet build $helperProject -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Copy-Item (Join-Path $addinOutput '*') (Join-Path $stage 'ToolboxServerAddinsFolder') -Recurse -Force
Copy-Item (Join-Path $helperOutput '*') (Join-Path $stage 'DeploymentAdditional') -Recurse -Force
Copy-Item (Join-Path $root 'AddinPackage\manifest.txt') $stage -Force

if (Test-Path $package) {
    Remove-Item $package -Force
}
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $package -CompressionLevel Optimal
Write-Host "Created $package"
