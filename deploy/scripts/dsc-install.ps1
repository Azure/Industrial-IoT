Install-Module -Name ComputerManagementDsc -RequiredVersion 8.5.0

Configuration InstallWindowsFeatures {

    Import-DscResource -ModuleName PsDesiredStateConfiguration
    Import-DscResource -ModuleName ComputerManagementDsc

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
            Ensure = "Present"
        }

        WindowsFeature Hyper-V-Management-Tools {
            Name = "RSAT-Hyper-V-Tools"
            Ensure = "Present"
        }

        WindowsOptionalFeature Microsoft-Hyper-V-Management-PowerShell {
            Name   = "Microsoft-Hyper-V-Management-PowerShell"
            Ensure = "Present"
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

        WindowsCapability OpenSSH-Client {
            Name   = "OpenSSH.Client*"
            Ensure = "Present"
        }

        WindowsOptionalFeature Microsoft-Windows-Subsystem-Linux {
            Name   = "Microsoft-Windows-Subsystem-Linux"
            Ensure = "Present"
        }
    }
}