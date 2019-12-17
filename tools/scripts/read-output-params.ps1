<#
 .SYNOPSIS
    Sets pipeline variables

 .DESCRIPTION
    The output of the AzureResourceManagerTemplateDeployment task is available via deploymentOutputs variable.
    This script reads that variable, parses it and sets the resource manager deployment output such as they can
    be used by other tasks.

 .PARAMETER armOutputString
    The output of the AzureResourceManagerTemplateDeployment task
#>

param (
    [Parameter(Mandatory=$true)]
    [string]
    $armOutputString = ''
)

$armOutputObj = $armOutputString | convertfrom-json

$armOutputObj.PSObject.Properties | ForEach-Object {
    $type = ($_.value.type).ToLower()
    $keyname = "Output_"+$_.name
    $value = $_.value.value

    if ($type -eq "securestring") {
        Write-Output "##vso[task.setvariable variable=$keyname;issecret=true]$value"
        Write-Output "Added VSTS variable '$keyname' ('$type')"
    } elseif ($type -eq "string") {
        Write-Output "##vso[task.setvariable variable=$keyname]$value"
        Write-Output "Added VSTS variable '$keyname' ('$type') with value '$value'"
    } else {
        Throw "Type '$type' is not supported for '$keyname'"
    }
}
