#!/usr/bin/env pwsh

# Clean script to remove all TestResults folders recursively
Write-Host "Cleaning TestResults folders..." -ForegroundColor Yellow

# Get all TestResults directories recursively
$testResultsFolders = Get-ChildItem -Path . -Directory -Recurse -Filter "TestResults" -ErrorAction SilentlyContinue

if ($testResultsFolders.Count -eq 0) {
    Write-Host "No TestResults folders found." -ForegroundColor Green
}
else {
    Write-Host "Found $($testResultsFolders.Count) TestResults folder(s) to delete:" -ForegroundColor Cyan

    foreach ($folder in $testResultsFolders) {
        Write-Host "  - $($folder.FullName)" -ForegroundColor Gray
        Remove-Item -Path $folder.FullName -Recurse -Force
    }

    Write-Host "All TestResults folders have been deleted." -ForegroundColor Green
}