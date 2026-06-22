# Automated Build, Sign, and Installer Compile Script for Work Tracker v1.4.2
$ErrorActionPreference = "Stop"

$subject = "CN=Work Tracker Local Testing"
$signtool = "C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages\microsoft.windows.sdk.buildtools\10.0.26100.1742\bin\10.0.26100.0\x64\signtool.exe"
$iscc = "C:\Users\augus\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
$publishDir = "bin\Release\net9.0-windows\win-x64\publish"

# 1. Setup & Trust Self-Signed Certificate
Write-Host "=== Step 1: Checking Code Signing Certificate ==="
$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq $subject } | Select-Object -First 1
if ($cert -eq $null) {
    Write-Host "Creating new self-signed certificate for local code-signing..."
    $cert = New-SelfSignedCertificate -Type CodeSigning -Subject $subject -CertStoreLocation "Cert:\CurrentUser\My" -NotAfter (Get-Date).AddYears(5)
    Write-Host "Certificate successfully created in Personal Store."
} else {
    Write-Host "Using existing certificate with Thumbprint: $($cert.Thumbprint)"
}
$thumb = $cert.Thumbprint

# 2. Publish Project
Write-Host "`n=== Step 2: Cleaning and Publishing Self-Contained App Binaries ==="
# Remove cached build assets to force MSBuild to compile the new app_icon.ico resource
Remove-Item -Path "bin", "obj" -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=true

# 3. Sign Executable
Write-Host "`n=== Step 3: Digitally Signing WorkTracker.exe ==="
$exePath = Join-Path $publishDir "WorkTracker.exe"
& $signtool sign /sha1 $thumb /fd sha256 /tr http://timestamp.digicert.com /td sha256 $exePath
Write-Host "Successfully signed $exePath"

# 4. Compile Installer
Write-Host "`n=== Step 4: Compiling Setup Installer ==="
& $iscc setup.iss
Write-Host "Successfully compiled installer"

# 5. Sign Installer
Write-Host "`n=== Step 5: Digitally Signing WorkTrackerSetup-1.4.2.exe ==="
$setupPath = "..\Installers\WorkTrackerSetup-1.4.2.exe"
& $signtool sign /sha1 $thumb /fd sha256 /tr http://timestamp.digicert.com /td sha256 $setupPath
Write-Host "Successfully signed installer: $setupPath"

Write-Host "`n=== SUCCESS! Version 1.4.2 has been published, signed, and packaged! ==="
