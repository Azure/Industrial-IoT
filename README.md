This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments

# OPC Publisher for Azure IoT Edge
This reference implementation demonstrates how Azure IoT Edge can be used to connect to existing OPC UA servers and publishes JSON encoded telemetry data from these servers in OPC UA "Pub/Sub" format (using a JSON payload) to Azure IoT Hub. All transport protocols supported by Azure IoT Edge can be used, i.e. HTTPS, AMQP and MQTT (the default).

This application, apart from including an OPC UA *client* for connecting to existing OPC UA servers you have on your network, also includes an OPC UA *server* on port 62222 that can be used to manage what gets published.

The application is implemented using .NET Core technology and is able to run on the platforms supported by .NET Core.

This application uses the OPC Foundations's OPC UA reference stack and therefore licensing restrictions apply. Visit http://opcfoundation.github.io/UA-.NETStandardLibrary/ for OPC UA documentation and licensing terms.

|Branch|Status|
|------|-------------|
|master|[![Build status](https://ci.appveyor.com/api/projects/status/6t7ru6ow7t9uv74r/branch/master?svg=true)](https://ci.appveyor.com/project/marcschier/iot-gateway-opc-ua-r4ba5/branch/master) [![Build Status](https://travis-ci.org/Azure/iot-gateway-opc-ua.svg?branch=master)](https://travis-ci.org/Azure/iot-gateway-opc-ua)|

# Building the Application
The application requires the .NET Core SDK 1.1.

## As native Windows application
Open the OpcPublisher.sln project with Visual Studio 2017 and build the solution by hitting F7.

## As Docker container

Depending if you use Docker Linux or Docker Windows containers, there are different configuration files (Dockerfile or Dockerfile.Windows) to use for building the container.
From the root of the repository, in a console, type:

    docker build -f <docker-configfile-to-use> -t <your-container-name> .

The `-f` option for `docker build` is optional and the default is to use Dockerfile. Docker also support building directly from a git repository, which means you also could build a Linux container by:

    docker build -t <your-container-name> .https://github.com/Azure/iot-edge-opc-publisher

# Configuring the OPC UA nodes to publish
The OPC UA nodes whose values should be published to Azure IoT Hub can be configured by creating a JSON formatted configuration file (defaultname: "publishednodes.json"). This file is updated and persisted by the application, when using it's OPC UA server methods "PublishNode" or "UnpublishNode".

The syntax of the configuration file is as follows:

    [
        {
            // Publisher will request the server at EndpointUrl to sample the node with the OPC sampling interval specified on command line (or the default value: OPC publishing interval)
            // and the subscription will publish the node value with the OPC publishing interval specified on command line (or the default value: server revised interval).
            // please consult the OPC UA specification for details on how OPC monitored node sampling interval and OPC subscription publishing interval settings are handled by the OPC UA stack.
            // the publishing interval of the data to Azure IoTHub is controlled by the command line settings (or the default: publish data to IoTHub at least each 1 second).

            // example for an EnpointUrl is: opc.tcp://win10iot:51210/UA/SampleServer
            "EndpointUrl": "opc.tcp://<your_opcua_server>:<your_opcua_server_port>/<your_opcua_server_path>",
            "OpcNodes": [
                {
                    // the identifier specifies the NamespaceUri and the node identifier in XML notation as specified in Part 6 of the OPC UA specification in the XML Mapping section.
                    "ExpandedNodeId": "nsu=http://opcfoundation.org/UA/;i=2258"
                }
            ]
        },


        {
            // Publisher will request the server at EndpointUrl to sample the node with the OPC sampling interval specified on command line (or the default value: OPC publishing interval).
            // Publisher will create a separate subscription to the OPC UA server to monitor the nodes and the OPC publishing interval for this subscription is set to 4000 ms.
            // if the OPC sampling interval is set to an higher value, Publisher will adjust the OPC publishing interval of the subscription to this value.
            // please consult the OPC UA specification for details on how OPC monitored node sampling interval and OPC subscription publishing interval settings are handled by the OPC UA stack.
            // the publishing interval of the data to Azure IoTHub is controlled by the command line settings (or the default: publish data to IoTHub at least each 1 second).
            "EndpointUrl": "opc.tcp://<your_opcua_server>:<your_opcua_server_port>/<your_opcua_server_path>",
            "OpcNodes": [
                {
                    "ExpandedNodeId": "nsu=http://opcfoundation.org/UA/;i=2258",
                    "OpcPublishingInterval": 4000
                }
            ]
        },


        {
            // Publisher will request the server at EndpointUrl to sample the nodes with the given different sampling intervals
            // and the subscription will publish the node value with the OPC publishing interval specified on command line (or the default value: server revised interval).
            // if the OPC publishing interval is set to a lower value, Publisher will adjust the OPC publishing interval of the subscription to the OPC sampling interval value.
            // Publisher will use for each publishing interval a separate subscription. Since in this case there is no publishing interval specified for the different nodes
            // there will be only one subscription used and the OPC UA stack will publish with the lowest sampling interval of a node.
            // please consult the OPC UA specification for details on how OPC monitored node sampling interval and OPC subscription publishing interval settings are handled by the OPC UA stack.
            // the publishing interval of the data to Azure IoTHub is controlled by the command line settings (or the default: publish data to IoTHub at least each 1 second).
            "EndpointUrl": "opc.tcp://<your_opcua_server>:<your_opcua_server_port>/<your_opcua_server_path>",
            "OpcNodes": [
                {
                    "ExpandedNodeId": "nsu=http://opcfoundation.org/UA/;i=2258",
                    // the OPC sampling interval to use for this node.
                    "OpcSamplingInterval": 1000
                },
                {
                    "ExpandedNodeId": "nsu=http://opcfoundation.org/UA/;i=2258",
                    // the OPC sampling interval to use for this node.
                    "OpcSamplingInterval": 2000
                },
                {
                    "ExpandedNodeId": "nsu=http://opcfoundation.org/UA/;i=2258",
                    // the OPC sampling interval to use for this node.
                    "OpcSamplingInterval": 3000
                },
                {
                    "ExpandedNodeId": "nsu=http://opcfoundation.org/UA/;i=2258",
                    // the OPC sampling interval to use for this node.
                    "OpcSamplingInterval": 4000
                }
            ]
        },


        {
            // Publisher will request the server at EndpointUrl to sample the nodes with the given different sampling intervals
            // and the subscription will publish the node value with the OPC publishing interval specified on command line (or the default value: server revised interval).
            // if the OPC publishing interval is set to a lower value, Publisher will adjust the OPC publishing interval of the subscription to the OPC sampling interval value.
            // Publisher will use for each publishing interval a separate subscription. Here it will create 4 subscriptions each with a different publishing interval.
            // please consult the OPC UA specification for details on how OPC monitored node sampling interval and OPC subscription publishing interval settings are handled by the OPC UA stack.
            // the publishing interval of the data to Azure IoTHub is controlled by the command line settings (or the default: publish data to IoTHub at least each 1 second).
            "EndpointUrl": "opc.tcp://<your_opcua_server>:<your_opcua_server_port>/<your_opcua_server_path>",
            "OpcNodes": [
                {
                    "ExpandedNodeId": "nsu=http://opcfoundation.org/UA/;i=2258",
                    // the OPC sampling interval to use for this node.
                    "OpcPublishingInterval": 1000
                },
                {
                    "ExpandedNodeId": "nsu=http://opcfoundation.org/UA/;i=2258",
                    // the OPC sampling interval to use for this node.
                    "OpcPublishingInterval": 2000
                },
                {
                    "ExpandedNodeId": "nsu=http://opcfoundation.org/UA/;i=2258",
                    // the OPC sampling interval to use for this node.
                    "OpcPublishingInterval": 3000
                },
                {
                    "ExpandedNodeId": "nsu=http://opcfoundation.org/UA/;i=2258",
                    // the OPC sampling interval to use for this node.
                    "OpcPublishingInterval": 4000
                }
            ]
        },


        // the format below is only supported for backward compatibility. you need to ensure that the
        // OPC UA server on the configured EndpointUrl has the namespaceindex you expect with your configuration.
        // please use the ExpandedNodeId syntax instead.
        {
            "EndpointUrl": "opc.tcp://<your_opcua_server>:<your_opcua_server_port>/<your_opcua_server_path>",
            "NodeId": {
                    // the identifier specifies the NamespaceIndex and the node identifier in XML notation as specified in Part 6 of the OPC UA specification in the XML Mapping section.
                "Identifier": "ns=0;i=2258"
            }
        }
    ]

# Running the Application

## Command line options
The complete usage of the application could be shown using the `--help` command line option and is as follows:

    OpcPublisher.exe <applicationname> [<iothubconnectionstring>] [<options>]

with:

    applicationname: the OPC UA application name to use, required
                     The application name is also used to register the publisher under this name in the
                     IoTHub device registry.

    iothubconnectionstring: the IoTHub owner connectionstring, optional

The following options are supported:

      --pf, --publishfile=VALUE
                             the filename to configure the nodes to publish.
                               Default: '/docker/publishednodes.json'
      --sd, --shopfloordomain=VALUE
                             the domain of the shopfloor. if specified this
                               domain is appended (delimited by a ':' to the '
                               ApplicationURI' property when telemetry is
                               ingested to IoTHub.
                               The value must follow the syntactical rules of a
                               DNS hostname.
                               Default: not set
      --sw, --sessionconnectwait=VALUE
                             specify the wait time in seconds publisher is
                               trying to connect to disconnected endpoints and
                               starts monitoring unmonitored items
                               Min: 10
                               Default: 10
      --vc, --verboseconsole=VALUE
                             the output of publisher is shown on the console.
                               Default: False
      --ih, --iothubprotocol=VALUE
                             the protocol to use for communication with Azure
                               IoTHub (allowed values: Amqp, Http1, Amqp_
                               WebSocket_Only, Amqp_Tcp_Only, Mqtt, Mqtt_
                               WebSocket_Only, Mqtt_Tcp_Only).
                               Default: Mqtt
      --ms, --iothubmessagesize=VALUE
                             the max size of a message which could be send to
                               IoTHub. when telemetry of this size is available
                               it will be sent.
                               0 will enforce immediate send when telemetry is
                               available
                               Min: 0
                               Max: 256 * 1024
                               Default: 4096
      --si, --iothubsendinterval=VALUE
                             the interval in seconds when telemetry should be
                               send to IoTHub. If 0, then only the
                               iothubmessagesize parameter controls when
                               telemetry is sent.
                               Default: '1'
      --lf, --logfile=VALUE  the filename of the logfile to use.
                               Default: './Logs/<applicationname>.log.txt'
      --pn, --portnum=VALUE  the server port of the publisher OPC server
                               endpoint.
                               Default: 62222
      --pa, --path=VALUE     the enpoint URL path part of the publisher OPC
                               server endpoint.
                               Default: '/UA/Publisher'
      --lr, --ldsreginterval=VALUE
                             the LDS(-ME) registration interval in ms. If 0,
                               then the registration is disabled.
                               Default: 0
      --ot, --operationtimeout=VALUE
                             the operation timeout of the publisher OPC UA
                               client in ms.
                               Default: 120000
      --oi, --opcsamplinginterval=VALUE
                             the publisher is using this as default value in
                               milliseconds to request the servers to sample
                               the nodes with this interval
                               this value might be revised by the OPC UA
                               servers to a supported sampling interval.
                               please check the OPC UA specification for
                               details how this is handled by the OPC UA stack.
                               a negative value will set the sampling interval
                               to the publishing interval of the subscription
                               this node is on.
                               0 will configure the OPC UA server to sample in
                               the highest possible resolution and should be
                               taken with care.
                               Default: 1000
      --op, --opcpublishinginterval=VALUE
                             the publisher is using this as default value in
                               milliseconds for the publishing interval setting
                               of the subscriptions established to the OPC UA
                               servers.
                               please check the OPC UA specification for
                               details how this is handled by the OPC UA stack.
                               a value less than or equal zero will let the
                               server revise the publishing interval.
                               Default: 0
      --ct, --createsessiontimeout=VALUE
                             specify the timeout in seconds used when creating
                               a session to an endpoint. On unsuccessful
                               connection attemps a backoff up to 5 times the
                               specified timeout value is used.
                               Min: 1
                               Default: 10
      --ki, --keepaliveinterval=VALUE
                             specify the interval in seconds the publisher is
                               sending keep alive messages to the OPC servers
                               on the endpoints it is connected to.
                               Min: 2
                               Default: 2
      --kt, --keepalivethreshold=VALUE
                             specify the number of keep alive packets a server
                               could miss, before the session is disconneced
                               Min: 1
                               Default: 5
      --st, --opcstacktracemask=VALUE
                             the trace mask for the OPC stack. See github OPC .
                               NET stack for definitions.
                               To enable IoTHub telemetry tracing set it to 711.

                               Default: 285  (645)
      --as, --autotrustservercerts=VALUE
                             the publisher trusts all servers it is
                               establishing a connection to.
                               Default: False
      --tm, --trustmyself=VALUE
                             the publisher certificate is put into the trusted
                               certificate store automatically.
                               Default: True
      --at, --appcertstoretype=VALUE
                             the own application cert store type.
                               (allowed values: Directory, X509Store)
                               Default: 'X509Store'
      --ap, --appcertstorepath=VALUE
                             the path where the own application cert should be
                               stored
                               Default (depends on store type):
                               X509Store: 'CurrentUser\UA_MachineDefault'
                               Directory: 'CertificateStores/own'
      --tt, --trustedcertstoretype=VALUE
                             the trusted cert store type.
                               (allowed values: Directory, X509Store)
                               Default: Directory
      --tp, --trustedcertstorepath=VALUE
                             the path of the trusted cert store
                               Default (depends on store type):
                               X509Store: 'CurrentUser\UA_MachineDefault'
                               Directory: 'CertificateStores/UA Applications'
      --rt, --rejectedcertstoretype=VALUE
                             the rejected cert store type.
                               (allowed values: Directory, X509Store)
                               Default: Directory
      --rp, --rejectedcertstorepath=VALUE
                             the path of the rejected cert store
                               Default (depends on store type):
                               X509Store: 'CurrentUser\UA_MachineDefault'
                               Directory: 'CertificateStores/Rejected
                               Certificates'
      --it, --issuercertstoretype=VALUE
                             the trusted issuer cert store type.
                               (allowed values: Directory, X509Store)
                               Default: Directory
      --ip, --issuercertstorepath=VALUE
                             the path of the trusted issuer cert store
                               Default (depends on store type):
                               X509Store: 'CurrentUser\UA_MachineDefault'
                               Directory: 'CertificateStores/UA Certificate
                               Authorities'
      --dt, --devicecertstoretype=VALUE
                             the iothub device cert store type.
                               (allowed values: Directory, X509Store)
                               Default: X509Store
      --dp, --devicecertstorepath=VALUE
                             the path of the iot device cert store
                               Default Default (depends on store type):
                               X509Store: 'IoTHub'
                               Directory: 'CertificateStores/IoTHub'
      -h, --help                 show this message and exit

There are a couple of environment variables which could be used to control the application:
_HUB_CS: sets the IoTHub owner connectionstring
_GW_LOGP: sets the filename of the log file to use
_TPC_SP: sets the path to store certificates of trusted stations
_GW_PNFP: sets the filename of the publishing configuration file

Command line arguments overrule environment variable settings.

Typically you specify the IoTHub owner connectionstring only on the first start of the application. The connectionstring will be encrypted and stored in the platforms certificiate store.
On subsequent calls it will be read from there and reused. If you specify the connectionstring on each start, the device which is created for the application in the IoTHub device registry will be removed and recreated each time.


## Native on Windows
Open the OpcPublisher.sln project with Visual Studio 2017, build the solution and publish it. You could start the application in the 'Target directory' you have published to with:

    dotnet OpcPublisher.dll <applicationname> [<iothubconnectionstring>] [options]


## Using a self-built container
Build your own container and then start the container:

    docker run <your-container-name> <applicationname> [<iothubconnectionstring>] [options]

## Using a container from hub.docker.com
There is a prebuilt container available on DockerHub. To start it, just do:

    docker run microsoft/iot-edge-opc-publisher <applicationname> [<iothubconnectionstring>] [options]

## Important when using a container

### Access to the Publisher OPC UA server
The Publisher OPC UA server listens by default on port 62222. To expose this inbound port in a container, you need to use `docker run` option `-p`:

    docker run -p 62222:62222 microsoft/iot-edge-opc-publisher <applicationname> [<iothubconnectionstring>] [options]

### Access to the host network
To enable name resolution from within the container to the host network, you need to create a user define docker bridge network and connect the container to this network using the `--network`option as in this example:

    docker network create -d bridge iot_edge
    docker run --network iot_edge microsoft/iot-edge-opc-publisher <applicationname> [<iothubconnectionstring>] [options]

### Access the container via a name on the network
To access the Publisher container via a name from another container you need to assign a name to the container using the `--name` option as in this example:

    docker run --name publisher microsoft/iot-edge-opc-publisher <applicationname> [<iothubconnectionstring>] [options]
The Publisher container could now be reached by other containers via the name `publisher`over the network.

### Assigning a hostname
Publisher uses the hostname of the machine is running on for certificate and endpoint generation. docker chooses a random hostname if there is none set by the `-h` option. Here an example to set the internal hostname of the container to `publisher`:

    docker run -h publisher microsoft/iot-edge-opc-publisher <applicationname> [<iothubconnectionstring>] [options]
### Access to host volume (shared filesystem)
In certain use cases it may make sense to use configuration information, certificate stores or log file locations from the host and not keep them in the container file system only. To achieve this you need to use the `-v` option of `docker run`.

# Debugging the Application

## Native on Winodws

Open the OpcPublisher.sln project with Visual Studio 2017 and start debugging the app by hitting F5.

## In a docker container

Visual Studio 2017 supports debugging of application in docker container. This is done by using docker-compose. Since this does not allow to pass command line parameters it is not convenient. 
Another debugging option VS2017 supports is to debug via ssh. In the root of the repository the docker build configuration file `Dockerfile.ssh`could be used to create a SSH enabled container by:

    docker build -f .\Dockerfile.ssh -t publisherssh .

The container could now be started for publisher debugging purposes with:

    docker run -it publisherssh

In the container you need to manually start the ssh daemon with:

    service ssh start

At this point you should be able to create an ssh session as user `root` with the password `Passw0rd`.

To prepare debugging of the application in the container you need to do the following additional steps:

On the host side create a launch.json:

    {
      "version": "0.2.0",
      "adapter": "C:\\Users\\johanng\\Work Folders\\tools\\plink.exe",
      "adapterArgs": "root@localhost -pw Passw0rd -batch -T ~/vsdbg/vsdbg --interpreter=vscode",
      "languageMappings": {
        "C#": {
          "languageId": "3F5162F8-07C6-11D3-9053-00C04FA302A1",
          "extensions": [ "*" ]
        }
      },
      "exceptionCategoryMappings": {
        "CLR": "449EC4CC-30D2-4032-9256-EE18EB41B62B",
        "MDA": "6ECE07A9-0EDE-45C4-8296-818D8FC401D4"
      },
      "configurations": [
        {
          "name": ".NET Core Launch",
          "type": "coreclr",
          "cwd": "~/publisher",
          "program": "Opc.Ua.Publisher.dll",
          "args": "<put-the-publisher-command-line-options-here>",

          "request": "launch"
        }
      ]
    }

Build your project and publish it to a directory of your choice.

Use a tool like WinSCP to copy over the published files to the container into the directory `/root/publisher` (this could be also a different directory, but needs to be in sync with the `cdw` property of launch.json.

Now you could start debugging with the following command in Visual Studio's Command Window (View->Other Windows->Command Window):
DebugAdapterHost.Launch /LaunchJson:"<path-to-the-launch.json-file-you-saved>"





