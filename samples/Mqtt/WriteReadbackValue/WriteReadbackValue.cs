// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Formatter;
using MQTTnet;
using MQTTnet.Extensions.Rpc;
using System.Text.Json;

// Connect to mqtt broker
var mqttFactory = new MqttFactory();
using var mqttClient = mqttFactory.CreateMqttClient();
var mqttClientOptions = new MqttClientOptionsBuilder()
    .WithProtocolVersion(MqttProtocolVersion.V500) // Important!!
    .WithTcpServer("localhost", 1883)
    .Build();
await mqttClient.ConnectAsync(mqttClientOptions).ConfigureAwait(false);
using var mqttRpcClient = mqttFactory.CreateMqttRpcClient(mqttClient,
    new MqttRpcClientOptionsBuilder().WithTopicGenerationStrategy(new PublisherTopicGenerationStrategy()).Build());

//
// Perform a couple write and readback operations:
//

var originalValue = await ReadSlowNumberOfUpdatesValueAsync().ConfigureAwait(false);
Console.WriteLine("Original value is: " + originalValue);

await WriteSlowNumberOfUpdatesValueAsync(33).ConfigureAwait(false);
var updatedValue = await ReadSlowNumberOfUpdatesValueAsync().ConfigureAwait(false);
Console.WriteLine("Value updated to : " + updatedValue);

await WriteSlowNumberOfUpdatesValueAsync(44).ConfigureAwait(false);
updatedValue = await ReadSlowNumberOfUpdatesValueAsync().ConfigureAwait(false);
Console.WriteLine("Value updated to : " + updatedValue);

await WriteSlowNumberOfUpdatesValueAsync(originalValue).ConfigureAwait(false);
originalValue = await ReadSlowNumberOfUpdatesValueAsync().ConfigureAwait(false);
Console.WriteLine("Now reset back to: " + originalValue);

//
// Helpers functions
//

// Read value of the slow number of updates configuration node
async ValueTask<int> ReadSlowNumberOfUpdatesValueAsync()
{
    var response = await mqttRpcClient.ExecuteAsync(TimeSpan.FromSeconds(60), "ValueRead",
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
                nodeId = "nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowNumberOfUpdates",
            }
        }), MqttQualityOfServiceLevel.AtMostOnce).ConfigureAwait(false);
    var resopnseJson = JsonSerializer.Deserialize<JsonElement>(response);
    return resopnseJson.GetProperty("value").GetInt32();
}

// Write value to the slow number of updates configuration node
async ValueTask WriteSlowNumberOfUpdatesValueAsync(int value)
{
    await mqttRpcClient.ExecuteAsync(TimeSpan.FromSeconds(60), "ValueWrite",
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
                nodeId = "nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowNumberOfUpdates",
                value
            }
        }), MqttQualityOfServiceLevel.AtMostOnce).ConfigureAwait(false);
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
