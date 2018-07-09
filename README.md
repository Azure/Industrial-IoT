This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments

# OPC Publisher for Azure IoT Edge
This reference implementation demonstrates how Azure IoT Edge can be used to connect to existing OPC UA servers and publishes JSON encoded telemetry data from these servers in OPC UA "Pub/Sub" format (using a JSON payload) to Azure IoT Hub. All transport protocols supported by Azure IoT Edge can be used, i.e. HTTPS, AMQP and MQTT (the default).

This application, apart from including an OPC UA *client* for connecting to existing OPC UA servers you have on your network, also includes an OPC UA *server* on port 62222 that can be used to manage what gets published.

The application is implemented using .NET Core technology and is able to run on the platforms supported by .NET Core.

Publisher implements a retry logic to establish connections to endpoints which have not responded to a certain number of keep alive requests, for example if the OPC UA server on this endpoint had a power outage.

For each distinct publishing interval to an OPC UA server it creates a separate subscription over which all nodes with this publishing interval are updated.

Publisher supports batching of the data sent to IoTHub, to reduce network load. This batching is sending a packet to IoTHub only if the configured package size is reached.

This application uses the OPC Foundations's OPC UA reference stack and therefore licensing restrictions apply. Visit http://opcfoundation.github.io/UA-.NETStandard/ for OPC UA documentation and licensing terms.

