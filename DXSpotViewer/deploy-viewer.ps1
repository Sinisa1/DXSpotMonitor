# =====================================================================
# DXSpotViewer Android Deployment Script
# =====================================================================

Write-Host "Starting DXSpotViewer deployment..." -ForegroundColor Cyan
Write-Host ""

# Navigate to project
Set-Location C:\_REPOSITORIES\VA3TNM_LOG\DXSpotViewer

# 1. Delete build folders
Write-Host "[1/7] Cleaning build folders..." -ForegroundColor Yellow
Remove-Item -Recurse -Force obj,bin -ErrorAction SilentlyContinue

# 2. Clean with dotnet
Write-Host "[2/7] Running dotnet clean..." -ForegroundColor Yellow
dotnet clean -f net9.0-android | Out-Null

# 3. Restore packages
Write-Host "[3/7] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore | Out-Null

# 4. Check if phone is connected
Write-Host "[4/7] Checking for connected Android devices..." -ForegroundColor Yellow
try {
    $devices = adb devices 2>&1 | Out-String
    Write-Host $devices
    
    if ($devices -match "device$") {
        Write-Host "SUCCESS: Android device detected!" -ForegroundColor Green
    } else {
        Write-Host "WARNING: No Android device detected!" -ForegroundColor Red
    }
} catch {
    Write-Host "INFO: Could not check devices" -ForegroundColor Gray
}

Write-Host ""

# 5. Verify audio file exists
Write-Host "[5/7] Checking audio file..." -ForegroundColor Yellow
$audioFile = "Resources\Raw\NewSpotArrived.mp3"
if (Test-Path $audioFile) {
    $size = (Get-Item $audioFile).Length
    Write-Host "SUCCESS: Audio file found ($size bytes)" -ForegroundColor Green
} else {
    Write-Host "WARNING: Audio file not found" -ForegroundColor Red
}

Write-Host ""

# 6. Build
Write-Host "[6/7] Building for Android..." -ForegroundColor Yellow
dotnet build -f net9.0-android

if ($LASTEXITCODE -eq 0) {
    Write-Host "SUCCESS: Build completed!" -ForegroundColor Green
} else {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""

# 7. Deploy to device
Write-Host "[7/7] Deploying to Android device..." -ForegroundColor Yellow
dotnet build -f net9.0-android -t:Run

Write-Host ""
Write-Host "=====================================================================" -ForegroundColor Green
Write-Host " Deployment complete!" -ForegroundColor Green
Write-Host "=====================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "App should now be running on your phone." -ForegroundColor Cyan
Write-Host ""