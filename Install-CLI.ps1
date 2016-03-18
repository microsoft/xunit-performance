[CmdletBinding()]



Param(
    [Parameter(Mandatory=$True, Position=1)]

    [string]$ToolsDir
)

$BootstrapScript = "$ToolsDir\Bootstrap.ps1"

# Ensure the tools directory exists.
if(!(Test-Path "$ToolsDir"))
{
	mkdir "$ToolsDir" | Out-Null
}

# Download the bootstrap script.
$req = Invoke-WebRequest -UseBasicParsing "https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/install.ps1"
$req.Content | Out-File "$BootstrapScript"

# Save and override the CLI install environment var.
$savedInstallDir = $env:DOTNET_INSTALL_DIR
$env:DOTNET_INSTALL_DIR = $ToolsDir

# Execute the bootstrap script
Invoke-Expression -Command "$BootstrapScript -Channel beta"

# Restore the CLI install environment var.
$env:DOTNET_INSTALL_DIR = $savedInstallDir