|Branch|Status|
|------|-------------|
|master|[![Build status](https://ci.appveyor.com/api/projects/status/6t7ru6ow7t9uv74r/branch/master?svg=true)](https://ci.appveyor.com/project/marcschier/iot-gateway-opc-ua-r4ba5/branch/master) [![Build Status](https://travis-ci.org/Azure/iot-gateway-opc-ua.svg?branch=master)](https://travis-ci.org/Azure/iot-gateway-opc-ua)|

# Building the Application
The application requires the .NET Core SDK 2.1.

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
            // Allows to access the endpoint with SecurityPolicy.None when set to 'false' (no signing and encryption applied to the OPC UA communication), default is true
            "UseSecurity": true,
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

# Configuring the telemetry published to IoTHub
When OpcPublisher gets notified about a value change in one of the configured published nodes, it generates a JSON formatted message, which is sent to IoTHub.
The content of this JSON formatted message can be configured via a configuration file. If no configuration file is specified via the `--tc` option a default configuration is used,
which is compatible with the [Connected factory Preconfigured Solution](https://github.com/Azure/azure-iot-connected-factory).

The data which is ingested is taken from three sources:
* the OpcPublisher node configuration for the node
* the MonitoredItem object of the OPC UA stack for which OpcPublisher got a notification
* the argument passed to this notification, which provides details on the data value change

The telemetry which is put into the JSON formatted message is a selection of important properties of these objects. If you need more properties, you need to change the OpcPublisher code base.

The syntax of the configuration file is as follows:

        // The configuration settings file consists of two objects:
        // 1) The 'Defaults' object, which defines defaults for the telemetry configuration
        // 2) An array 'EndpointSpecific' of endpoint specific configuration
        // Both objects are optional and if they are not specified, then publisher uses
        // its internal default configuration, which generates telemetry messages compatible
        // with the Microsoft Connected factory Preconfigured Solution (https://github.com/Azure/azure-iot-connected-factory).

        // A JSON telemetry message for Connected factory looks like:
        //  {
        //      "NodeId": "i=2058",
        //      "ApplicationUri": "urn:myopcserver",
        //      "DisplayName": "CurrentTime",
        //      "Value": {
        //          "Value": "10.11.2017 14:03:17",
        //          "SourceTimestamp": "2017-11-10T14:03:17Z"
        //      }
        //  }

        // The 'Defaults' object in the sample below, are similar to what publisher is
        // using as its internal default telemetry configuration.
        {
            "Defaults": {
                // The first two properties ('EndpointUrl' and 'NodeId' are configuring data
                // taken from the OpcPublisher node configuration.
                "EndpointUrl": {

                    // The following three properties can be used to configure the 'EndpointUrl'
                    // property in the JSON message send by publisher to IoTHub.

                    // Publish controls if the property should be part of the JSON message at all.
                    "Publish": false,

                    // Pattern is a regular expression, which is applied to the actual value of the
                    // property (here 'EndpointUrl').
                    // If this key is ommited (which is the default), then no regex matching is done
                    // at all, which improves performance.
                    // If the key is used you need to define groups in the regular expression.
                    // Publisher applies the regular expression and then concatenates all groups
                    // found and use the resulting string as the value in the JSON message to
                    //sent to IoTHub.
                    // This example mimics the default behaviour and defines a group,
                    // which matches the conplete value:
                    "Pattern": "(.*)",
                    // Here some more exaples for 'Pattern' values and the generated result:
                    // "Pattern": "i=(.*)"
                    // defined for Defaults.NodeId.Pattern, will generate for the above sample
                    // a 'NodeId' value of '2058'to be sent by publisher
                    // "Pattern": "(i)=(.*)"
                    // defined for Defaults.NodeId.Pattern, will generate for the above sample
                    // a 'NodeId' value of 'i2058' to be sent by publisher

                    // Name allows you to use a shorter string as property name in the JSON message
                    // sent by publisher. By default the property name is unchanged and will be
                    // here 'EndpointUrl'.
                    // The 'Name' property can only be set in the 'Defaults' object to ensure
                    // all messages from publisher sent to IoTHub have a similar layout.
                    "Name": "EndpointUrl"

                },
                "NodeId": {
                    "Publish": true,

                    // If you set Defaults.NodeId.Name to "ni", then the "NodeId" key/value pair
                    // (from the above example) will change to:
                    //      "ni": "i=2058",
                    "Name": "NodeId"
                },

                // The MonitoredItem object is configuring the data taken from the MonitoredItem
                // OPC UA object for published nodes.
                "MonitoredItem": {

                    // If you set the Defaults.MonitoredItem.Flat to 'false', then a
                    // 'MonitoredItem' object will appear, which contains 'ApplicationUri'
                    // and 'DisplayNode' proerties:
                    //      "NodeId": "i=2058",
                    //      "MonitoredItem": {
                    //          "ApplicationUri": "urn:myopcserver",
                    //          "DisplayName": "CurrentTime",
                    //      }
                    // The 'Flat' property can only be used in the 'MonitoredItem' and
                    // 'Value' objects of the 'Defaults' object and will be used
                    // for all JSON messages sent by publisher.
                    "Flat": true,

                    "ApplicationUri": {
                        "Publish": true,
                        "Name": "ApplicationUri"
                    },
                    "DisplayName": {
                        "Publish": true,
                        "Name": "DisplayName"
                    }
                },
                // The Value object is configuring the properties taken from the event object
                // the OPC UA stack provided in the value change notification event.
                "Value": {
                    // If you set the Defaults.Value.Flat to 'true', then the 'Value'
                    // object will disappear completely and the 'Value' and 'SourceTimestamp'
                    // members won't be nested:
                    //      "DisplayName": "CurrentTime",
                    //      "Value": "10.11.2017 14:03:17",
                    //      "SourceTimestamp": "2017-11-10T14:03:17Z"
                    // The 'Flat' property can only be used for the 'MonitoredItem' and 'Value'
                    // objects of the 'Defaults' object and will be used for all
                    // messages sent by publisher.
                    "Flat": false,

                    "Value": {
                        "Publish": true,
                        "Name": "Value"
                    },
                    "SourceTimestamp": {
                        "Publish": true,
                        "Name": "SourceTimestamp"
                    },
                    // 'StatusCode' is the 32 bit OPC UA status code
                    "StatusCode": {
                        "Publish": false,
                        "Name": "StatusCode"
                        // 'Pattern' is ignored for the 'StatusCode' value
                    },
                    // 'Status' is the symbolic name of 'StatusCode'
                    "Status": {
                        "Publish": false,
                        "Name": "Status"
                    }
                }
            },

            // The next object allows to configure 'Publish' and 'Pattern' for specific
            // endpoint URLs. Those will overwrite the ones specified in the 'Defaults' object
            // or the defaults used by publisher.
            // It is not allowed to specify 'Name' and 'Flat' properties in this object.
            "EndpointSpecific": [
                // The following shows how a endpoint specific configuration can look like:
                {
                    // 'ForEndpointUrl' allows to configure for which OPC UA server this
                    // object applies and is a required property for all objects in the
                    // 'EndpointSpecific' array.
                    // The value of 'ForEndpointUrl' must be an 'EndpointUrl' configured in
                    // the publishednodes.json confguration file.
                    "ForEndpointUrl": "opc.tcp://<your_opcua_server>:<your_opcua_server_port>/<your_opcua_server_path>",
                    "EndpointUrl": {
                        // We overwrite the default behaviour and publish the
                        // endpoint URL in this case.
                        "Publish": true,
                        // We are only interested in the URL part following the 'opc.tcp://' prefix
                        // and define a group matching this.
                        "Pattern": "opc.tcp://(.*)"
                    },
                    "NodeId": {
                        // We are not interested in the configured 'NodeId' value, 
                        // so we do not publish it.
                        "Publish": false
                        // No 'Pattern' key is specified here, so the 'NodeId' value will be
                        // taken as specified in the publishednodes configuration file.
                    },
                    "MonitoredItem": {
                        "ApplicationUri": {
                            // We already publish the endpoint URL, so we do not want
                            // the ApplicationUri of the MonitoredItem to be published.
                            "Publish": false
                        },
                        "DisplayName": {
                            "Publish": true
                        }
                    },
                    "Value": {
                        "Value": {
                            // The value of the node is important for us, everything else we
                            // are not interested in to keep the data ingest as small as possible.
                            "Publish": true
                        },
                        "SourceTimestamp": {
                            "Publish": false
                        },
                        "StatusCode": {
                            "Publish": false
                        },
                        "Status": {
                            "Publish": false
                        }
                    }
                }
            ]
        }

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
                                    Default: '.\publishednodes.json'
           --tc, --telemetryconfigfile=VALUE
                                  the filename to configure the ingested telemetry
                                    Default: not set
       -s, --site=VALUE           the site OPC Publisher is working in. if specified
                                    this domain is appended (delimited by a ':' to
                                    the 'ApplicationURI' property when telemetry is
                                    sent to IoTHub.
                                    The value must follow the syntactical rules of a
                                    DNS hostname.
                                    Default: not set
           --ic, --iotcentral     publisher will send OPC UA data in IoTCentral
                                    compatible format (DisplayName of a node is used
                                    as key, this key is the Field name in IoTCentral)
                                    . you need to ensure that all DisplayName's are
                                    unique. (Auto enables fetch display name)
                                    Default: False
           --sw, --sessionconnectwait=VALUE
                                  specify the wait time in seconds publisher is
                                    trying to connect to disconnected endpoints and
                                    starts monitoring unmonitored items
                                    Min: 10
                                    Default: 10
           --mq, --monitoreditemqueuecapacity=VALUE
                                  specify how many notifications of monitored items
                                    can be stored in the internal queue, if the data
                                    can not be sent quick enough to IoTHub
                                    Min: 1024
                                    Default: 8192
           --di, --diagnosticsinterval=VALUE
                                  shows publisher diagnostic info at the specified
                                    interval in seconds (need log level info). 0
                                    disables diagnostic output.
                                    Default: 0
           --vc, --verboseconsole=VALUE
                                  ignored, only supported for backward comaptibility.
           --ns, --noshutdown=VALUE
                                  same as runforever.
                                    Default: False
           --rf, --runforwver     publisher can not be stopped by pressing a key on
                                    the console, but will run forever.
                                    Default: False
           --ih, --iothubprotocol=VALUE
                                  the protocol to use for communication with Azure
                                    IoTHub (allowed values: Amqp, Http1, Amqp_
                                    WebSocket_Only, Amqp_Tcp_Only, Mqtt, Mqtt_
                                    WebSocket_Only, Mqtt_Tcp_Only).
                                    Default: Mqtt_WebSocket_Only
           --ms, --iothubmessagesize=VALUE
                                  the max size of a message which can be send to
                                    IoTHub. when telemetry of this size is available
                                    it will be sent.
                                    0 will enforce immediate send when telemetry is
                                    available
                                    Min: 0
                                    Max: 262144
                                    Default: 262144
           --si, --iothubsendinterval=VALUE
                                  the interval in seconds when telemetry should be
                                    send to IoTHub. If 0, then only the
                                    iothubmessagesize parameter controls when
                                    telemetry is sent.
                                    Default: '10'
           --dc, --deviceconnectionstring=VALUE
                                  if publisher is not able to register itself with
                                    IoTHub, you can create a device with name <
                                    applicationname> manually and pass in the
                                    connectionstring of this device.
                                    Default: none
       -c, --connectionstring=VALUE
                                  the IoTHub owner connectionstring.
                                    Default: none
           --lf, --logfile=VALUE  the filename of the logfile to use.
                                    Default: './<hostname>-publisher.log'
           --ll, --loglevel=VALUE the loglevel to use (allowed: fatal, error, warn,
                                    info, debug, verbose).
                                    Default: info
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
           --ol, --opcmaxstringlen=VALUE
                                  the max length of a string opc can transmit/
                                    receive.
                                    Default: 1048576
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
                                  ignored, only supported for backward comaptibility.
           --as, --autotrustservercerts=VALUE
                                  same as autoaccept, only supported for backward
                                    cmpatibility.
                                    Default: False
           --aa, --autoaccept     the publisher trusts all servers it is
                                    establishing a connection to.
                                    Default: False
           --tm, --trustmyself=VALUE
                                  same as trustowncert.
                                    Default: True
           --to, --trustowncert   the publisher certificate is put into the trusted
                                    certificate store automatically.
                                    Default: True
           --fd, --fetchdisplayname
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
       -i, --install              register OPC Publisher with IoTHub and then exits.
                                    Default:  False
       -h, --help                 show this message and exit


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

## Using it as a module in Azure IoT Edge
[Azure IoT Edge](https://docs.microsoft.com/en-us/azure/iot-edge) Microsoft's Intelligent Edge framework and OpcPublisher is ready to be used as a module to run in IoT Edge.
We recommend to take a look on the information available on the beforementioned link and use then the information provided here.

To add OPC Publisher as module to your IoT Edge deployment, you go to the Azure portal and navigate to your IoTHub and:
* Go to IoT Edge and create or select your IoT Edge device.
* Select `Set Modules`.
* Select `Add`under `Deployment Modules`and `Add IoT Edge Module`.
* In the `Name` field, enter `iot-edge-opc-publisher`.
In the `Image URI` field, enter `microsoft/iot-edge-opc-publisher:latest`
* Paste the following into the `Container Create Options` field:

        {
          "Hostname": "publisher",
          "Cmd": [
            "publisher",
            "--pf",
            "/cfg/pn.json",
            "--aa"
          ],
          "HostConfig": {
            "PortBindings": {
              "62222/tcp": [
                {
                  "HostPort": "62222"
                }
              ]
            },
            "Binds": [
              "test_cfx509certstores:/root/.dotnet/corefx/cryptography/x509stores",
              "d:/iiotedge:/cfg"
            ],
            "ExtraHosts": [
              "localhost:127.0.0.1"
            ]
          }
        }

* Adjust the command line parameters in `Cmd` as needed
* Adjust the `Binds` source, which is set in the example to `d:/iiotedge`, which means that the configuration file is in the host folder 
  `d:\iiotedge` on the Windows host IoT Edge is running. The command line option specifies the configuration file as `\cfg\pn.json`.
  This resolves with the volume mapping to `d:\iiotedge\pn.json`. (Note: you need to apply the the appropriate Settings to allow Docker for Windows accessing the drives.)
* Leave the other settings unchanged and select `Save`.
* Back in the `Set Modules` page, select `Next`
* Add the following route in the `Specify Routes` page:

        {
           "routes":{
              "upstream":"FROM /* INTO $upstream"
           }
        }

* Select `Next`
* Update your publishednodes.json file on your system IoT Edge is running as needed.
* Select `Submit` to send your configuration down to IoT Edge



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

## Configuration of the nodes to publish
### Configuration via configuration file
The easiest way to configure the OPC UA nodes to publish is via configuration file. The configuration file format is documented in `publishednodes.json` in this repository.
Configuration file syntax has changed over time and OPC Publisher still can read old formats, but converts them into the latest format when persisting the configuration.
An example for the format of the configuration file is:
        [
          {
            "EndpointUrl": "opc.tcp://testserver:62541/Quickstarts/ReferenceServer",
            "UseSecurity": false,
            "OpcNodes": [
              {
                "Id": "i=2258",
                "OpcSamplingInterval": 2000,
                "OpcPublishingInterval": 5000
              }
            ]
          }
        ]

### Configuration via OPC UA method calls
OPC Publisher has an OPC UA Server integrated, which can be accessed on port 62222. If the hostname is `publisher`, then the URI of the endpoint is: `opc.tcp://publisher:62222/UA/Publisher`
This endpoint exposes three methods:
  - PublishNode
  - UnpublishNode
  - GetPublishedNodes

### Configuration via IoTHub direct function calls
OPC Publisher implements the following IoTHub direct method calls, which can be called when OPC Publisher runs standalone or in IoTEdge:
  - PublishNodes
  - UnpublishNodes
  - UnpublishAllNodes
  - GetConfiguredEndpoints
  - GetConfiguredNodesOnEndpoint

The format of the JSON payload of the method request and responses are defined in the file HubMethodModels.cs.


## Performance and memory considerations
### Commandline parameters contolling performance and memory
When running Publisher you need to be aware of your performance requirements and the memory resources you have available on your platform.
Since both are interdependent and both depend on the configuration of how many nodes are configured to publish, you should ensure that the parameters you are using for:
* IoTHub send interval (`--si`)
* IoTHub message size (`--ms`)
* Monitored Items queue capacity (`--mq`)
do meet your requirements.

The `--mq` parameter controls the upper bound of the capacity of the internal queue, which buffers all notifications if a value of an OPC node changes. If Publisher is not able to send messages to IoTHub fast enough,
then this queue buffers those notifications. The parameter sets the number of notifications which can be buffered. If you seen the number of items in this queue increasing in your test runs, you need to:
* decrease the IoTHub send interval (`--si`)
* increase the IoTHub message size (`--ms`)
otherwise you will loose the data values of those OPC node changes. The `--mq` parameter at the same time allows to prevent controlling the upper bound of the memory resources used by Publisher.

The `--si` parameter enforces Publisher to send messages to IoTHub as the specified interval. If there is an IoTHub message size specified via the `--ms` parameter (or by the default value for it),
then a message will be sent either when the message size is reached (in this case the interval is restarted) or when the specified interval time has passed. If you disable the message size by `--ms 0`, Publisher
uses the maximal possible IoTHub message size of 256 kB to batch data.

The `--ms` parameter allows you to enable batching of messages sent to IoTHub. Depending on the protocol you are using, the overhead to send a message to IoTHub is high compared to the actual time of sending the payload.
If your scenario allows latency for the data ingested, you should configure Publisher to use the maximal message size of 256 kB.

Before you use Publisher in production scenarios, you need to test the performance and memory under production conditions. You can use the `--di` commandline parameter to specify a interval in seconds,
which will trigger the output of diagnostic information at this interval.

### Test measurements
Here are some measurements with different values for `--si` and `--ms` parameters publishing 500 nodes with an OPC publishing interval of 1 second.
Publisher was used as debug build on Windows 10 natively for 120 seconds. The IoTHub protocol was the default MQTT protocol.

#### Default configuration (--si 10 --ms 262144)
 
        ==========================================================================
        OpcPublisher status @ 26.10.2017 15:33:05 (started @ 26.10.2017 15:31:09)
        ---------------------------------
        OPC sessions: 1
        connected OPC sessions: 1
        connected OPC subscriptions: 5
        OPC monitored items: 500
        ---------------------------------
        monitored items queue bounded capacity: 8192
        monitored items queue current items: 0
        monitored item notifications enqueued: 54363
        monitored item notifications enqueue failure: 0
        monitored item notifications dequeued: 54363
        ---------------------------------
        messages sent to IoTHub: 109
        last successful msg sent @: 26.10.2017 15:33:04
        bytes sent to IoTHub: 12709429
        avg msg size: 116600
        msg send failures: 0
        messages too large to sent to IoTHub: 0
        times we missed send interval: 0
        ---------------------------------
        current working set in MB: 90
        --si setting: 10
        --ms setting: 262144
        --ih setting: Mqtt
        ==========================================================================

The default configuration sends data to IoTHub each 10 seconds or when 256 kB of data to ingest is available. This adds a moderate latency of max 10 seconds, but has lowest probablilty of loosing data because of the large message size.
As you see in the diagnostics ouptut there are no OPC node udpates lost (`monitored item notifications enqueue failure`).

#### Constant send inverval (--si 1 --ms 0)

        ==========================================================================
        OpcPublisher status @ 26.10.2017 15:35:59 (started @ 26.10.2017 15:34:03)
        ---------------------------------
        OPC sessions: 1
        connected OPC sessions: 1
        connected OPC subscriptions: 5
        OPC monitored items: 500
        ---------------------------------
        monitored items queue bounded capacity: 8192
        monitored items queue current items: 0
        monitored item notifications enqueued: 54243
        monitored item notifications enqueue failure: 0
        monitored item notifications dequeued: 54243
        ---------------------------------
        messages sent to IoTHub: 109
        last successful msg sent @: 26.10.2017 15:35:59
        bytes sent to IoTHub: 12683836
        avg msg size: 116365
        msg send failures: 0
        messages too large to sent to IoTHub: 0
        times we missed send interval: 0
        ---------------------------------
        current working set in MB: 90
        --si setting: 1
        --ms setting: 0
        --ih setting: Mqtt
        ==========================================================================

When the message size is set to 0 and there is a send interval configured (or the default of 1 second is used), then Publisher does use internally batch data using the maximal supported IoTHub message size, which is 256 kB. As you see in the diagnostic output,
the average message size is 115019 byte. In this configuration we do not loose any OPC node value udpates and compared to the default it adds lower latency.

#### Send each OPC node value update (--si 0 --ms 0)

        ==========================================================================
        OpcPublisher status @ 26.10.2017 15:39:33 (started @ 26.10.2017 15:37:37)
        ---------------------------------
        OPC sessions: 1
        connected OPC sessions: 1
        connected OPC subscriptions: 5
        OPC monitored items: 500
        ---------------------------------
        monitored items queue bounded capacity: 8192
        monitored items queue current items: 8184
        monitored item notifications enqueued: 54232
        monitored item notifications enqueue failure: 44624
        monitored item notifications dequeued: 1424
        ---------------------------------
        messages sent to IoTHub: 1423
        last successful msg sent @: 26.10.2017 15:39:33
        bytes sent to IoTHub: 333046
        avg msg size: 234
        msg send failures: 0
        messages too large to sent to IoTHub: 0
        times we missed send interval: 0
        ---------------------------------
        current working set in MB: 96
        --si setting: 0
        --ms setting: 0
        --ih setting: Mqtt
        ==========================================================================

This configuration sends for each OPC node value change a message to IoTHub. You see the average message size of 234 byte is pretty small. The advantage of this configuration is that Publisher does not add any latency to the ingest data path. The number of
lost OPC node value updates (`monitored item notifications enqueue failure: 44624`) is the highest of all compared configurations, which make this configuration not recommendable for use cases, when a lot of telemetry should be published.

#### Maximum batching (--si 0 --ms 262144)

        ==========================================================================
        OpcPublisher status @ 26.10.2017 15:42:55 (started @ 26.10.2017 15:41:00)
        ---------------------------------
        OPC sessions: 1
        connected OPC sessions: 1
        connected OPC subscriptions: 5
        OPC monitored items: 500
        ---------------------------------
        monitored items queue bounded capacity: 8192
        monitored items queue current items: 0
        monitored item notifications enqueued: 54137
        monitored item notifications enqueue failure: 0
        monitored item notifications dequeued: 54137
        ---------------------------------
        messages sent to IoTHub: 48
        last successful msg sent @: 26.10.2017 15:42:55
        bytes sent to IoTHub: 12565544
        avg msg size: 261782
        msg send failures: 0
        messages too large to sent to IoTHub: 0
        times we missed send interval: 0
        ---------------------------------
        current working set in MB: 90
        --si setting: 0
        --ms setting: 262144
        --ih setting: Mqtt
        ==========================================================================

This configuration batches as much OPC node value udpates as possible. The maximum IoTHub message size is 256 kB, which is configured here. There is no send interval requested, which makes the time when data is ingested
completely controlled by the data itself. This configuration has the least probability of loosing any OPC node values and can be used for publishing a high number of nodes.
When using this configuration you need to ensure, that your scenario does not have conditions where high latency is introduced (because the message size of 256 kB is not reached).

# Debugging the Application

## Native on Windows

Open the OpcPublisher.sln project with Visual Studio 2017 and start debugging the app by hitting F5.

If you need to access the OPC UA server in the publisher, you should ensure that the firewall setting allow access to the port the server is listening on (default: 62222).







