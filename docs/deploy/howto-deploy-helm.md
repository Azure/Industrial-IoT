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
- `0.4.0` version of `azure-industrial-iot` Helm chart to deploy `2.8.0` version of Azure Industrial IoT
  Platform microservices.
- `0.4.1` version of `azure-industrial-iot` Helm chart to deploy `2.8.1` version of Azure Industrial IoT
  Platform microservices.
- `0.4.2` version of `azure-industrial-iot` Helm chart to deploy `2.8.2` version of Azure Industrial IoT
  Platform microservices.
- `0.4.3` version of `azure-industrial-iot` Helm chart to deploy `2.8.3` version of Azure Industrial IoT
  Platform microservices.
- `0.4.4` version of `azure-industrial-iot` Helm chart to deploy `2.8.4` and later version of Azure Industrial IoT
  Platform microservices.

## Installing The Chart

Please check documentation of `azure-industrial-iot` Helm chart for steps on how to create Azure
infrastructure resources necessary to run Azure Industrial IoT Platform and how to deploy the chart to a
Kubernetes cluster:

- For `0.1.0` version of the Chart: [Azure Industrial IoT Helm Chart v0.1.0](https://github.com/Azure/Industrial-IoT/blob/helm/0.1.0/deploy/helm/azure-industrial-iot/README.md)
- For `0.2.0` version of the Chart: [Azure Industrial IoT Helm Chart v0.2.0](https://github.com/Azure/Industrial-IoT/blob/helm/0.2.0/deploy/helm/azure-industrial-iot/README.md)
- For `0.3.1` version of the Chart: [Azure Industrial IoT Helm Chart v0.3.1](https://github.com/Azure/Industrial-IoT/blob/helm_0.3.1/deploy/helm/azure-industrial-iot/README.md)
- For `0.3.2` version of the Chart: [Azure Industrial IoT Helm Chart v0.3.2](https://github.com/Azure/Industrial-IoT/blob/helm_0.3.2/deploy/helm/azure-industrial-iot/README.md)
- For `0.4.0` version of the Chart: [Azure Industrial IoT Helm Chart v0.4.0](https://github.com/Azure/Industrial-IoT/blob/helm_0.4.0/deploy/helm/azure-industrial-iot/README.md)
- For `0.4.1` version of the Chart: [Azure Industrial IoT Helm Chart v0.4.1](https://github.com/Azure/Industrial-IoT/blob/helm_0.4.1/deploy/helm/azure-industrial-iot/README.md)
- For `0.4.2` version of the Chart: [Azure Industrial IoT Helm Chart v0.4.2](https://github.com/Azure/Industrial-IoT/blob/helm_0.4.2/deploy/helm/azure-industrial-iot/README.md)
- For `0.4.3` version of the Chart: [Azure Industrial IoT Helm Chart v0.4.3](https://github.com/Azure/Industrial-IoT/blob/helm_0.4.3/deploy/helm/azure-industrial-iot/README.md)
- For `0.4.4` version of the Chart: [Azure Industrial IoT Helm Chart v0.4.4](https://github.com/Azure/Industrial-IoT/blob/helm_0.4.4/deploy/helm/azure-industrial-iot/README.md)

For latest documentation and chart sources please check [deploy/helm/azure-industrial-iot/](../../deploy/helm/azure-industrial-iot/)
directory on `main` branch.

## Helm Repositories

You can find `azure-industrial-iot` Helm chart in the following [Helm Chart Repositories](https://helm.sh/docs/topics/chart_repository/)

- `https://azure.github.io/Industrial-IoT/helm`

  > NOTE: This is the recommended Helm chart repository to use.

  To add the repository:

  ```bash
  helm repo add industrial-iot https://azure.github.io/Industrial-IoT/helm
  ```

- `https://azureiiot.blob.core.windows.net/helm`

  > NOTE: This is a legacy Helm chart repository. We will keep updating it, but we recommend using `https://azure.github.io/Industrial-IoT/helm`.

  To add the repository:

  ```bash
  helm repo add azure-iiot https://azureiiot.blob.core.windows.net/helm
  ```

- `https://microsoft.github.io/charts/repo`

  > NOTE: This is a legacy Helm chart repository. We will keep updating it, but we recommend using `https://azure.github.io/Industrial-IoT/helm`.

  To add the repository:

  ```bash
  helm repo add microsoft https://microsoft.github.io/charts/repo
  ```

## Helm Hub

You also can find `azure-industrial-iot` Helm chart on [Helm Hub](https://hub.helm.sh/charts/microsoft/azure-industrial-iot).
