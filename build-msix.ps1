param([string]$ProjectRoot = "D:\DELL\PDF工具")

$ErrorActionPreference = "Stop"
$makeappx = "D:\Windows Kits\10\bin\10.0.26100.0\x64\makeappx.exe"

# Step 1: Publish
Write-Host "=== Step 1: dotnet publish ===" -ForegroundColor Cyan
$publishDir = Join-Path $ProjectRoot "publish\msix"
Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish (Join-Path $ProjectRoot "PdfToolbox\PdfToolbox.csproj") -c Release -r win-x64 --self-contained true -o $publishDir -p:IncludeNativeLibrariesForSelfExtract=false -p:PublishReadyToRun=true  2>&1 | Out-Host
if ($LASTEXITCODE -ne 0) { throw "publish failed" }

# Step 2: Stage - prepare the directory structure
Write-Host "=== Step 2: Stage MSIX content ===" -ForegroundColor Cyan
$stageDir = Join-Path $ProjectRoot "PdfToolbox.Package\Stage"
Remove-Item $stageDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $stageDir -Force | Out-Null

# Copy all published files
Get-ChildItem $publishDir -File | Copy-Item -Destination $stageDir

# Copy Assets
$as = Join-Path $ProjectRoot "PdfToolbox.Package\Assets"
if (Test-Path $as) {
    $ad = Join-Path $stageDir "Assets"
    New-Item -ItemType Directory -Path $ad -Force | Out-Null
    Copy-Item (Join-Path $as "*") -Destination $ad
}

# Copy AppxManifest.xml as the manifest
Copy-Item (Join-Path $ProjectRoot "PdfToolbox.Package\Package.appxmanifest") (Join-Path $stageDir "AppxManifest.xml")

Write-Host "Files staged: $( (Get-ChildItem $stageDir -Recurse -File | Measure-Object).Count ) files" -ForegroundColor Green

# Step 3: Create MSIX with makeappx.exe
Write-Host "=== Step 3: Creating MSIX with makeappx.exe ===" -ForegroundColor Cyan
$msixName = "PdfToolbox-" + (Get-Date -Format "yyyyMMdd") + ".msix"
$msixPath = Join-Path $ProjectRoot $msixName
Remove-Item $msixPath -ErrorAction SilentlyContinue

& $makeappx pack /v /h SHA256 /d $stageDir /p $msixPath /o 2>&1
if ($LASTEXITCODE -ne 0) { throw "makeappx failed" }

# Step 4: Validate the package
Write-Host "=== Step 4: Validate MSIX ===" -ForegroundColor Cyan
$signtool = "D:\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe"
if (Test-Path $signtool) {
    & $signtool verify /pa $msixPath 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Package signature verified" -ForegroundColor Green
    } else {
        Write-Host "  Package is unsigned (normal for Store submission)" -ForegroundColor Yellow
    }
}

# Cleanup
Remove-Item $stageDir -Recurse -Force -ErrorAction SilentlyContinue

$mb = [math]::Round((Get-Item $msixPath).Length / 1MB, 1)
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SUCCESS! MSIX package created:" -ForegroundColor Green
Write-Host "  $msixPath" -ForegroundColor White
Write-Host "  Size: $mb MB" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Upload this .msix file to Partner Center." -ForegroundColor Yellow
