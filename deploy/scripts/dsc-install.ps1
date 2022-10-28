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

        WindowsOptionalFeature VirtualMachinePlatform {
            Name   = "VirtualMachinePlatform"
            Ensure = "Enable"
        }

        WindowsFeature Hyper-V-Management-Tools {
            Name = "RSAT-Hyper-V-Tools"
            Ensure = "Present"
        }

        WindowsOptionalFeature Microsoft-Hyper-V-Management-PowerShell {
            Name   = "Microsoft-Hyper-V-Management-PowerShell"
            Ensure = "Enable"
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
            GetScript = { @{ Result = (Get-WindowsCapability -Online -Name "OpenSSH.Client*").State } }
        }

        WindowsOptionalFeature Microsoft-Windows-Subsystem-Linux {
            Name   = "Microsoft-Windows-Subsystem-Linux"
            Ensure = "Enable"
        }
    }
}
