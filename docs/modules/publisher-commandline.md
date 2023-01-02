[Home](../../readme.md)

# OPC Publisher configuration via command line options and environment variables

The following OPC Publisher configuration can be applied by Command Line Interface (CLI) options or as environment variable settings.
The `Alternative` field, where present, refers to the CLI argument applicable in **standalone mode only**.  When both environment variable and CLI argument are provided, the latest will overrule the env variable.

```text


```

Currently supported combinations of `--mm` snd `--me` are:

```text
    --mm Samples and --me Json
    --mm FullSamples and --me Json
    --mm PubSub and --me Json
    --mm FullNetworkMessages and --me Json
    --mm Samples and --me Uadp
    --mm FullSamples and --me Uadp
    --mm PubSub and --me JsonGzip
    --mm FullNetworkMessages and --me JsonGzip
    --mm PubSub and --me JsonReversible
    --mm PubSub and --me JsonReversibleGzip
    --mm FullNetworkMessages and --me JsonReversible
    --mm FullNetworkMessages and --me JsonReversibleGzip
    --mm Samples and --me JsonReversible
    --mm Samples and --me JsonReversibleGzip
    --mm FullSamples and --me JsonReversible
    --mm FullSamples and --me JsonReversibleGzip
    --mm DataSetMessages and --me Json
    --mm DataSetMessages and --me JsonGzip
    --mm DataSetMessages and --me JsonReversible
    --mm DataSetMessages and --me JsonReversibleGzip
    --mm RawDataSets and --me Json
    --mm RawDataSets and --me JsonGzip
    --mm PubSub and --me Uadp
    --mm FullNetworkMessages and --me Uadp
    --mm DataSetMessages and --me Uadp
    --mm RawDataSets and --me Uadp

```









