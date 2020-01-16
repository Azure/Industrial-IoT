# Deploying Azure Industrial IoT Platform

[Home](../readme.md)

## Quickstart

The simplest way to get started is to deploy the [Azure Industrial IoT Platform and Simulation demonstrator using the deployment script](howto-deploy-all-in-one.md).  Unless you decide otherwise, it also deploys 2 simulated Edge Gateways and assets.

To connect your physical Assets [install one ore more Azure IoT Edge Gateways](howto-install-iot-edge.md).

## Other hosting and deployment methods

Alternative options to deploy the platform services include:

- Deploying the Industrial IoT platform to [Azure Kubernetes Service (AKS)](howto-deploy-aks.md).
- For development and testing purposes, you can also [deploy only the Microservices dependencies in Azure](howto-deploy-local.md) and [run Microservices locally](setup/howto-run-microservices-locally.md)
- [Enable ASC for IoT](enable-asc-for-iot-and-sentinel-steps.md) if you want to monitor security of OPC UA endpoints


