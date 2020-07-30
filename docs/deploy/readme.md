# Deploying Azure Industrial IoT Platform

[Home](../readme.md)

## Quickstart

The simplest way to get started is to deploy the [Azure Industrial IoT Platform and Simulation demonstrator using the deployment script](howto-deploy-all-in-one.md).
Unless you decide otherwise, it also deploys 2 simulated Edge Gateways and assets.

> The all in one hosting option is intended as a quick start solution. For production deployments that require staging, rollback, scaling and resilience you should deploy the platform into Kubernetes as explained [here](howto-deploy-aks.md).

To connect your physical Assets [install one ore more Azure IoT Edge Gateways](howto-install-iot-edge.md).

## Other hosting and deployment methods

Alternative options to deploy the platform services include:

- Deploying Azure Industrial IoT Platform to [Azure Kubernetes Service (AKS)](howto-deploy-aks.md) as production solution.
- Deploying Azure Industrial IoT Platform microservices into an existing Kubernetes cluster [using Helm](howto-deploy-helm.md).
- Deploying [Azure Kubernetes Service (AKS) cluster on top of Azure Industrial IoT Platform created by deployment script and adding Azure Industrial IoT components into the cluster](howto-add-aks-to-ps1.md).
- For development and testing purposes, you can also [deploy only the Microservices dependencies in Azure](howto-deploy-local.md) and [run Microservices locally](howto-run-microservices-locally.md).