```



            PublishedNodesFile=VALUE
                                      The file used to store the configuration of the nodes to be published
                                      along with the information to connect to the OPC UA server sources
                                      When this file is specified, or the default file is accessible by
                                      the module, OPC Publisher will start in standalone mode
                                      Alternative: --pf, --publishfile
                                      Mode: Standalone only
                                      Type: string - file name, optionally prefixed with the path
                                      Default: publishednodes.json

            site=VALUE
                                      The site OPC Publisher is assigned to
                                      Alternative: --s, --site
                                      Mode: Standalone and Orchestrated
                                      Type: string
                                      Default: <not set>

            LogFileName==VALUE
                                      The filename of the logfile to use
                                      Alternative: --lf, --logfile
                                      Mode: Standalone only
                                      Type: string - file name, optionally prefixed with the path
                                      Default: <not set>

            LogFileFlushTimeSpan=VALUE
                                      The time span in seconds when the logfile should be flushed in the storage
                                      Alternative: --lt, --logflushtimespan
                                      Mode: Standalone only
                                      Environment variable type: time span string {[d.]hh:mm:ss[.fffffff]}
                                      Alternative argument type: integer in seconds
                                      Default: {00:00:30}

            loglevel=Value
                                      The level for logs to pe persisted in the logfile
                                      Alternative: --ll --loglevel
                                      Mode: Standalone only
                                      Type: string enum - Fatal, Error, Warning, Information, Debug, Verbose
                                      Default: info

            EdgeHubConnectionString=VALUE
                                      An IoT Edge Device or IoT Edge module connection string to use,
                                      when deployed as module in IoT Edge, the environment variable
                                      is already set as part of the container deployment
                                      Alternative: --dc, --deviceconnectionstring
                                                   --ec, --edgehubconnectionstring
                                      Mode: Standalone and Orchestrated
                                      Type: connection string
                                      Default: <not set> <set by iotedge runtime>

            Transport=VALUE
                                      Protocol to use for upstream communication to edgeHub or IoTHub
                                      Alternative: --ih, --iothubprotocol
                                      Mode: Standalone and Orchestrated
                                      Type: string enum: Any, Amqp, Mqtt, AmqpOverTcp, AmqpOverWebsocket,
                                        MqttOverTcp, MqttOverWebsocket, Tcp, Websocket.
                                      Default: MqttOverTcp

            BypassCertVerification=VALUE
                                      Enables/disables bypass of certificate verification for upstream communication to edgeHub
                                      Alternative: N/A
                                      Mode: Standalone and Orchestrated
                                      Type: boolean
                                      Default: false

            EnableMetrics=VALUE
                                      Enables/disables upstream metrics propagation
                                      Alternative: N/A
                                      Mode: Standalone and Orchestrated
                                      Type: boolean
                                      Default: true

            DefaultPublishingInterval=VALUE
                                      Default value for the OPC UA publishing interval of OPC UA subscriptions
                                      created to an OPC UA server. This value is used when no explicit setting
                                      is configured.
                                      Alternative: --op, --opcpublishinginterval
                                      Mode: Standalone only
                                      Environment variable type: time span string {[d.]hh:mm:ss[.fffffff]}
                                      Alternative argument type: integer in milliseconds
                                      Default: {00:00:01} (1000)

            DefaultSamplingInterval=VALUE
                                      Default value for the OPC UA sampling interval of nodes to publish.
                                      This value is used when no explicit setting is configured.
                                      Alternative: --oi, --opcsamplinginterval
                                      Mode: Standalone only
                                      Environment variable type: time span string {[d.]hh:mm:ss[.fffffff]}
                                      Alternative argument type: integer in milliseconds
                                      Default: {00:00:01} (1000)

            DefaultQueueSize=VALUE
                                      Default setting value for the monitored item's queue size to be used when
                                      not explicitly specified in pn.json file
                                      Alternative: --mq, --monitoreditemqueuecapacity
                                      Mode: Standalone only
                                      Type: integer
                                      Default: 1

            DefaultHeartbeatInterval=VALUE
                                      Default value for the heartbeat interval setting of published nodes
                                      having no explicit setting for heartbeat interval.
                                      Alternative: --hb, --heartbeatinterval
                                      Mode: Standalone
                                      Environment variable type: time span string {[d.]hh:mm:ss[.fffffff]}
                                      Alternative argument type: integer in seconds
                                      Default: {00:00:00} meaning heartbeat is disabled

            MessageEncoding=VALUE
                                      The messaging encoding for outgoing telemetry.
                                      Alternative: --me, --messageencoding
                                      Mode: Standalone only
                                      Type: string enum - Json, Uadp
                                      Default: Json

            MessagingMode=VALUE
                                      The messaging mode for outgoing telemetry.
                                      Alternative: --mm, --messagingmode
                                      Mode: Standalone only
                                      Type: string enum - PubSub, Samples
                                      Default: Samples

            FetchOpcNodeDisplayName=VALUE
                                      Fetches the DisplayName for the nodes to be published from
                                      the OPC UA Server when not explicitly set in the configuration.
                                      Note: This has high impact on OPC Publisher startup performance.
                                      Alternative: --fd, --fetchdisplayname
                                      Mode: Standalone only
                                      Type: boolean
                                      Default: false

            FullFeaturedMessage=VALUE
                                      The full featured mode for messages (all fields filled in the telemetry).
                                      Default is 'false' for legacy compatibility.
                                      Alternative: --fm, --fullfeaturedmessage
                                      Mode: Standalone only
                                      Type:boolean
                                      Default: false

            BatchSize=VALUE
                                      The number of incoming OPC UA data change messages to be cached for batching.
                                      When BatchSize is 1 or TriggerInterval is set to 0 batching is disabled.
                                      Alternative: --bs, --batchsize
                                      Mode: Standalone and Orchestrated
                                      Type: integer
                                      Default: 50

            BatchTriggerInterval=VALUE
                                      The batching trigger interval.
                                      When BatchSize is 1 or TriggerInterval is set to 0 batching is disabled.
                                      Alternative: --bi, --batchtriggerinterval <integer in milliseconds>
                                                   --si, --iothubsendinterval <integer in seconds>
                                      Mode: Standalone and Orchestrated
                                      Environment variable type: time span string {[d.]hh:mm:ss[.fffffff]}
                                      Alternative argument type: integer in milliseconds (--bi) or seconds (--si)
                                      Default: {00:00:10}

            IoTHubMaxMessageSize=VALUE
                                      The maximum size of the (IoT D2C) telemetry message.
                                      Alternative: --ms, --iothubmessagesize
                                      Mode: Standalone and Orchestrated
                                      Type: integer
                                      Default: 0

            DiagnosticsInterval=VALUE
                                      Shows publisher diagnostic info at the specified interval in seconds
                                      (need log level info). -1 disables remote diagnostic log and
                                      diagnostic output
                                      Alternative: --di, --diagnosticsinterval
                                      Mode: Standalone only
                                      Environment variable type: time span string {[d.]hh:mm:ss[.fffffff]}
                                      Alternative argument type: integer in seconds
                                      Default: {00:00:60}

            LegacyCompatibility=VALUE
                                      Forces the Publisher to operate in 2.5 legacy mode, using
                                      `"application/opcua+uajson"` for `ContentType` on the IoT Hub
                                      Telemetry message.
                                      Alternative: --lc, --legacycompatibility
                                      Mode: Standalone only
                                      Type: boolean
                                      Default: false

            PublishedNodesSchemaFile=VALUE
                                      The validation schema filename for published nodes file.
                                      Alternative: --pfs, --publishfileschema
                                      Mode: Standalone only
                                      Type: string
                                      Default: <not set>

            MaxNodesPerDataSet=VALUE
                                      Maximum number of nodes within a DataSet/Subscription.
                                      When more nodes than this value are configured for a
                                      DataSetWriter, they will be added in a separate DataSet/Subscription.
                                      Alternative: N/A
                                      Mode: Standalone only
                                      Type: integer
                                      Default: 1000

            ApplicationName=VALUE
                                      OPC UA Client Application Config - Application name as per
                                      OPC UA definition. This is used for authentication during communication
                                      init handshake and as part of own certificate validation.
                                      Alternative: --an, --appname
                                      Mode: Standalone and Orchestrated
                                      Type: string
                                      Default: "Microsoft.Azure.IIoT"

            ApplicationUri=VALUE
                                      OPC UA Client Application Config - Application URI as per
                                      OPC UA definition.
                                      Alternative: N/A
                                      Mode: Standalone and Orchestrated
                                      Type: string
                                      Default: $"urn:localhost:{ApplicationName}:microsoft:"

            ProductUri=VALUE
                                      OPC UA Client Application Config - Product URI as per
                                      OPC UA definition.
                                      Alternative: N/A
                                      Mode: Standalone and Orchestrated
                                      Type: string
                                      Default: "https://www.github.com/Azure/Industrial-IoT"

            DefaultSessionTimeout=VALUE
                                      OPC UA Client Application Config - Session timeout in seconds
                                      as per OPC UA definition.
                                      Alternative: --ct --createsessiontimeout
                                      Mode: Standalone and Orchestrated
                                      Type: integer
                                      Default: 0, meaning <not set>

            MinSubscriptionLifetime=VALUE
                                      OPC UA Client Application Config - Minimum subscription lifetime in seconds
                                      as per OPC UA definition.
                                      Alternative: N/A
                                      Mode: Standalone and Orchestrated
                                      Type: integer
                                      Default: 0, <not set>

            KeepAliveInterval=VALUE
                                      OPC UA Client Application Config - Keep alive interval in seconds
                                      as per OPC UA definition.
                                      Alternative: --ki, --keepaliveinterval
                                      Mode: Standalone and Orchestrated
                                      Type: integer milliseconds
                                      Default: 10,000 (10s)

            MaxKeepAliveCount=VALUE
                                      OPC UA Client Application Config - Maximum count of keep alive events
                                      as per OPC UA definition.
                                      Alternative: --kt, --keepalivethreshold
                                      Mode: Standalone and Orchestrated
                                      Type: integer
                                      Default: 50

            PkiRootPath=VALUE
                                      OPC UA Client Security Config - PKI certificate store root path
                                      Alternative: N/A
                                      Mode: Standalone and Orchestrated
                                      Type: string
                                      Default: "pki"

            ApplicationCertificateStorePath=VALUE
                                      OPC UA Client Security Config - application's
                                      own certificate store path
                                      Alternative: --ap, --appcertstorepath
                                      Mode: Standalone and Orchestrated
                                      Type: string
                                      Default: $"{PkiRootPath}/own"

            ApplicationCertificateStoreType=VALUE
                                      OPC UA Client Security Config - application's
                                      own certificate store type
                                      Alternative: --at, --appcertstoretype
                                      Mode: Standalone and Orchestrated
                                      Type: enum string : Directory, X509Store
                                      Default: Directory

            ApplicationCertificateSubjectName=VALUE
                                      OPC UA Client Security Config - the subject name
                                      in the application's own certificate
                                      Alternative: --sn, --appcertsubjectname
                                      Mode: Standalone and Orchestrated
                                      Type: string
                                      Default: "CN=Microsoft.Azure.IIoT, C=DE, S=Bav, O=Microsoft, DC=localhost"

            TrustedIssuerCertificatesPath=VALUE
                                      OPC UA Client Security Config - trusted certificate issuer
                                      store path
                                      Alternative: --ip, --issuercertstorepath
                                      Mode: Standalone and Orchestrated
                                      Type: string
                                      Default: $"{PkiRootPath}/issuers"

            TrustedIssuerCertificatesType=VALUE
                                      OPC UA Client Security Config - trusted issuer certificates
                                      store type
                                      Alternative: N/A
                                      Mode: Standalone and Orchestrated
                                      Type: enum string : "Directory", "X509Store"
                                      Default: "Directory"

            TrustedPeerCertificatesPath=VALUE
                                      OPC UA Client Security Config - trusted peer certificates
                                      store path
                                      Alternative: --tp, --trustedcertstorepath
                                      Mode: Standalone and Orchestrated
                                      Type: string
                                      Default: $"{PkiRootPath}/trusted"

            TrustedPeerCertificatesType=VALUE
                                      OPC UA Client Security Config - trusted peer certificates
                                      store type
                                      Alternative: N/A
                                      Mode: Standalone and Orchestrated
                                      Type: enum string : Directory, X509Store
                                      Default: Directory

            RejectedCertificateStorePath=VALUE
                                      OPC UA Client Security Config - rejected certificates
                                      store path
                                      Alternative: --rp, --rejectedcertstorepath
                                      Mode: Standalone and Orchestrated
                                      Type: string
                                      Default: $"{PkiRootPath}/rejected"

            RejectedCertificateStoreType=VALUE
                                      OPC UA Client Security Config - rejected certificates
                                      store type
                                      Alternative: N/A
                                      Mode: Standalone and Orchestrated
                                      Type: enum string : Directory, X509Store
                                      Default: Directory

            AutoAcceptUntrustedCertificates=VALUE
                                      OPC UA Client Security Config - auto accept untrusted
                                      peer certificates
                                      Alternative: --aa, --autoaccept
                                      Mode: Standalone and Orchestrated
                                      Type: boolean
                                      Default: false

            RejectSha1SignedCertificates=VALUE
                                      OPC UA Client Security Config - reject deprecated Sha1
                                      signed certificates
                                      Alternative: N/A
                                      Mode: Standalone and Orchestrated
                                      Type: boolean
                                      Default: false

            MinimumCertificateKeySize=VALUE
                                      OPC UA Client Security Config - minimum accepted
                                      certificates key size
                                      Alternative: N/A
                                      Mode: Standalone and Orchestrated
                                      Type: integer
                                      Default: 1024

            AddAppCertToTrustedStore=VALUE
                                      OPC UA Client Security Config - automatically copy own
                                      certificate's public key to the trusted certificate store
                                      Alternative: --tm, --trustmyself
                                      Mode: Standalone and Orchestrated
                                      Type: boolean
                                      Default: true
            RejectUnknownRevocationStatus=VALUE
                                      OPC UA Client Security Config - reject chain validation 
                                      with CA certs with unknown revocation status, e.g. when the
                                      CRL is not available or the OCSP provider is offline.
                                      Alternative: N/A
                                      Mode: Standalone and Orchestrated
                                      Type: boolean
                                      Default: true

            SecurityTokenLifetime=VALUE
                                      OPC UA Stack Transport Secure Channel - Security token lifetime in milliseconds
                                      Alternative: N/A
                                      Mode: Standalone and Orchestrated
                                      Type: integer (milliseconds)
                                      Default: 3,600,000 (1h)

            ChannelLifetime=VALUE
                                      OPC UA Stack Transport Secure Channel - Channel lifetime in milliseconds
                                      Alternative: N/A
                                      Mode: Standalone and Orchestrated
                                      Type: integer (milliseconds)
                                      Default: 300,000 (5 min)

            MaxBufferSize=VALUE
                                      OPC UA Stack Transport Secure Channel - Max buffer size
                                      Alternative: N/A
                                      Mode: Standalone and Orchestrated
                                      Type: integer
                                      Default: 65,535 (64KB -1)

            MaxMessageSize=VALUE
                                      OPC UA Stack Transport Secure Channel - Max message size
                                      Alternative: N/A
                                      Mode: Standalone and Orchestrated
                                      Type: integer
                                      Default: 4,194,304 (4 MB)

            MaxArrayLength=VALUE
                                      OPC UA Stack Transport Secure Channel - Max array length
                                      Alternative: N/A
                                      Mode: Standalone and Orchestrated
                                      Type: integer
                                      Default: 65,535 (64KB - 1)

            MaxByteStringLength=VALUE
                                      OPC UA Stack Transport Secure Channel - Max byte string length
                                      Alternative: N/A
                                      Mode: Standalone and Orchestrated
                                      Type: integer
                                      Default: 1,048,576 (1MB);

            OperationTimeout=VALUE
                                      OPC UA Stack Transport Secure Channel - OPC UA Service call
                                      operation timeout
                                      Alternative: --ot, --operationtimeout
                                      Mode: Standalone and Orchestrated
                                      Type: integer (milliseconds)
                                      Default: 120,000 (2 min)

            MaxStringLength=VALUE
                                      OPC UA Stack Transport Secure Channel - Maximum length of a string
                                      that can be send/received over the OPC UA Secure channel
                                      Alternative: --ol, --opcmaxstringlen
                                      Mode: Standalone and Orchestrated
                                      Type: integer
                                      Default: 130,816 (128KB - 256)

            RuntimeStateReporting=VALUE
                                      Enables reporting of OPC Publisher restarts.
                                      Alternative: --rs, --runtimestatereporting
                                      Mode: Standalone only
                                      Type: boolean
                                      Default: false

            EnableRoutingInfo=VALUE
                                      Adds the routing info to telemetry messages. The name of the property is
                                      `$$RoutingInfo` and the value is the `DataSetWriterGroup` for that particular message.
                                      When the `DataSetWriterGroup` is not configured, the `$$RoutingInfo` property will
                                      not be added to the message even if this argument is set.
                                      Alternative: --ri, --enableroutinginfo
                                      Mode: Standalone
                                      Type: boolean
                                      Default: false

            DefaultDataChangeTrigger=VALUE
                                      [Preview feature]
                                      Default data change trigger for all monitored items configured in the 
                                      published nodes configuration unless explicitly overridden.
                                      Alternative: --mc, --monitoreditemdatachangetrigger
                                      Mode: Standalone and Orchestrated
                                      Type: enum string : "Status", "StatusValue", "StatusValueTimestamp"
                                      Default: "StatusValue"

            TelemetryTopicTemplate=VALUE
                                      [Preview feature]
                                      A template that shall be used to build the topic for outgoing telemetry messages. 
                                      If no template is defined an IoT Hub compatible topic is used. 
                                      The placeholder ```{device_id}``` can be used to inject the device id into the topic.
                                      Alternative: --ttt, --telemetrytopictemplate
                                      Mode: Standalone only
                                      Type: string 
                                      Default: <not set>

            MqttClientConnectionString=VALUE
                                      Publisher connects as IoT Device to a MQTT V5 Broker or to IoT Hub MQTT broker 
                                      endpoint. Cannot be set together with --ec or --dc options.
                                      Alternative: --mqc, --mqttclientconnectionstring
                                      Mode: Standalone only
                                      Type: connection string 
                                          for MQTT Broker: `HostName=<IPorDnsName>;Port=<Port>;DeviceId=<IoTDeviceId>`
                                          for IoT Hub use a regular device connection string (*)
                                      Default: <not set>

            DefaultSkipFirst=VALUE
                                      The publisher is using this as default value for the skip first
                                      setting of nodes without a skip first setting.
                                      Alternative: --sf, --skipfirst
                                      Mode: Standalone only
                                      Type: bool
                                      Default: false
                                      
            DiscardNew=VALUE
                                      Use reversible encoding in JSON encoders.
                                      Alternative: --ndo, --nodiscardold
                                      Mode: Standalone and Orchestrated
                                      Type: boolean
                                      Default: false
                                      Mode: Standalone only
                                      Type: boolean
                                      Default: false

(*) Check [here](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-mqtt-support#using-the-mqtt-protocol-directly-as-a-device) and [here](https://learn.microsoft.com/en-us/azure/iot-hub/iot-hub-mqtt-support#for-azure-iot-tools) how to retrieve the device connection string or generate a SharedAccessSignature for one.
