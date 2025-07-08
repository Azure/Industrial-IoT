<#
    This script forwards the MQTT port of the aio-broker-insecure
    service in the specified namespace.
    It is intended for debugging purposes in a Kubernetes cluster.
    Presumes the --enable-insecure-listener was used during az iot
    ops create.
#>
param(
    [string] $Namespace = "azure-iot-operations"
)
kubectl -n $Namespace port-forward service/aio-broker-insecure 1883:1883