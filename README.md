This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments

# OPC Publisher for Azure IoT Edge
This reference implementation demonstrates how Azure IoT Edge can be used to connect to existing OPC UA servers and publishes JSON encoded telemetry data from these servers in OPC UA "Pub/Sub" format (using a JSON payload) to Azure IoT Hub. All transport protocols supported by Azure IoT Edge can be used, i.e. HTTPS, AMQP and MQTT (the default).

This application, apart from including an OPC UA *client* for connecting to existing OPC UA servers you have on your network, also includes an OPC UA *server* on port 62222 that can be used to manage what gets published.

The application is implemented using .NET Core technology and is able to run on the platforms supported by .NET Core.

Publisher implements a retry logic to establish connections to endpoints which have not responded to a certain number of keep alive requests, for example if the OPC UA server on this endpoint had a power outage.

For each distinct publishing interval to an OPC UA server it creates a separate subscription over which all nodes with this publishing interval are updated.

Publisher supports batching of the data sent to IoTHub, to reduce network load. This batching is sending a packet to IoTHub only if the configured package size is reached.

This application uses the OPC Foundations's OPC UA reference stack and therefore licensing restrictions apply. Visit http://opcfoundation.github.io/UA-.NETStandardLibrary/ for OPC UA documentation and licensing terms.

