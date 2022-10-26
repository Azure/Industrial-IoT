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
    }
}