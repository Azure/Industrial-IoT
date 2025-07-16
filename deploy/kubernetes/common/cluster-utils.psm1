# Common utilities

<#
    .SYNOPSIS
        Imports a container image into a Kubernetes cluster.
    .DESCRIPTION
        Imports a container image into different types of Kubernetes
        clusters (microk8s, k3d, kind).
        Handles the specific import requirements for each cluster type.
    .PARAMETER ClusterType
        The type of Kubernetes cluster (microk8s, k3d, or kind).
    .PARAMETER ContainerImage
        The container image to import.
#>
function Import-ContainerImage {
    [CmdletBinding()]
    param(
        [string] [Parameter(Mandatory = $true)] [ValidateSet(
            "microk8s",
            "k3d",
            "kind")] $ClusterType,
        [string] [Parameter(Mandatory = $true)] $ContainerImage
    )
    Write-Host "Importing $ContainerImage into $ClusterType cluster..." -ForegroundColor Cyan
    switch ($ClusterType) {
        "microk8s" {
            # Windows only, support on linux is not implemented yet
            $imageTar = Join-Path $env:TEMP "image.tar"
            docker image save $ContainerImage -o $imageTar
            multipass transfer $imageTar microk8s-vm:/tmp/image.tar
            microk8s ctr image import /tmp/image.tar
            Remove-Item -Path $imageTar -Force
            docker image rm -f $ContainerImage
            $ContainerImage = "docker.io/$ContainerImage"
        }
        "k3d" {
            # import in all clusters which avoids asking for the cluster name
            $clusters = $(& { k3d cluster list --no-headers } -split ")`n") `
                | ForEach-Object { $($_ -split " ")[0].Trim() }
            k3d image import $ContainerImage `
                --mode auto `
                --cluster $($clusters -join ",")
        }
        "kind" {
            kind load docker-image $ContainerImage
        }
    }
    Write-Host "Container $ContainerImage imported into $ClusterType cluster." -ForegroundColor Green
}

# Export module members
Export-ModuleMember -Function Import-ContainerImage