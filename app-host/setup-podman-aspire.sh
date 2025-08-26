#!/bin/bash

# Setup script for Podman + Aspire compatibility for International Center Services APIs
# This script configures Podman for optimal use with Microsoft Aspire orchestration
# Run this script once to set up the development environment

set -euo pipefail

# Colors for output
readonly RED='\033[0;31m'
readonly GREEN='\033[0;32m'
readonly YELLOW='\033[1;33m'
readonly BLUE='\033[0;34m'
readonly NC='\033[0m' # No Color

# Configuration directories
readonly CONFIG_DIR="$HOME/.config/containers"
readonly DATA_DIR="$HOME/.local/share/containers"
readonly PROJECT_ROOT="$(dirname "$(realpath "$0")")"

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

check_podman_installed() {
    if ! command -v podman >/dev/null 2>&1; then
        log_error "Podman is not installed. Please install Podman first."
        exit 1
    fi
    
    local podman_version=$(podman version --format "{{.Client.Version}}")
    log_info "Found Podman version: $podman_version"
}

setup_podman_configuration() {
    log_info "Setting up Podman configuration for Services APIs..."
    
    # Create configuration directories
    mkdir -p "$CONFIG_DIR"
    mkdir -p "$DATA_DIR/storage/volumes"
    
    # Copy optimized containers.conf
    if [[ -f "$PROJECT_ROOT/containers.conf" ]]; then
        cp "$PROJECT_ROOT/containers.conf" "$CONFIG_DIR/containers.conf"
        log_success "Copied optimized containers.conf"
    else
        log_warning "containers.conf not found in project root"
    fi
    
    # Create storage.conf optimized for Services APIs
    cat > "$CONFIG_DIR/storage.conf" << 'EOF'
[storage]
driver = "overlay"
runroot = "/run/user/1000/containers"
graphroot = "/home/tojkuv/.local/share/containers/storage"

[storage.options.overlay]
# Performance optimizations for Services APIs containers
mountopt = "nodev,metacopy=on"
size = "50G"

[storage.options.thinpool]
# Enhanced for frequent container builds during development
autoextend_percent = "20"
autoextend_threshold = "80"
basesize = "10G"
blocksize = "64k"
directlvm_device = ""
directlvm_device_force = "True"
fs = "xfs"
log_level = "7"
min_free_space = "10%"
mkfsarg = ""
mountopt = ""
use_deferred_deletion = "True"
use_deferred_removal = "True"
xfs_nospace_max_retries = "0"
EOF
    
    log_success "Created optimized storage.conf for Services APIs"
}

setup_aspire_network() {
    log_info "Setting up Aspire-compatible network for Services APIs..."
    
    # Create dedicated network for Services APIs
    if ! podman network exists international-center-network 2>/dev/null; then
        podman network create \
            --driver bridge \
            --subnet 10.89.0.0/24 \
            --gateway 10.89.0.1 \
            --opt com.docker.network.bridge.name=ic-bridge \
            international-center-network
        log_success "Created international-center-network"
    else
        log_info "international-center-network already exists"
    fi
}

setup_development_volumes() {
    log_info "Setting up persistent volumes for Services APIs..."
    
    # Create data directories for Services APIs persistence
    local data_dirs=(
        "$PROJECT_ROOT/data/postgres"
        "$PROJECT_ROOT/data/redis"
        "$PROJECT_ROOT/secrets"
    )
    
    for dir in "${data_dirs[@]}"; do
        mkdir -p "$dir"
        chmod 755 "$dir"
    done
    
    # Create default postgres password for development
    if [[ ! -f "$PROJECT_ROOT/secrets/postgres_password.txt" ]]; then
        echo "development_password_change_in_production" > "$PROJECT_ROOT/secrets/postgres_password.txt"
        chmod 600 "$PROJECT_ROOT/secrets/postgres_password.txt"
        log_success "Created default postgres password (development only)"
    fi
    
    log_success "Set up data directories for Services APIs"
}

configure_aspire_compatibility() {
    log_info "Configuring Aspire compatibility settings..."
    
    # Set environment variables for Aspire + Podman compatibility
    local env_file="$PROJECT_ROOT/.env"
    cat > "$env_file" << 'EOF'
# Aspire + Podman compatibility for Services APIs
CONTAINER_RUNTIME=podman
ASPIRE_CONTAINER_RUNTIME=podman
DOCKER_HOST=unix:///run/user/1000/podman/podman.sock

# Services APIs specific configuration
ASPIRE_ALLOW_UNSECURED_TRANSPORT=true
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317

# Medical-grade compliance settings
MEDICAL_GRADE_AUDIT=true
ZERO_TRUST_SECURITY=true
AUDIT_LOG_LEVEL=Information

# Performance tuning for Services APIs
ASPNETCORE_THREADPOOL_MAXWORKERTHREADS=100
ASPNETCORE_THREADPOOL_MAXCOMPLETIONPORTTHREADS=100
EOF
    
    log_success "Created .env file with Aspire compatibility settings"
}

verify_setup() {
    log_info "Verifying Podman + Aspire setup for Services APIs..."
    
    # Test podman functionality
    if podman info >/dev/null 2>&1; then
        log_success "Podman is working correctly"
    else
        log_error "Podman configuration issue detected"
        return 1
    fi
    
    # Test network creation
    if podman network exists international-center-network 2>/dev/null; then
        log_success "Services APIs network is ready"
    else
        log_error "Network setup failed"
        return 1
    fi
    
    # Test volume access
    if [[ -d "$PROJECT_ROOT/data" ]]; then
        log_success "Data directories are accessible"
    else
        log_error "Data directory setup failed"
        return 1
    fi
    
    log_success "Podman + Aspire setup verification completed!"
}

print_usage_instructions() {
    cat << 'EOF'

🚀 Podman + Aspire Setup Complete for Services APIs!

Next steps:
1. Start Aspire orchestration:
   cd /path/to/InternationalCenter.AppHost
   dotnet run

2. Or use direct Podman Compose:
   podman-compose -f podman-compose.yml up -d

3. Verify Services APIs are running:
   curl http://localhost:8081/health  # Services Public API
   curl http://localhost:8088/health  # Services Admin API

4. View logs:
   podman logs international-center-services-public-api
   podman logs international-center-services-admin-api

5. Access PostgreSQL (development):
   podman exec -it international-center-postgres psql -U postgres -d database

Configuration files created:
- ~/.config/containers/containers.conf (Podman optimizations)
- ~/.config/containers/storage.conf (Storage optimizations)
- ./podman-compose.yml (Direct Podman orchestration)
- ./.env (Aspire compatibility settings)

For production deployment, ensure to:
- Update secrets/postgres_password.txt with secure password
- Configure proper TLS certificates
- Review resource limits in podman-compose.yml

EOF
}

main() {
    log_info "🐧 Setting up Podman + Aspire for International Center Services APIs"
    
    check_podman_installed
    setup_podman_configuration
    setup_aspire_network
    setup_development_volumes
    configure_aspire_compatibility
    verify_setup
    
    print_usage_instructions
    
    log_success "🎉 Podman + Aspire setup completed successfully!"
}

# Run main function if script is executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi