# How to estimate the costs of running the Azure Industrial IoT Platform

[Home](readme.md)

## Overview
The costs of running the [Azure IIoT Platform](https://github.com/Azure/Industrial-IoT) in Azure depend on factors such as:
* Number of OPC UA servers (e.g. PLCs)
* Number of nodes per server
* Frequency of data changes
* Node data types

In order to estimate your monthly costs, follow the steps below to create a simulation to gather the data for the calculation.

### IoT Hub
While there are several resources needed to run the IIoT Cloud Platform, most of the costs are incurred by the IoT Hub. The [pricing](https://azure.microsoft.com/en-us/pricing/details/iot-hub/) page lists a few editions with different daily message limits:

| IoT Hub size | Max. msg/day  |
| ------------ | ------------: |
| S1           |       400,000 |
| S2           |     6,000,000 |
| S3           |   300,000,000 |

### IoT Hub pricing
 The pricing page also mentions *MESSAGE METER SIZE*, which is the measure of the message chunks that are discounted from the daily contingent. If a device-to-cloud (D2C) message is within 4 KB, it will be charged as 1 message. However, if the message exceeds 4 KB by even 1 byte, it will be charged as 2 messages. See [Azure IoT Hub pricing information](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-pricing) and [Message size](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-messages-construct#message-size) for more details.

### IoT Hub message optimization
Crafting message chunks in an optimal way can lead to large savings. For instance, consider 10 IoT devices that send a 1-KB message every second. These messages will be charged as 10 message chunks per second in total. Consequently, you will need IoT Hub capacity for 10 * 86,400 = 864,000 messages per day, hence at least three S1 units. 

However, if every device instead sends a 4-KB message every 4 seconds, the messages will be charged as 10 message chunks every 4 seconds. This translates to 86,400 / 4 = 21,600 messages per device, for a total of 216,000 messages for all 10 devices per day. In this case, one S1 unit would be enough.

If the requirements state that waiting 4 seconds for the data is not acceptable, that will of course affect the monthly costs. See [IoT Hub quotas and throttling](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-quotas-throttling) and [IoT Hub limits](https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/azure-subscription-service-limits#iot-hub-limits) for further relevant information.

### Reference factories
The following factory setups are provided for reference. A slow node changes every 10 s, a fast one every 1 s.

| Factory size | PLCs | Slow nodes | Fast nodes | Values/s/PLC | VM size | IoT Hub |
| ------------ | ---: | ---------: | ---------: | -----------: | ------- | ------- |
| Small        |   25 |        250 |         50 |           75 | >= B2ms | 8 * S1  |
| Medium       |   50 |      1,250 |        250 |          375 | >= B2ms | 5 * S2  |
| Large        |   75 |      3,750 |        750 |        1,125 | >= B4ms | 1 * S3  |

## Simulation
1. Deploy IoT Hub or [![Deploy minimal configuration to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2FIndustrial-IoT%2Fmain%2Fdeploy%2Ftemplates%2Fazuredeploy.minimum.json) to a resource group called `rg-iiot`, use the reference above to roughly size the hub
2. To deploy the [ARM template](../../deploy/templates/azuredeploy.minimum.json) manually on the Azure Portal:
- Create the resource group and select it
- Click on *Add* and select *Template deployment (deploy using custom templates)*
- Click on *Build your own template in the editor*
- Click on *Load file*, select the template and click on *Save*
3. On the IoT Hub's blade, add an IoT Edge device
4. Click on the new IoT Edge device, then *Set Modules -> Add Marketplace Module* and add the following:
  - OPC Publisher
5. Set the container options for the OPC Publisher. The `si` value sets publishing of messages to the IoT Hub to every 10 s. The option `mm` sets the messaging mode and `me` the encoding.   
```
{
    "Cmd": [
        "--pf=/appdata/pn.json",
        "--di=60",
        "--aa",
        "--si=10",
        "--bs=100",
        "--mm=PubSub",
        "--me=Uadp"
    ],
    "HostConfig": {
        "Binds": [
            "/var/iiotedge:/appdata"
        ]
    }
}
```
6. Copy the *Primary Connection String*

7. Deploy the factory simulation [![Deploy factory simulation to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2FIndustrial-IoT%2Fmain%2Fdocs%2Ftutorials%2Fazuredeploy.aci.simulation.json) ([ARM template](./azuredeploy.aci.simulation.json)) to a resource group called `rg-plcs`
- The simulated factory uses Azure Container Instances (ACI) to simulate the PLCs, which are currently limited to 100 units per Azure region
- Configure the number of slow and fast nodes, the change rate and data types (e.g. uint)
- Paste the IoT Edge *Primary Connection String*
- Note that a large deployment may sometimes fail due to a resource conflict, in this case, retry the deployment 2-3 times

8. Check IoT Edge connection status and metrics on the IoT Hub's overview tab to make sure that messages are arriving. It can take up to 10 minutes to reflect expected values, depending on the number of PLCs etc. Allow at least 1 hour to gather reliable data in the form of a stable graph without too many wiggles.

### Adjust IoT Hub
1. On the IoT Hub's blade, click on *Overview -> Device to cloud messages -> Add metric -> Total number of messages used*
2. Set the *Time range* to *Last 3 days*
3. The daily message counter is a line that increases steeply, and gets reset at 0:00 UTC
4. If the line is flat at the top before the daily reset, then the IoT hub's size or number of units needs to be increased
5. The peak of the line should always be within the limits of your IoT Hub's capacity
6. Note that in some cases, a larger IoT Hub may be better priced than several units of a lower size one

An additional and quick way to get IoT Hub sizing information is by login to the Edge device and viewing the OPC Publisher's logs with `iotedge logs OPCPublisher`. The diagnostic information for each connected PLC shows a line with the *Estimated IoT Chunks per day*. The longer the simulation runs, the more reliable this number will get. Multiply it by the total number of PLCs to get the estimated number of messages per day that the IoT Hub should be able to handle. Here you can also ensure that *Ingress ValueChanges* matches the expected number of messages per second.

### Estimate costs
The costs of the IoT Hub configuration will be the bulk of your monthly costs. To estimate the costs for the entire resource group, including all IIoT resources, follow these steps:
1. The IoT Hub's daily message counter gets reset at 0:00 UTC, so run the factory simulation for at least 3 days to get a good estimation that is unaffected by time differences
2. Open the resource group `rg-iiot` that contains the IIoT resources
3. Click on *Cost Management -> Cost analysis*
4. Click on the calendar, then on *Custom date range*
5. Select the day that is in the middle of the 3-day period in which the simulation ran
6. You should see the *ACTUAL COST* of running the platform for 1 day, multiply this number by 31 to get the monthly costs

### Format
The encoding format used for the messages to the IoT Hub can heavily affect the costs. Using the configuration for the OPC Publisher above, the publisher sends messages in the binary Uadp format. Using an uncompressed format such as JSON (--mm=Samples --me=Json) will increase costs by a factor of 5 to 10.

### Larger factories
For very large factories, the above method may approach the limits of the Edge VM or its network connection. If *Ingress ValueChanges* does not match the expected number of messages per second, the daily estimate will not be correct.

One way to deal with this would be to split the factory and then add the results. For instance, instead of creating 3750 slow and 750 fast nodes for the large reference factory, you may create 3750 slow and 0 fast nodes, then run the simulation for a few minutes/hours and then note the daily estimate. Afterwards you would do the opposite and run the simulation with 0 slow and 750 fast nodes. Finally, you can add the daily estimates to get the overall result.

### Troubleshooting
#### Edge device
- Enable Boot diagnostics
- Serial console
  - Check pn.json  
`cat /tmp/pn.log`  
`cat /var/iiotedge/pn.json`  
  - Check web access to the PLC(s)  
`ping <PLC>`  
`curl http://<PLC>/pn.json`
  - Check logs  
`iotedge list`  
`iotedge logs OPCPublisher --tail 100`
  - Restart OPC Publisher  
`iotedge restart OPCPublisher`

#### PLC
- Check *Containers -> Logs*

#### IoT Hub
- Check status of daily quota
- IoT Edge: Check OPC Publisher -> Container Create Options
  - "Cmd" contains: `"--pf=/appdata/pn.json"`
  - "Binds" contains `"/var/iiotedge:/appdata"`
