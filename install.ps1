# CodeMapper Installation Script for Windows
# Usage: irm https://raw.githubusercontent.com/DennisDyallo/CodeMapper/main/install.ps1 | iex
# Or download and run: .\install.ps1
# Set $env:VERSION to install a specific version (default: latest)
# Set $env:INSTALL_DIR to install to a custom directory (default: $HOME\.local\bin)

$ErrorActionPreference = "Stop"

$Repo = "DennisDyallo/CodeMapper"

Write-Host "Installing CodeMapper..."

# Detect architecture
$Arch = if ([Environment]::Is64BitOperatingSystem) {
    if ($env:PROCESSOR_ARCHITECTURE -eq "ARM64" -or $env:PROCESSOR_ARCHITEW6432 -eq "ARM64") {
        "arm64"
    } else {
        "x64"
    }
} else {
    Write-Error "Error: 32-bit Windows is not supported."
    exit 1
}

# Determine download URL based on VERSION
if ($env:VERSION) {
    $Version = $env:VERSION
    if (-not $Version.StartsWith("v")) {
        $Version = "v$Version"
    }
    $DownloadUrl = "https://github.com/$Repo/releases/download/$Version/codemapper-win-$Arch.zip"
    $ChecksumsUrl = "https://github.com/$Repo/releases/download/$Version/SHA256SUMS.txt"
} else {
    $DownloadUrl = "https://github.com/$Repo/releases/latest/download/codemapper-win-$Arch.zip"
    $ChecksumsUrl = "https://github.com/$Repo/releases/latest/download/SHA256SUMS.txt"
}

Write-Host "Platform: win-$Arch"
Write-Host "Downloading from: $DownloadUrl"

# Create temp directory
$TmpDir = Join-Path $env:TEMP "codemapper-install-$(Get-Random)"
New-Item -ItemType Directory -Path $TmpDir -Force | Out-Null
$TmpZip = Join-Path $TmpDir "codemapper-win-$Arch.zip"

try {
    # Download the zip file
    try {
        Invoke-WebRequest -Uri $DownloadUrl -OutFile $TmpZip -UseBasicParsing
    } catch {
        Write-Error "Error: Failed to download CodeMapper. Check if the release exists."
        exit 1
    }

    # Attempt to download checksums and validate
    $TmpChecksums = Join-Path $TmpDir "SHA256SUMS.txt"
    $ChecksumsAvailable = $false
    try {
        Invoke-WebRequest -Uri $ChecksumsUrl -OutFile $TmpChecksums -UseBasicParsing -ErrorAction SilentlyContinue
        $ChecksumsAvailable = $true
    } catch {
        # Checksums not available, continue without validation
    }

    if ($ChecksumsAvailable) {
        $ExpectedHash = (Get-Content $TmpChecksums | Where-Object { $_ -match "codemapper-win-$Arch\.zip" }) -replace '\s+.*$', ''
        if ($ExpectedHash) {
            $ActualHash = (Get-FileHash -Path $TmpZip -Algorithm SHA256).Hash.ToLower()
            if ($ActualHash -eq $ExpectedHash.ToLower()) {
                Write-Host "✓ Checksum validated"
            } else {
                Write-Error "Error: Checksum validation failed."
                exit 1
            }
        }
    }

    # Determine install directory
    if ($env:INSTALL_DIR) {
        $InstallDir = $env:INSTALL_DIR
    } else {
        $InstallDir = Join-Path $HOME ".local\bin"
    }

    # Create install directory if it doesn't exist
    if (-not (Test-Path $InstallDir)) {
        try {
            New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
        } catch {
            Write-Error "Error: Could not create directory $InstallDir. You may not have write permissions."
            exit 1
        }
    }

    # Check if binary already exists
    $BinaryPath = Join-Path $InstallDir "codemapper.exe"
    if (Test-Path $BinaryPath) {
        Write-Host "Notice: Replacing codemapper.exe found at $BinaryPath."
    }

    # Extract zip
    Expand-Archive -Path $TmpZip -DestinationPath $TmpDir -Force

    # Move binary to install directory
    $ExtractedBinary = Join-Path $TmpDir "codemapper.exe"
    if (-not (Test-Path $ExtractedBinary)) {
        Write-Error "Error: codemapper.exe not found in the downloaded archive."
        exit 1
    }
    Move-Item -Path $ExtractedBinary -Destination $BinaryPath -Force

    Write-Host "✓ CodeMapper installed to $BinaryPath"

    # Check if install directory is in PATH
    $UserPath = [Environment]::GetEnvironmentVariable("PATH", "User")
    if ($UserPath -notlike "*$InstallDir*") {
        Write-Host ""
        Write-Host "Warning: $InstallDir is not in your PATH"
        Write-Host "Add it to your PATH by running:"
        Write-Host "  `$env:PATH += `";$InstallDir`""
        Write-Host ""
        Write-Host "Or permanently add it with:"
        Write-Host "  [Environment]::SetEnvironmentVariable('PATH', `$env:PATH + ';$InstallDir', 'User')"
    }

    Write-Host ""
    Write-Host "Installation complete! Run 'codemapper --help' to get started."
    Write-Host ""
    Write-Host "Usage examples:"
    Write-Host "  codemapper C:\path\to\repo              # Scan repo, output text"
    Write-Host "  codemapper C:\path\to\repo --format json # Output as JSON"
    Write-Host "  codemapper C:\path\to\repo --output .\out # Custom output directory"

} finally {
    # Cleanup temp directory
    if (Test-Path $TmpDir) {
        Remove-Item -Path $TmpDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
