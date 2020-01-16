# Deploying Azure Industrial IoT Platform

[Home](../readme.md)

The simplest way to get started is to deploy the [Azure Industrial IoT Platform and Simulation demonstrator using the deployment script](howto-deploy-all-in-one.md)..  

Alternative options to deploy the platform services include:

- Deploying the Industrial IoT platform to Azure Kubernetes Service (AKS) follow the steps outlined [here](howto-deploy-aks.md).
- For development and testing purposes, one can also [deploy only the Microservices dependencies in Azure](howto-deploy-local.md) and [run Microservices locally](setup/howto-run-microservices-locally.md)
- [Enable ASC for IoT](enable-asc-for-iot-and-sentinel-steps.md) if you want to monitor security of OPC UA endpoints

To connect your physical Equipment you should also [install one ore more Azure IoT Edge Gateways](howto-install-iot-edge.md).
