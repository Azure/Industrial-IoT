#!/bin/bash -e

# Check powershell available and if not install
silent=""
if ! [ -x "$(command -v pwsh)" ]; then
  if [ -x "$(command -v lsb_release)" ]; then
    if [ -n $silent ]; then
      echo -e "\033[1;31mPwsh (Powershell) is not installed but required to deploy.\033[0m"
      while true; do
        read -p "Do you want to install Powershell (requires sudo privileges)? (y/n) " yn
        case $yn in
          [Yy]* ) break;;
          * ) echo "You must install Powershell manually to continue..."; exit 1;;
        esac
      done
    fi
    # install powershell
    ubuntuversion="$(lsb_release -rs)"
    curl -O https://packages.microsoft.com/config/ubuntu/$ubuntuversion/packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    rm -f packages-microsoft-prod.deb
    sudo apt-get update
    sudo apt-get install -y --no-install-recommends powershell
    silent="yes"
  else
    echo "Error: Pwsh (Powershell) is not installed but required to deploy - please install it." >&2
    exit 1
  fi
fi

test=$(pwsh -Command "Get-Module -ListAvailable -Name Az.* | ForEach-Object Name")
if [ -z "$test" ]; then
  if [ -n $silent ]; then
    echo -e "\033[1;31mAz Powershell module is not installed but is required to deploy.\033[0m"
    while true; do
      read -p "Do you want to install Az module (requires sudo privileges)? (y/n) " yn
      case $yn in
        [Yy]* ) break;;
        * ) echo "You must install Az Powershell module manually to continue..."; exit 1;;
      esac
    done
  fi
  sudo pwsh -Command "Set-psrepository -Name PSGallery -InstallationPolicy Trusted"
  sudo pwsh -Command "Install-Module -Repository PSGallery -Name Az -AllowClobber"
  silent="yes"
fi

test=$(pwsh -Command "Get-Module -ListAvailable -Name Microsoft.Graph | ForEach-Object Name")
if [ -z "$test" ]; then
  if [ -n $silent ]; then
    echo -e "\033[1;31mMicrosoft.Graph Powershell module is not installed but is required to deploy.\033[0m"
    while true; do
      read -p "Do you want to install Microsoft.Graph module (requires sudo privileges)? (y/n) " yn
      case $yn in
        [Yy]* ) break;;
        * ) echo "You must install Microsoft.Graph Powershell module manually to continue..."; exit 1;;
      esac
    done
  fi
  sudo pwsh -Command "Set-psrepository -Name PSGallery -InstallationPolicy Trusted"
  sudo pwsh -Command "Install-Module -Repository PSGallery -Name Microsoft.Graph -AllowClobber"
  silent="yes"
fi

# Call powershell script
curdir="$( cd "$(dirname "$0")" ; pwd -P )"
pwsh -File $curdir/deploy/scripts/deploy.ps1 "$@"
