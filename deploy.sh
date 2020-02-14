#!/bin/bash -e

# Check powershell available and if not install
if ! [ -x "$(command -v pwsh)" ]; then
  if [ -x "$(command -v lsb_release)" ]; then
    echo -e "\033[1;31mPwsh (Powershell) is not installed but required to deploy.\033[0m"
    while true; do
        read -p "Do you want to install Powershell (requires sudo privileges)? (y/n) " yn
        case $yn in
            [Yy]* ) break;;
            * ) echo "You must install Powershell manually to continue..."; exit 1;;
        esac
    done
    # install powershell
    ubuntuversion="$(lsb_release -rs)"
    curl -O https://packages.microsoft.com/config/ubuntu/$ubuntuversion/packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    rm -f packages-microsoft-prod.deb
    sudo apt-get update
    sudo apt-get install -y --no-install-recommends powershell
    
    sudo pwsh -Command "Set-psrepository -Name PSGallery -InstallationPolicy Trusted"
    sudo pwsh -Command "Install-Module -Repository PSGallery -Name Az -AllowClobber"

    # TODO Check update to released version when available
    # sudo pwsh -Command "Install-Module -Repository PSGallery -Name AzureAD -AllowClobber"
    sudo pwsh -Command "Register-PackageSource -ForceBootstrap -Force -Trusted -ProviderName 'PowerShellGet' -Name 'Posh Test Gallery' -Location https://www.poshtestgallery.com/api/v2/"
    sudo pwsh -Command "Install-Module -Repository 'Posh Test Gallery' -Name AzureAD.Standard.Preview -RequiredVersion 0.0.0.10 -AllowClobber"
  else
    echo "Error: Pwsh (Powershell) is not installed but required to deploy - please install it." >&2
    exit 1
  fi
fi

# Call powershell script
curdir="$( cd "$(dirname "$0")" ; pwd -P )"
pwsh -File $curdir/deploy/scripts/deploy.ps1 "$@"

