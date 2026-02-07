#!/bin/bash
set -e


echo "Running post-create setup..."

# Fix .dotnet directory ownership
echo "Fixing .dotnet directory ownership..."
sudo chown -R vscode:vscode /home/vscode/.dotnet || true

# Display .NET version and runtime details
echo "Checking .NET installation..."
dotnet --info

# Ensure handy CLI tools are available (ripgrep only - minimal set)
echo "Installing development CLI utilities..."
sudo apt-get update -y
sudo apt-get install -y ripgrep || echo "Warning: ripgrep installation failed, continuing..."

# Install EF Core CLI matching the repo packages
EF_TOOLS_VERSION="10.0.*"
echo "Installing dotnet-ef $EF_TOOLS_VERSION..."
dotnet tool update --global dotnet-ef --version "$EF_TOOLS_VERSION" 2>/dev/null \
	|| dotnet tool install --global dotnet-ef --version "$EF_TOOLS_VERSION"

# Add vscode user to docker group
echo "Adding vscode user to docker group..."
sudo usermod -aG docker vscode

# Set docker socket permissions
echo "Setting docker socket permissions..."
sudo chmod 666 /var/run/docker.sock

# Verify docker is working
echo "Verifying Docker installation..."
docker --version

# Setup SSH agent
echo "Setting up SSH agent..."
if [ -z "$SSH_AUTH_SOCK" ]; then
	echo "Starting new SSH agent..."
	eval "$(ssh-agent -s)"
else
	echo "Using existing SSH agent at $SSH_AUTH_SOCK"
fi

# Try to load SSH keys if available
if compgen -G "/home/vscode/.ssh/id_*" >/dev/null 2>&1; then
	for key in /home/vscode/.ssh/id_*; do
		if [[ -f "$key" && "$key" != *.pub ]]; then
			if ssh-add "$key" >/dev/null 2>&1; then
				echo "Loaded SSH key: $key"
			else
				echo "Warning: Failed to load key $key (may require passphrase)"
			fi
		fi
	done
else
	echo "No default SSH keys found. You can add keys manually with ssh-add if needed."
fi

ensure_docker_context() {
	local name="$1"
	local description="$2"
	local host="$3"
	if docker context inspect "$name" >/dev/null 2>&1; then
		echo "Context '$name' already present."
	else
		echo "Creating docker context '$name' (${description})"
		docker context create "$name" --description "$description" --docker "host=$host"
	fi
}

ensure_docker_context "proxmox-home" "Remote engine on Home Proxmox" "ssh://roys@192.168.2.104"
ensure_docker_context "rpi-home" "Remote engine on Home Raspberry Pi" "ssh://roys@192.168.2.21"

echo

# Generate HTTPS developer certificate
echo "Generating HTTPS developer certificate..."
dotnet dev-certs https --clean
dotnet dev-certs https


echo "Post-create setup completed successfully!"
