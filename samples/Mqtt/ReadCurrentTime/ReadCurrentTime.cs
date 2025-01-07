// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using MQTTnet.Protocol;
using MQTTnet.Formatter;
using MQTTnet;
using MQTTnet.Extensions.Rpc;
using System.Text.Json;

// Connect to mqtt broker
var mqttFactory = new MqttClientFactory();
using var mqttClient = mqttFactory.CreateMqttClient();
var mqttClientOptions = new MqttClientOptionsBuilder()
    .WithProtocolVersion(MqttProtocolVersion.V500) // Important!!
    .WithTcpServer("localhost", 1883)
    .Build();
await mqttClient.ConnectAsync(mqttClientOptions).ConfigureAwait(false);
using var mqttRpcClient = mqttFactory.CreateMqttRpcClient(mqttClient,
    new MqttRpcClientOptionsBuilder().WithTopicGenerationStrategy(new PublisherTopicGenerationStrategy()).Build());

while (true)
{
    // Read current time from server - see ValueRead api in api.md
    var response = await mqttRpcClient.ExecuteAsync(TimeSpan.FromSeconds(60), "ValueRead_V2",
        JsonSerializer.SerializeToUtf8Bytes(new
        {
            connection = new
            {
                endpoint = new
                {
                    url = "opc.tcp://opcplc:50000",
                    securityMode = "SignAndEncrypt"
                }
            },
            request = new
            {
                nodeId = "i=2258"
            }
        }), MqttQualityOfServiceLevel.AtMostOnce).ConfigureAwait(false);

    var resopnseJson = JsonSerializer.Deserialize<JsonElement>(response);
    Console.WriteLine("Current time on server:" + resopnseJson.GetProperty("value").GetString());
}

public sealed class PublisherTopicGenerationStrategy : IMqttRpcClientTopicGenerationStrategy
{
    // Both are configured in with-mosquitto.yaml
    const string PublisherId = "Microsoft";
    public MqttRpcTopicPair CreateRpcTopics(TopicGenerationContext context)
    {
        // THe default method topic root is
        var methodTopicRoot = $"{PublisherId}/methods";
        return new MqttRpcTopicPair
        {
            RequestTopic = $"{methodTopicRoot}/{context.MethodName}",
            ResponseTopic = $"{methodTopicRoot}/{context.MethodName}/response"
        };
    }
}
