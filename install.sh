#!/usr/bin/env bash
set -e

# CodeMapper Installation Script
# Usage: curl -fsSL https://raw.githubusercontent.com/OWNER/CodeMapper/main/install.sh | bash
#    or: wget -qO- https://raw.githubusercontent.com/OWNER/CodeMapper/main/install.sh | bash
# Use | sudo bash to run as root and install to /usr/local/bin
# Export PREFIX to install to $PREFIX/bin/ directory (default: /usr/local for
# root, $HOME/.local for non-root)
# Export VERSION to install a specific version (default: latest)

REPO="DennisDyallo/CodeMapper"

echo "Installing CodeMapper..."

# Detect platform
case "$(uname -s || echo "")" in
  Darwin*) PLATFORM="osx" ;;
  Linux*) PLATFORM="linux" ;;
  *)
    echo "Error: Unsupported platform $(uname -s). CodeMapper supports Linux and macOS." >&2
    exit 1
    ;;
esac

# Detect architecture
case "$(uname -m)" in
  x86_64|amd64) ARCH="x64" ;;
  aarch64|arm64) ARCH="arm64" ;;
  *) echo "Error: Unsupported architecture $(uname -m)" >&2 ; exit 1 ;;
esac

# Determine download URL based on VERSION
if [ -n "$VERSION" ]; then
  # Prefix version with 'v' if not already present
  case "$VERSION" in
    v*) ;;
    *) VERSION="v$VERSION" ;;
  esac
  DOWNLOAD_URL="https://github.com/${REPO}/releases/download/${VERSION}/codemapper-${PLATFORM}-${ARCH}.tar.gz"
  CHECKSUMS_URL="https://github.com/${REPO}/releases/download/${VERSION}/SHA256SUMS.txt"
else
  DOWNLOAD_URL="https://github.com/${REPO}/releases/latest/download/codemapper-${PLATFORM}-${ARCH}.tar.gz"
  CHECKSUMS_URL="https://github.com/${REPO}/releases/latest/download/SHA256SUMS.txt"
fi
echo "Platform: ${PLATFORM}-${ARCH}"
echo "Downloading from: $DOWNLOAD_URL"

# Download and extract with error handling
TMP_DIR="$(mktemp -d)"
TMP_TARBALL="$TMP_DIR/codemapper-${PLATFORM}-${ARCH}.tar.gz"
if command -v curl >/dev/null 2>&1; then
  if ! curl -fsSL "$DOWNLOAD_URL" -o "$TMP_TARBALL"; then
    echo "Error: Failed to download CodeMapper. Check if the release exists." >&2
    rm -rf "$TMP_DIR"
    exit 1
  fi
elif command -v wget >/dev/null 2>&1; then
  if ! wget -qO "$TMP_TARBALL" "$DOWNLOAD_URL"; then
    echo "Error: Failed to download CodeMapper. Check if the release exists." >&2
    rm -rf "$TMP_DIR"
    exit 1
  fi
else
  echo "Error: Neither curl nor wget found. Please install one of them." >&2
  rm -rf "$TMP_DIR"
  exit 1
fi

# Attempt to download checksums file and validate
TMP_CHECKSUMS="$TMP_DIR/SHA256SUMS.txt"
CHECKSUMS_AVAILABLE=false
if command -v curl >/dev/null 2>&1; then
  curl -fsSL "$CHECKSUMS_URL" -o "$TMP_CHECKSUMS" 2>/dev/null && CHECKSUMS_AVAILABLE=true
elif command -v wget >/dev/null 2>&1; then
  wget -qO "$TMP_CHECKSUMS" "$CHECKSUMS_URL" 2>/dev/null && CHECKSUMS_AVAILABLE=true
fi

if [ "$CHECKSUMS_AVAILABLE" = true ]; then
  if command -v sha256sum >/dev/null 2>&1; then
    if (cd "$TMP_DIR" && sha256sum -c --ignore-missing SHA256SUMS.txt >/dev/null 2>&1); then
      echo "✓ Checksum validated"
    else
      echo "Error: Checksum validation failed." >&2
      rm -rf "$TMP_DIR"
      exit 1
    fi
  elif command -v shasum >/dev/null 2>&1; then
    if (cd "$TMP_DIR" && shasum -a 256 -c --ignore-missing SHA256SUMS.txt >/dev/null 2>&1); then
      echo "✓ Checksum validated"
    else
      echo "Error: Checksum validation failed." >&2
      rm -rf "$TMP_DIR"
      exit 1
    fi
  else
    echo "Warning: No sha256sum or shasum found, skipping checksum validation."
  fi
fi

# Check that the file is a valid tarball
if ! tar -tzf "$TMP_TARBALL" >/dev/null 2>&1; then
  echo "Error: Downloaded file is not a valid tarball or is corrupted." >&2
  rm -rf "$TMP_DIR"
  exit 1
fi

# Check if running as root, fallback to non-root
if [ "$(id -u 2>/dev/null || echo 1)" -eq 0 ]; then
  PREFIX="${PREFIX:-/usr/local}"
else
  PREFIX="${PREFIX:-$HOME/.local}"
fi
INSTALL_DIR="$PREFIX/bin"
if ! mkdir -p "$INSTALL_DIR"; then
  echo "Error: Could not create directory $INSTALL_DIR. You may not have write permissions." >&2
  echo "Try running this script with sudo or set PREFIX to a directory you own (e.g., export PREFIX=\$HOME/.local)." >&2
  rm -rf "$TMP_DIR"
  exit 1
fi

# Install binary
if [ -f "$INSTALL_DIR/codemapper" ]; then
  echo "Notice: Replacing codemapper binary found at $INSTALL_DIR/codemapper."
fi
tar -xz -C "$INSTALL_DIR" -f "$TMP_TARBALL"
chmod +x "$INSTALL_DIR/codemapper"
echo "✓ CodeMapper installed to $INSTALL_DIR/codemapper"
rm -rf "$TMP_DIR"

# Check if install directory is in PATH
case ":$PATH:" in
  *":$INSTALL_DIR:"*) ;;
  *)
    echo ""
    echo "Warning: $INSTALL_DIR is not in your PATH"
    echo "Add it to your PATH by adding this line to your shell profile:"
    echo "  export PATH=\"\$PATH:$INSTALL_DIR\""
    ;;
esac

echo ""
echo "Installation complete! Run 'codemapper --help' to get started."
echo ""
echo "Usage examples:"
echo "  codemapper /path/to/repo              # Scan repo, output text"
echo "  codemapper /path/to/repo --format json # Output as JSON"
echo "  codemapper /path/to/repo --output ./out # Custom output directory"
