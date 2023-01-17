[Home](../../readme.md)

# OPC Publisher configuration via command line options and environment variables

The following OPC Publisher configuration can be applied by Command Line Interface (CLI) options or as environment variable settings.

CamcelCase options can also typically be provided using enviornment variables. When both environment variable and CLI argument are provided, the command line option will override the environment variable.

```text

General
-------

  -h, --help                 Show help and exit.
  -f, --pf, --publishfile, --PublishedNodesFile=VALUE
                             The name of the file containing the configuration
                               of the nodes to be published as well as the
                               information to connect to the OPC UA server
                               sources.
                               This file is also used to persist changes made
                               through the control plane, e.g., through IoT Hub
                               device method calls.
                               When this file is specified, or the default file
                               is accessible by the module, OPC Publisher will
                               start in standalone mode.
                               Default: `publishednodes.json`
      --pfs, --publishfileschema, --PublishedNodesSchemaFile=VALUE
                             The validation schema filename for publish file.
                               Schema validation is disabled by default.
                               Default: `not set` (disabled)
      --rs, --runtimestatereporting, --RuntimeStateReporting[=VALUE]
                             Enable that when publisher starts or restarts it
                               reports its runtime state using a restart
                               message.
                               Default: `False` (disabled)

Messaging configuration
-----------------------

  -c, --strict, --UseStandardsCompliantEncoding[=VALUE]
                             Use strict UA compliant encodings. Default is '
                               false' for backwards (2.5.x - 2.8.x)
                               compatibility. It is recommended to run the
                               publisher in compliant mode for best
                               interoperability.
                               Default: `False`
      --mm, --messagingmode, --MessagingMode=VALUE
                             The messaging mode for messages Allowed values:
                                   `PubSub`
                                   `Samples`
                                   `FullNetworkMessages`
                                   `FullSamples`
                                   `DataSetMessages`
                                   `RawDataSets`
                               Default: `PubSub` if `-c` is specified,
                               otherwise `Samples` for backwards compatibility.
      --me, --messageencoding, --MessageEncoding=VALUE
                             The message encoding for messages Allowed values:
                                   `Uadp`
                                   `Json`
                                   `JsonReversible`
                                   `Gzip`
                                   `JsonGzip`
                                   `JsonReversibleGzip`
                               Default: `Json`.
      --bi, --batchtriggerinterval, --BatchTriggerInterval=VALUE
                             The network message publishing interval in
                               milliseconds. Determines the publishing period
                               at which point messages are emitted. When `--bs`
                               is 1 and `--bi` is set to 0 batching is disabled.

                               Default: `10000` (10 seconds).
                               Alternatively can be set using `
                               BatchTriggerInterval` environment variable in
                               the form of a time span string formatted string `
                               [d.]hh:mm:ss[.fffffff]`.
      --bs, --batchsize, --BatchSize=VALUE
                             The number of incoming OPC UA subscription
                               notifications to collect until sending a network
                               messages. When `--bs` is set to 1 and `--bi` is
                               0 batching is disabled and messages are sent as
                               soon as notifications arrive.
                               Default: `50`.
      --ms, --maxmessagesize, --iothubmessagesize, --IoTHubMaxMessageSize=VALUE
                             The maximum size of the messages to emit. In case
                               the encoder cannot encode a message because the
                               size would be exceeded, the message is dropped.
                               Otherwise the encoder will aim to chunk messages
                               if possible.
                               Default: `256k` in case of IoT Hub messages, `0`
                               otherwise.
      --npd, --maxnodesperdataset, --MaxNodesPerDataSet=VALUE
                             Maximum number of nodes within a Subscription.
                               When there are more nodes configured for a data
                               set writer, they will be added to new
                               subscriptions. This also affects metadata
                               message size.
                               Default: `1000`.
      --kfc, --keyframecount, --DefaultKeyFrameCount=VALUE
                             The default number of delta messages to send until
                               a key frame message is sent. If 0, no key frame
                               messages are sent, if 1, every message will be a
                               key frame.
                               Default: `0`.
      --msi, --metadatasendinterval, --DefaultMetaDataSendInterval=VALUE
                             Default value in milliseconds for the metadata
                               send interval which determines in which interval
                               metadata is sent.
                               Even when disabled, metadata is still sent when
                               the metadata version changes unless `--mm=*
                               Samples` is set in which case this setting is
                               ignored. Only valid for network message
                               encodings.
                               Default: `0` which means periodic sending of
                               metadata is disabled.
      --dm, --disablemetadata, --DisableDataSetMetaData[=VALUE]
                             Disables sending any metadata when metadata
                               version changes. Only valid for network message
                               encodings. If `--mm=*Samples` is specified this
                               setting is ignored.
                               Default: `False` which means sending metadata is
                               enabled.
      --ri, --enableroutinginfo, --EnableRoutingInfo[=VALUE]
                             Add the routing info to telemetry messages. The
                               name of the property is `$$RoutingInfo` and the
                               value is the `DataSetWriterGroup` for that
                               particular message.
                               When the `DataSetWriterGroup` is not configured,
                               the `$$RoutingInfo` property will not be added
                               to the message even if this argument is set.
                               Default: `False` (disabled).

Transport settings
------------------

  -b, --mqc, --mqttclientconnectionstring, --MqttClientConnectionString=VALUE
                             An mqtt client connection string to use. Use this
                               option to connect OPC Publisher to a MQTT Broker
                               or to an EdgeHub or IoT Hub MQTT endpoint.
                               To connect to an MQTT broker use the format '
                               HostName=<IPorDnsName>;Port=<Port>[;DeviceId=<
                               IoTDeviceId>]'.
                               To connect to IoT Hub or EdgeHub MQTT endpoint
                               use a regular IoT Hub connection string.
                               Ignored if `-c` option is used to set a
                               connection string.
                               Default: `not set` (disabled).
                               For more information consult https://docs.
                               microsoft.com/en-us/azure/iot-hub/iot-hub-mqtt-
                               support#using-the-mqtt-protocol-directly-as-a-
                               device) and https://learn.microsoft.com/en-us/
                               azure/iot-hub/iot-hub-mqtt-support#for-azure-iot-
                               tools) on how to retrieve the device connection
                               string or generate a SharedAccessSignature for
                               one.
      --ttt, --telemetrytopictemplate, --TelemetryTopicTemplate=output_name
                             A template that shall be used to build the topic
                               for outgoing telemetry messages. If not
                               specified IoT Hub and EdgeHub compatible topics
                               will be used. The placeholder 'device_id' can be
                               used to inject the device id and 'output_name'
                               to inject routing info into the topic template.
                               Default: `not set`.
      --ht, --ih, --iothubprotocol, --Transport=VALUE
                             Protocol to use for communication with EdgeHub.
                               Allowed values:
                                   `AmqpOverTcp`
                                   `AmqpOverWebsocket`
                                   `Amqp`
                                   `MqttOverTcp`
                                   `Tcp`
                                   `MqttOverWebsocket`
                                   `Websocket`
                                   `Mqtt`
                                   `Any`
                               Default: `Mqtt` if device or edge hub connection
                               string is provided, ignored otherwise.
      --ec, --edgehubconnectionstring, --dc, --deviceconnectionstring, --EdgeHubConnectionString=VALUE
                             A edge hub or iot hub connection string to use if
                               you run OPC Publisher outside of IoT Edge. The
                               connection string can be obtained from the IoT
                               Hub portal. Use this setting for testing only.
                               Default: `not set`.
      --BypassCertVerification=VALUE
                             Enables bypass of certificate verification for
                               upstream communication to edgeHub. This setting
                               is for debugging purposes only and should not be
                               used in production.
                               Default: `False`
      --om, --maxoutgressmessages, --MaxOutgressMessages=VALUE
                             The maximum number of messages to buffer on the
                               send path before messages are dropped.
                               Default: `4096`

Subscription settings
---------------------

      --oi, --opcsamplinginterval, --DefaultSamplingInterval=VALUE
                             Default value in milliseconds to request the
                               servers to sample values. This value is used if
                               an explicit sampling interval for a node was not
                               configured.
                               Default: `1000`.
                               Alternatively can be set using `
                               DefaultSamplingInterval` environment variable in
                               the form of a time span string formatted string `
                               [d.]hh:mm:ss[.fffffff]`.
      --op, --opcpublishinginterval, --DefaultPublishingInterval=VALUE
                             Default value in milliseconds for the publishing
                               interval setting of a subscription created with
                               an OPC UA server. This value is used if an
                               explicit publishing interval was not configured.
                               Default: `1000`.
                               Alternatively can be set using `
                               DefaultPublishingInterval` environment variable
                               in the form of a time span string formatted
                               string `[d.]hh:mm:ss[.fffffff]`.
      --ki, --keepaliveinterval, --KeepAliveInterval=VALUE
                             The interval in seconds the publisher is sending
                               keep alive messages to the OPC servers on the
                               endpoints it is connected to.
                               Default: `10000` (10 seconds).
      --kt, --keepalivethreshold, --MaxKeepAliveCount=VALUE
                             Specify the number of keep alive packets a server
                               can miss, before the session is disconneced.
                               Default: `50`.
      --fd, --fetchdisplayname, --FetchOpcNodeDisplayName[=VALUE]
                             Fetches the displayname for the monitored items
                               subscribed if a display name was not specified
                               in the configuration.
                               Note: This has high impact on OPC Publisher
                               startup performance.
                               Default: `False` (disabled).
      --qs, --queuesize, --DefaultQueueSize=VALUE
                             Default queue size for all monitored items if
                               queue size was not specified in the
                               configuration.
                               Default: `1` (for backwards compatibility).
      --ndo, --nodiscardold, --DiscardNew[=VALUE]
                             The publisher is using this as default value for
                               the discard old setting of monitored item queue
                               configuration. Setting to true will ensure that
                               new values are dropped before older ones are
                               drained.
                               Default: `False` (which is the OPC UA default).
      --mc, --monitoreditemdatachangetrigger, --DefaulDataChangeTrigger=VALUE
                             Default data change trigger for all monitored
                               items configured in the published nodes
                               configuration unless explicitly overridden.
                               Allowed values:
                                   `Status`
                                   `StatusValue`
                                   `StatusValueTimestamp`
                               Default: `StatusValue` (which is the OPC UA
                               default).
      --sf, --skipfirst, --DefaultSkipFirst[=VALUE]
                             The publisher is using this as default value for
                               the skip first setting of nodes configured
                               without a skip first setting. A value of True
                               will skip sending the first notification
                               received when the monitored item is added to the
                               subscription.
                               Default: `False` (disabled).
      --hb, --heartbeatinterval, --DefaultHeartbeatInterval=VALUE
                             The publisher is using this as default value in
                               seconds for the heartbeat interval setting of
                               nodes that were configured without a heartbeat
                               interval setting. A heartbeat is sent at this
                               interval if no value has been received.
                               Default: `0` (disabled)
                               Alternatively can be set using `
                               DefaultHeartbeatInterval` environment variable
                               in the form of a time span string formatted
                               string `[d.]hh:mm:ss[.fffffff]`.

OPC UA Client configuration
---------------------------

      --aa, --autoaccept, --AutoAcceptUntrustedCertificates[=VALUE]
                             The publisher trusts all servers it is
                               establishing a connection to. WARNING: This
                               setting should never be used in production
                               environments!
      --ot, --operationtimeout, --OperationTimeout=VALUE
                             The operation service call timeout of the
                               publisher OPC UA client in milliseconds.
                               Default: `120000` (2 minutes).
      --ct, --createsessiontimeout, --DefaultSessionTimeout=VALUE
                             Maximum amount of time in seconds that a session
                               should remain open by the OPC server without any
                               activity (session timeout) to request from the
                               OPC server at session creation.
                               Default: `not set`.
      --slt, --MinSubscriptionLifetime=VALUE
                             Minimum subscription lifetime in seconds as per
                               OPC UA definition.
                               Default: `not set`.
      --otl, --opctokenlifetime, --SecurityTokenLifetime=VALUE
                             OPC UA Stack Transport Secure Channel - Security
                               token lifetime in milliseconds.
                               Default: `3600000` (1h).
      --ocl, --opcchannellifetime, --ChannelLifetime=VALUE
                             OPC UA Stack Transport Secure Channel - Channel
                               lifetime in milliseconds.
                               Default: `300000` (5 min).
      --omb, --opcmaxbufferlen, --MaxBufferSize=VALUE
                             OPC UA Stack Transport Secure Channel - Max buffer
                               size.
                               Default: `65535` (64KB -1).
      --oml, --opcmaxmessagelen, --MaxMessageSize=VALUE
                             OPC UA Stack Transport Secure Channel - Max
                               message size.
                               Default: `4194304` (4 MB).
      --oal, --opcmaxarraylen, --MaxArrayLength=VALUE
                             OPC UA Stack Transport Secure Channel - Max array
                               length.
                               Default: `65535` (64KB - 1).
      --ol, --opcmaxstringlen, --MaxStringLength=VALUE
                             The max length of a string opc can transmit/
                               receive over the OPC UA secure channel.
                               Default: `130816` (128KB - 256).
      --obl, --opcmaxbytestringlen, --MaxByteStringLength=VALUE
                             OPC UA Stack Transport Secure Channel - Max byte
                               string length.
                               Default: `1048576` (1MB).
      --au, --appuri, --ApplicationUri=VALUE
                             Application URI as per OPC UA definition inside
                               the OPC UA client application configuration
                               presented to the server.
                               Default: `not set`.
      --pu, --producturi, --ProductUri=VALUE
                             The Product URI as per OPC UA definition insde the
                               OPC UA client application configuration
                               presented to the server.
                               Default: `not set`.
      --rejectsha1, --RejectSha1SignedCertificates=VALUE
                             The publisher rejects deprecated SHA1 certificates.

                               Note: It is recommended to always set this value
                               to `True` if the connected OPC UA servers does
                               not use Sha1 signed certificates.
                               Default: `False` (to support older equipment).
      --mks, --minkeysize, --MinimumCertificateKeySize=VALUE
                             Minimum accepted certificate size.
                               Note: It is recommended to this value to the
                               highest certificate key size possible based on
                               the connected OPC UA servers.
                               Default: 1024.
      --tm, --trustmyself, --AddAppCertToTrustedStore=VALUE
                             Set to `False` to disable adding the publisher's
                               own certificate to the trusted store
                               automatically.
                               Default: `True`.
      --sn, --appcertsubjectname, --ApplicationCertificateSubjectName=VALUE
                             The subject name for the app cert.
                               Default: `CN=Microsoft.Azure.IIoT, C=DE, S=Bav,
                               O=Microsoft, DC=localhost`.
      --an, --appname, --ApplicationName=VALUE
                             The name for the app (used during OPC UA
                               authentication).
                               Default: `Microsoft.Azure.IIoT`
      --pki, --pkirootpath, --PkiRootPath=VALUE
                             PKI certificate store root path.
                               Default: `pki`.
      --ap, --appcertstorepath, --ApplicationCertificateStorePath=PkiRootPath
                             The path where the own application cert should be
                               stored.
                               Default: $"PkiRootPath/own".
      --apt, --at, --appcertstoretype, --ApplicationCertificateStoreType=VALUE
                             The own application cert store type. Allowed
                               values:
                                   `Directory`
                                   `X509Store`
                               Default: `Directory`.
      --tp, --trustedcertstorepath, --TrustedPeerCertificatesPath=PkiRootPath
                             The path of the trusted cert store.
                               Default: $"PkiRootPath/trusted".
      --tpt, --TrustedPeerCertificatesType=VALUE
                             Trusted peer certificate store type. Allowed
                               values:
                                   `Directory`
                                   `X509Store`
                               Default: `Directory`.
      --rp, --rejectedcertstorepath, --RejectedCertificateStorePath=PkiRootPath
                             The path of the rejected cert store.
                               Default: $"PkiRootPath/rejected".
      --rpt, --RejectedCertificateStoreType=VALUE
                             Rejected certificate store type. Allowed values:
                                   `Directory`
                                   `X509Store`
                               Default: `Directory`.
      --ip, --issuercertstorepath, --TrustedIssuerCertificatesPath=PkiRootPath
                             The path of the trusted issuer cert store.
                               Default: $"PkiRootPath/issuers".
      --tit, --TrustedIssuerCertificatesType=VALUE
                             Trusted issuer certificate store types. Allowed
                               values:
                                   `Directory`
                                   `X509Store`
                               Default: `Directory`.

Diagnostic options
------------------

      --di, --diagnosticsinterval, --DiagnosticsInterval=VALUE
                             Shows publisher diagnostic information at this
                               specified interval in seconds in the OPC
                               Publisher log (need log level info). `-1`
                               disables remote diagnostic log and diagnostic
                               output.
                               Default:60000 (60 seconds).
                               Alternatively can be set using `
                               DiagnosticsInterval` environment variable in the
                               form of a time span string formatted string `[d.]
                               hh:mm:ss[.fffffff]`".
  -l, --lf, --logfile, --LogFileName=VALUE
                             The filename of the logfile to write log output to.

                               Default: `not set` (publisher logs to the
                               console only).
      --lt, --logflushtimespan, --LogFileFlushTimeSpan=VALUE
                             The timespan in seconds when the logfile should be
                               flushed to disk.
                               Default: `not set`.
      --ll, --loglevel=VALUE The loglevel to use. Allowed values:
                                   `Verbose`
                                   `Debug`
                                   `Information`
                                   `Warning`
                                   `Error`
                                   `Fatal`
                               Default: `Information`.
      --em, --EnableMetrics=VALUE
                             Enables exporting prometheus metrics on the
                               default prometheus endpoint.
                               Default: `True` (set to `False` to disable
                               metrics exporting).
```

Currently supported combinations of `--mm` snd `--me` can be found [here](./telemetry-messages-format.md).