|Branch|Status|
|------|-------------|
|master|[![Build status](https://ci.appveyor.com/api/projects/status/6t7ru6ow7t9uv74r/branch/master?svg=true)](https://ci.appveyor.com/project/marcschier/iot-gateway-opc-ua-r4ba5/branch/master) [![Build Status](https://travis-ci.org/Azure/iot-gateway-opc-ua.svg?branch=master)](https://travis-ci.org/Azure/iot-gateway-opc-ua)|

# Building the Application
The application requires the .NET Core SDK 2.0.

## As native Windows application
Open the OpcPublisher.sln project with Visual Studio 2017 and build the solution by hitting F7.

## As Docker container

Depending if you use Docker Linux or Docker Windows containers, there are different configuration files (Dockerfile or Dockerfile.Windows) to use for building the container.
From the root of the repository, in a console, type:

    docker build -f <docker-configfile-to-use> -t <your-container-name> .

The `-f` option for `docker build` is optional and the default is to use Dockerfile. Docker also support building directly from a git repository, which means you also can build a Linux container by:

    docker build -t <your-container-name> https://github.com/Azure/iot-edge-opc-publisher

# Configuring the OPC UA nodes to publish
The OPC UA nodes whose values should be published to Azure IoT Hub can be configured by creating a JSON formatted configuration file (defaultname: "publishednodes.json"). This file is updated and persisted by the application, when using it's OPC UA server methods "PublishNode" or "UnpublishNode".

The syntax of the configuration file is as follows:

    [
        {
            // example for an EnpointUrl is: opc.tcp://win10iot:51210/UA/SampleServer
            "EndpointUrl": "opc.tcp://<your_opcua_server>:<your_opcua_server_port>/<your_opcua_server_path>",
            "OpcNodes": [
                // Publisher will request the server at EndpointUrl to sample the node with the OPC sampling interval specified on command line (or the default value: OPC publishing interval)
                // and the subscription will publish the node value with the OPC publishing interval specified on command line (or the default value: server revised publishing interval).
                {
                    // The identifier specifies the NamespaceUri and the node identifier in XML notation as specified in Part 6 of the OPC UA specification in the XML Mapping section.
                    "ExpandedNodeId": "nsu=http://opcfoundation.org/UA/;i=2258"
                },
                // Publisher will request the server at EndpointUrl to sample the node with the OPC sampling interval specified on command line (or the default value: OPC publishing interval)
                // and the subscription will publish the node value with an OPC publishing interval of 4 seconds.
                // Publisher will use for each dinstinct publishing interval (of nodes on the same EndpointUrl) a separate subscription. All nodes without a publishing interval setting,
                // will be on the same subscription and the OPC UA stack will publish with the lowest sampling interval of a node.
                {
                    "ExpandedNodeId": "nsu=http://opcfoundation.org/UA/;i=2258",
                    "OpcPublishingInterval": 4000
                },
                // Publisher will request the server at EndpointUrl to sample the node with the given sampling interval of 1 second
                // and the subscription will publish the node value with the OPC publishing interval specified on command line (or the default value: server revised interval).
                // If the OPC publishing interval is set to a lower value, Publisher will adjust the OPC publishing interval of the subscription to the OPC sampling interval value.
                {
                    "ExpandedNodeId": "nsu=http://opcfoundation.org/UA/;i=2258",
                    // the OPC sampling interval to use for this node.
                    "OpcSamplingInterval": 1000
                }
            ]
        },

        // the format below (NodeId format) is only supported for backward compatibility. you need to ensure that the
        // OPC UA server on the configured EndpointUrl has the namespaceindex you expect with your configuration.
        // please use the ExpandedNodeId format as in the examples above instead.
        {
            "EndpointUrl": "opc.tcp://<your_opcua_server>:<your_opcua_server_port>/<your_opcua_server_path>",
            "NodeId": {
                "Identifier": "ns=0;i=2258"
            }
        }
        // please consult the OPC UA specification for details on how OPC monitored node sampling interval and OPC subscription publishing interval settings are handled by the OPC UA stack.
        // the publishing interval of the data to Azure IoTHub is controlled by the command line settings (or the default: publish data to IoTHub at least each 1 second).
    ]
# Running the Application

## Command line options
The complete usage of the application can be shown using the `--help` command line option and is as follows:

    Usage: OpcPublisher.exe <applicationname> [<iothubconnectionstring>] [<options>]

    OPC Edge Publisher to subscribe to configured OPC UA servers and send telemetry to Azure IoTHub.
    To exit the application, just press ENTER while it is running.

    applicationname: the OPC UA application name to use, required
                     The application name is also used to register the publisher under this name in the
                     IoTHub device registry.

    iothubconnectionstring: the IoTHub owner connectionstring, optional

    There are a couple of environment variables which can be used to control the application:
    _HUB_CS: sets the IoTHub owner connectionstring
    _GW_LOGP: sets the filename of the log file to use
    _TPC_SP: sets the path to store certificates of trusted stations
    _GW_PNFP: sets the filename of the publishing configuration file

    Command line arguments overrule environment variable settings.

    Options:
          --pf, --publishfile=VALUE
                                 the filename to configure the nodes to publish.
                                   Default: 'D:\Repos\hg\iot-edge-opc-publisher\src\
                                   publishednodes.json'
          --sd, --shopfloordomain=VALUE
                                 the domain of the shopfloor. if specified this
                                   domain is appended (delimited by a ':' to the '
                                   ApplicationURI' property when telemetry is sent
                                   to IoTHub.
                                   The value must follow the syntactical rules of a
                                   DNS hostname.
                                   Default: not set
          --sw, --sessionconnectwait=VALUE
                                 specify the wait time in seconds publisher is
                                   trying to connect to disconnected endpoints and
                                   starts monitoring unmonitored items
                                   Min: 10
                                   Default: 10
          --mq, --monitoreditemqueuecapacity=VALUE
                                 specify how many notifications of monitored items
                                   could be stored in the internal queue, if the
                                   data could not be sent quick enough to IoTHub
                                   Min: 1024
                                   Default: 8192
          --di, --diagnosticsinterval=VALUE
                                 shows publisher diagnostic info at the specified
                                   interval in seconds. 0 disables diagnostic
                                   output.
                                   Default: 0
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
                                 the max size of a message which can be send to
                                   IoTHub. when telemetry of this size is available
                                   it will be sent.
                                   0 will enforce immediate send when telemetry is
                                   available
                                   Min: 0
                                   Max: 262144
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
                                   can miss, before the session is disconneced
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
          --fd, --fetchdisplayname=VALUE
                                 enable to read the display name of a published
                                   node from the server. this will increase the
                                   runtime.
                                   Default: False
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
                                   Directory: 'CertificateStores/trusted'
          --rt, --rejectedcertstoretype=VALUE
                                 the rejected cert store type.
                                   (allowed values: Directory, X509Store)
                                   Default: Directory
          --rp, --rejectedcertstorepath=VALUE
                                 the path of the rejected cert store
                                   Default (depends on store type):
                                   X509Store: 'CurrentUser\UA_MachineDefault'
                                   Directory: 'CertificateStores/rejected'
          --it, --issuercertstoretype=VALUE
                                 the trusted issuer cert store type.
                                   (allowed values: Directory, X509Store)
                                   Default: Directory
          --ip, --issuercertstorepath=VALUE
                                 the path of the trusted issuer cert store
                                   Default (depends on store type):
                                   X509Store: 'CurrentUser\UA_MachineDefault'
                                   Directory: 'CertificateStores/issuers'
          --dt, --devicecertstoretype=VALUE
                                 the iothub device cert store type.
                                   (allowed values: Directory, X509Store)
                                   Default: X509Store
          --dp, --devicecertstorepath=VALUE
                                 the path of the iot device cert store
                                   Default Default (depends on store type):
                                   X509Store: 'My'
                                   Directory: 'CertificateStores/IoTHub'
      -h, --help                 show this message and exit


There are a couple of environment variables which can be used to control the application:
* _HUB_CS: sets the IoTHub owner connectionstring
* _GW_LOGP: sets the filename of the log file to use
* _TPC_SP: sets the path to store certificates of trusted stations
* _GW_PNFP: sets the filename of the publishing configuration file

Command line arguments overrule environment variable settings.

Typically you specify the IoTHub owner connectionstring only on the first start of the application. The connectionstring will be encrypted and stored in the platforms certificiate store.
On subsequent calls it will be read from there and reused. If you specify the connectionstring on each start, the device which is created for the application in the IoTHub device registry will be removed and recreated each time.


## Native on Windows
Open the OpcPublisher.sln project with Visual Studio 2017, build the solution and publish it. You can start the application in the 'Target directory' you have published to with:

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

### Enable intercontainer nameresolution
To enable name resolution from within the container to other containers, you need to create a user define docker bridge network and connect the container to this network using the `--network` option.
Additionally you need to assign the container a name using the `--name` option as in this example:

    docker network create -d bridge iot_edge
    docker run --network iot_edge --name publisher microsoft/iot-edge-opc-publisher <applicationname> [<iothubconnectionstring>] [options]

The container can now be reached by other containers via the name `publisher`over the network.

### Access other systems from within the container
Other containers, can be reached using the parameters described in the "Enable intercontainer nameresolution" paragraph.
If operating system on which docker is hosted is DNS enabled, then accessing all systems which are known by DNS will work..
A problems occurs in a network which does use NetBIOS name resolution. To enable access to other systems (including the one on which docker is hosted) you need to start your container using the `--add-host` option,
which effectevly is adding an entry to the containers host file.

    docker run --add-host mydevbox:192.168.178.23  microsoft/iot-edge-opc-publisher <applicationname> [<iothubconnectionstring>] [options]

### Assigning a hostname
Publisher uses the hostname of the machine is running on for certificate and endpoint generation. docker chooses a random hostname if there is none set by the `-h` option. Here an example to set the internal hostname of the container to `publisher`:

    docker run -h publisher microsoft/iot-edge-opc-publisher <applicationname> [<iothubconnectionstring>] [options]

### Using bind mounts (shared filesystem)
In certain use cases it may make sense to read configuration information from or write log files to locations on the host and not keep them in the container file system only. To achieve this you need to use the `-v` option of `docker run` in the bind mount mode.

### Store for X509 certificates
Storing X509 certificates does not work with bind mounts, since the permissions of the path to the store need to be `rw` for the owner. Instead you need to use the `-v` option of `docker run` in the volume mode.

## Performance and memory considerations
### Commandline parameters contolling performance and memory
When running Publisher you need to be aware of your performance requirements and the memory resources you have available on your platform.
Since both are interdependent and both depend on the configuration of how many nodes are configured to publish, you should ensure that the parameters you are using for:
* IoTHub send interval (`--si`)
* IoTHub message size (`--ms`)
* Monitored Items queue capacity (`--mq`)
do meet your requirements.

The `--mq` parameter controls the upper bound of the capacity of the internal queue, which buffers all notifications if a value of an OPC node changes. If Publisher is not able to send messages to IoTHub fast enough,
then this queue buffers those notifications. The parameter sets the number of notifications which could be buffered. If you seen the number of items in this queue increasing in your test runs, you need to:
* decrease the IoTHub send interval (`--si`)
* increase the IoTHub message size (`--ms`)
otherwise you will loose the data values of those OPC node changes. The `--mq` parameter at the same time allows to prevent controlling the upper bound of the memory resources used by Publisher.

The `--si` parameter enforces Publisher to send messages to IoTHub as the specified interval. If there is an IoTHub message size specified via the `--ms` parameter (or by the default value for it),
then a message will be sent either when the message size is reached (in this case the interval is restarted) or when the specified interval time has passed. If you disable the message size by `--ms 0`, Publisher
uses the maximal possible IoTHub message size of 256 kB to batch data.

The `--ms` parameter allows you to enable batching of messages sent to IoTHub. Depending on the protocol you are using, the overhead to send a message to IoTHub is high compared to the actual time of sending the payload.
If your scenario allows latency for the data ingested, you should configure Publisher to use the maximal message size of 256 kB.

Before you use Publisher in production scenarios, you need to test the performance and memory under production conditions. You could use the `--di` commandline parameter to specify a interval in seconds,
which will trigger the output of diagnostic information at this interval.

### Test measurements
Here are some measurements with different values for `--si` and `--ms` parameters publishing 497 nodes with an OPC publishing interval of 1 second.
Publisher was used as debug build on Windows 10 natively for 120 seconds. The IoTHub protocol was configured to use HTTP (`--ih Http1`).

#### Default configuration (--si 1 --ms 4096)
 
        ======================================================================
        OpcPublisher status @ 25.10.2017 11:08:25
        ---------------------------------
        OPC sessions: 1
        connected OPC sessions: 1
        connected OPC subscriptions: 1
        OPC monitored items: 497
        ---------------------------------
        monitored items queue bounded capacity: 8192
        monitored items queue current items: 8107
        monitored item notifications enqueued: 57076
        monitored item notifications enqueue failure: 28551
        monitored item notifications dequeued: 20418
        ---------------------------------
        messages sent to IoTHub: 1200
        bytes sent to IoTHub: 4772773
        avg msg size: 3977
        time in ms for sent msgs: 113744
        min time in ms for msg: 84
        max time in ms for msg: 441
        avg time in ms for msg: 94
        msg send failures: 0
        time in ms for failed msgs: 0
        avg time in ms for failed msg: 0
        messages too large to sent to IoTHub: 0
        times we missed send interval: 0
        ---------------------------------
        current working set in MB: 90
        ======================================================================

The default configuration sends data to IoTHub each second or when 4kB of data to ingest is available. In this configuration we loose 28551 OPC node value updates (`monitored item notifications enqueue failure`).

#### Constant send inverval (--si 1 --ms 0)

        ======================================================================
        OpcPublisher status @ 25.10.2017 11:18:20
        ---------------------------------
        OPC sessions: 1
        connected OPC sessions: 1
        connected OPC subscriptions: 1
        OPC monitored items: 497
        ---------------------------------
        monitored items queue bounded capacity: 8192
        monitored items queue current items: 0
        monitored item notifications enqueued: 56682
        monitored item notifications enqueue failure: 0
        monitored item notifications dequeued: 56682
        ---------------------------------
        messages sent to IoTHub: 114
        bytes sent to IoTHub: 13130523
        avg msg size: 115180
        time in ms for sent msgs: 14454
        min time in ms for msg: 100
        max time in ms for msg: 705
        avg time in ms for msg: 126
        msg send failures: 0
        time in ms for failed msgs: 0
        avg time in ms for failed msg: 0
        messages too large to sent to IoTHub: 0
        times we missed send interval: 0
        ---------------------------------
        current working set in MB: 87
        ======================================================================

When the message size is set to 0 and there is a send interval configured (or the default of 1 second is used), then Publisher does use internally batch data using the maximal supported IoTHub message size, which is 256 kB. As you see in the diagnostic output,
the average message size is 115180 byte. In this configuration we do not loose any OPC node value udpates. The average time to send a message to IoTHub (`avg time in ms for msg`) was only 22ms higher than in the
default configuration, which has a average message size of 3877 byte.

#### Send each OPC node value update (--si 0 --ms 0)

        ======================================================================
        OpcPublisher status @ 25.10.2017 10:08:03
        ---------------------------------
        OPC sessions: 1
        connected OPC sessions: 1
        connected OPC subscriptions: 1
        OPC monitored items: 497
        ---------------------------------
        monitored items queue bounded capacity: 8192
        monitored items queue current items: 8192
        monitored item notifications enqueued: 57323
        monitored item notifications enqueue failure: 48116
        monitored item notifications dequeued: 1015
        ---------------------------------
        messages sent to IoTHub: 1014
        bytes sent to IoTHub: 237292
        avg msg size: 234
        time in ms for sent msgs: 114268
        min time in ms for msg: 84
        max time in ms for msg: 330
        avg time in ms for msg: 112
        msg send failures: 0
        time in ms for failed msgs: 0
        avg time in ms for failed msg: 0
        messages too large to sent to IoTHub: 0
        times we missed send interval: 0
        ---------------------------------
        current working set in MB: 92
        ======================================================================

This configuration sends for each OPC node value change a message to IoTHub. You see the average message size of 234 byte is pretty small and the average time required to send such a message was 
112 ms - which is compared to larger message sizes - a high value. The advantage of this configuration is that Publisher does not add any latency to the ingest data path. The number of
lost OPC node value updates (`monitored item notifications enqueue failure: 48116`) is the highest of all compared configurations.

#### Maximum batching (--si 0 --ms 262144)

        ======================================================================
        OpcPublisher status @ 25.10.2017 10:05:23
        ---------------------------------
        OPC sessions: 1
        connected OPC sessions: 1
        connected OPC subscriptions: 1
        OPC monitored items: 497
        ---------------------------------
        monitored items queue bounded capacity: 8192
        monitored items queue current items: 0
        monitored item notifications enqueued: 57086
        monitored item notifications enqueue failure: 0
        monitored item notifications dequeued: 57086
        ---------------------------------
        messages sent to IoTHub: 50
        bytes sent to IoTHub: 13096746
        avg msg size: 261934
        time in ms for sent msgs: 8648
        min time in ms for msg: 136
        max time in ms for msg: 596
        avg time in ms for msg: 172
        msg send failures: 0
        time in ms for failed msgs: 0
        avg time in ms for failed msg: 0
        messages too large to sent to IoTHub: 0
        times we missed send interval: 0
        ---------------------------------
        current working set in MB: 89
        ======================================================================

This configuration batches as much OPC node value udpates as possible. The maximum IoTHub message size is 256 kB, which is configured here. There is no send interval requested, which makes the time when data is ingested
completly controlled by the data itself. This configuration has the least probability of loosing any OPC node values and could be used for publishing a high number of nodes.
When using this configuration you need to ensure, that your scenario does not have conditions where high latency is introduced (because the message size of 256 kB is not reached).

# Debugging the Application

## Native on Winodws

Open the OpcPublisher.sln project with Visual Studio 2017 and start debugging the app by hitting F5.

If you need to access the OPC UA server in the publisher, you should ensure that the firewall setting allow access to the port the server is listening on (default: 62222).

## In a docker container

Visual Studio 2017 supports debugging of application in docker container. This is done by using docker-compose. Since this does not allow to pass command line parameters it is not convenient. 
Another debugging option VS2017 supports is to debug via ssh. In the root of the repository the docker build configuration file `Dockerfile.ssh` can be used to create a SSH enabled container by:

    docker build -f .\Dockerfile.ssh -t publisherssh .

The container can now be started for publisher debugging purposes with:

    docker run -it publisherssh

In the container you need to manually start the ssh daemon with:

    service ssh start

At this point you should be able to create an ssh session as user `root` with the password `Passw0rd`.

To prepare debugging of the application in the container you need to do the following additional steps:

On the host side create a launch.json:

    {
      "version": "0.2.0",
      "adapter": "<path>\\plink.exe",
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

Use a tool like WinSCP to copy over the published files to the container into the directory `/root/publisher` (this can be also a different directory, but needs to be in sync with the `cdw` property of launch.json.

Now you could start debugging with the following command in Visual Studio's Command Window (View->Other Windows->Command Window):
DebugAdapterHost.Launch /LaunchJson:"<path-to-the-launch.json-file-you-saved>"





