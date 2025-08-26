#!/bin/bash
# Generate version file for production endpoint versioning
# Format: Date.BuildNumber.ShortGitSha
# Usage: ./generate-version.sh [build_number]

# Get build number from parameter or use default
BUILD_NUMBER=${1:-${BUILD_NUMBER:-"dev"}}

# Get current date in UTC
DATE=$(date -u +"%Y.%m.%d")

# Get short git SHA (first 7 characters)
if git rev-parse --git-dir > /dev/null 2>&1; then
    SHORT_GIT_SHA=$(git rev-parse --short=7 HEAD)
else
    SHORT_GIT_SHA="unknown"
fi

# Generate version string
VERSION="${DATE}.${BUILD_NUMBER}.${SHORT_GIT_SHA}"

echo "Generating version: ${VERSION}"

# Create version.txt file in the project root and all API bin directories
echo "${VERSION}" > version.txt

# Also create in each API's bin directory for deployment
API_DIRS=(
    "InternationalCenter.Services.Public.Api"
    "InternationalCenter.Services.Admin.Api"
    "InternationalCenter.Contacts.Api"
    "InternationalCenter.Events.Api"
    "InternationalCenter.News.Api"
    "InternationalCenter.Newsletter.Api"
    "InternationalCenter.Research.Api"
    "InternationalCenter.Search.Api"
)

for dir in "${API_DIRS[@]}"; do
    if [ -d "$dir/bin" ]; then
        echo "${VERSION}" > "$dir/bin/version.txt"
        echo "Created version file in $dir/bin/"
    fi
    
    # Also create in the API root directory for development
    echo "${VERSION}" > "$dir/version.txt"
    echo "Created version file in $dir/"
done

echo "Version generation complete: ${VERSION}"