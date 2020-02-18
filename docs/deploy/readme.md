# Deploying Azure Industrial IoT Platform

[Home](../readme.md)

## Quickstart

The simplest way to get started is to deploy the [Azure Industrial IoT Platform and Simulation demonstrator using the deployment script](howto-deploy-all-in-one.md).  Unless you decide otherwise, it also deploys 2 simulated Edge Gateways and assets.   

> The all in one hosting option is intended as a quick start solution.   For production deployments that require staging, rollback, scaling and resilience you should deploy the platform into Kubernetes as explained [here](howto-deploy-aks.md).

To connect your physical Assets [install one ore more Azure IoT Edge Gateways](howto-install-iot-edge.md).

## Other hosting and deployment methods

Alternative options to deploy the platform services include:

- Deploying the Industrial IoT platform to [Azure Kubernetes Service (AKS)](howto-deploy-aks.md) as production solution.
- For development and testing purposes, you can also [deploy only the Microservices dependencies in Azure](howto-deploy-local.md) and [run Microservices locally](howto-run-microservices-locally.md)
- [Enable ASC for IoT](howto-enable-asc-for-iot-and-sentinel-steps.md) if you want to monitor security of OPC UA endpoints


