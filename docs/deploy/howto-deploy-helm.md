# Deploying Azure Industrial IoT Platform Microservices Using Helm

[Helm](https://helm.sh/) is a package manager for [Kubernetes](https://kubernetes.io/) which you can use to
deploy microservices of Azure Industrial IoT Platform into an existing Kubernetes cluster. To facilitate this
we have created `azure-industrial-iot` Helm charts. You can use it for `2.5`, `2.6`, `2.7` and `2.8` versions
of our microservices:

- `0.1.0` version of `azure-industrial-iot` Helm chart to deploy `2.5` version of Azure Industrial IoT
  Platform microservices.
- `0.2.0` version of `azure-industrial-iot` Helm chart to deploy `2.6` version of Azure Industrial IoT
  Platform microservices.
- `0.3.1` version of `azure-industrial-iot` Helm chart to deploy `2.7` version (up until `2.7.199`) of Azure
  Industrial IoT Platform microservices.
- `0.3.2` version of `azure-industrial-iot` Helm chart to deploy `2.7.206` version of Azure Industrial IoT
  Platform microservices.
- `0.4.0` version of `azure-industrial-iot` Helm chart to deploy `2.8` version of Azure Industrial IoT
  Platform microservices.

## Installing The Chart

Please check documentation of `azure-industrial-iot` Helm chart for steps on how to create Azure
infrastructure resources necessary to run Azure Industrial IoT Platform and how to deploy the chart to a
Kubernetes cluster:

- For `0.1.0` version of the Chart: [Azure Industrial IoT Helm Chart v0.1.0](https://github.com/Azure/Industrial-IoT/blob/helm/0.1.0/deploy/helm/azure-industrial-iot/README.md)
- For `0.2.0` version of the Chart: [Azure Industrial IoT Helm Chart v0.2.0](https://github.com/Azure/Industrial-IoT/blob/helm/0.2.0/deploy/helm/azure-industrial-iot/README.md)
- For `0.3.1` version of the Chart: [Azure Industrial IoT Helm Chart v0.3.1](https://github.com/Azure/Industrial-IoT/blob/helm_0.3.1/deploy/helm/azure-industrial-iot/README.md)
- For `0.3.2` version of the Chart: [Azure Industrial IoT Helm Chart v0.3.2](https://github.com/Azure/Industrial-IoT/blob/helm_0.3.2/deploy/helm/azure-industrial-iot/README.md)
- For `0.4.0` version of the Chart: [Azure Industrial IoT Helm Chart v0.3.3](https://github.com/Azure/Industrial-IoT/blob/helm_0.4.0/deploy/helm/azure-industrial-iot/README.md)

For latest (`0.4.0`) documentation and chart sources please check [deploy/helm/azure-industrial-iot/](../../deploy/helm/azure-industrial-iot/)
directory.

## Helm Repositories

You can find `azure-industrial-iot` Helm chart in the following [Helm Repositories](https://helm.sh/docs/topics/chart_repository/)

- `https://azureiiot.blob.core.windows.net/helm`

  To add the repository:

  ```bash
  helm repo add azure-iiot https://azureiiot.blob.core.windows.net/helm
  ```

- `https://microsoft.github.io/charts/repo`

  To add the repository:

  ```bash
  helm repo add microsoft https://microsoft.github.io/charts/repo
  ```

## Helm Hub

You also can find `azure-industrial-iot` Helm chart on [Helm Hub](https://hub.helm.sh/charts/microsoft/azure-industrial-iot).
