Configuration InstallWindowsFeatures {

    Import-DscResource -ModuleName PsDesiredStateConfiguration

    Node "localhost" {

        LocalConfigurationManager {
            RebootNodeIfNeeded = $true
            ActionAfterReboot  = 'ContinueConfiguration'
        }

        WindowsFeature Hyper-V {
            Name   = "Hyper-V"
            Ensure = "Present"
            IncludeAllSubFeature = $true
        }

        Script VirtualMachinePlatform {
            SetScript = { Enable-WindowsOptionalFeature -Online -FeatureName "VirtualMachinePlatform" }
            TestScript = { (Get-WindowsOptionalFeature -Online -FeatureName "VirtualMachinePlatform").State -eq "Enabled" }
            GetScript = { @{ Result = Get-WindowsOptionalFeature -Online -FeatureName "VirtualMachinePlatform" } }
        }

        WindowsFeature Hyper-V-Management-Tools {
            Name = "RSAT-Hyper-V-Tools"
            Ensure = "Present"
        }

        Script Microsoft-Hyper-V-Management-PowerShell {
            SetScript = { Enable-WindowsOptionalFeature -Online -FeatureName "Microsoft-Hyper-V-Management-PowerShell" }
            TestScript = { (Get-WindowsOptionalFeature -Online -FeatureName "Microsoft-Hyper-V-Management-PowerShell").State -eq "Enabled" }
            GetScript = { @{ Result = Get-WindowsOptionalFeature -Online -FeatureName "Microsoft-Hyper-V-Management-PowerShell" } }
        }

        WindowsFeature DHCP {
            Name   = "DHCP"
            Ensure = "Present"
            IncludeAllSubFeature = $true
        }

        WindowsFeature DHCP-Management-Tools {
            Name = "RSAT-DHCP"
            Ensure = "Present"
        }

        Script OpenSSH-Client-Capability {
            SetScript = { Add-WindowsCapability -Online -Name "OpenSSH.Client*" }
            TestScript = { (Get-WindowsCapability -Online -Name "OpenSSH.Client*").State -eq "Installed" }
            GetScript = { @{ Result = Get-WindowsCapability -Online -Name "OpenSSH.Client*" } }
        }
    }
}
