Simulation
==========

The Edge simulation API allows you to create a number of *Simulation Environment*s consisting of a IoT Edge device and simulated devices, which can be individually controlled.

The client library creates individual Linux Virtual Machines for each *Simulation Environment* using Azure Management client and installs the iot edge runtime in them using cloud init. It thereby follows the steps in https://docs.microsoft.com/en-us/azure/iot-edge/how-to-install-iot-edge-linux.

Each IoT Edge device rollout will be provisioned in IoT Hub together with the provided module manifest.
FUTURE: The client also allows auto provisioning the edge rollout using DPS enrollment group.  It follows the steps layed out in https://docs.microsoft.com/en-us/azure/iot-dps/tutorial-group-enrollments.

Each simulated device can be started and stopped using a handle to the created *Simulation Environment*.  In addition it can also be access using SSH directly (using SSHClient).  

The environment is deleted once it is disposed, cleaning up all resources, including the IoT Edge device that was provisioned in IoT Hub.