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
using System.Text;

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

// Delete old writers
var response = await mqttRpcClient.ExecuteAsync(TimeSpan.FromSeconds(60), "RemoveDataSetWriterEntry",
    JsonSerializer.Serialize(new
    {
        dataSetWriterId = "MyWriterId",
        dataSetWriterGroup = "MyWriterGroup"
    }), MqttQualityOfServiceLevel.AtMostOnce).ConfigureAwait(false);

if (response.Length > 1)
{
    var error = Encoding.UTF8.GetString(response);
    Console.WriteLine(error);
}

// For payload see PublishedNodesEntryModel format
response = await mqttRpcClient.ExecuteAsync(TimeSpan.FromSeconds(60), "CreateOrUpdateDataSetWriterEntry",
    JsonSerializer.Serialize(new
    {
        endpointUrl = "opc.tcp://opcplc:50000",
        endpointSecurityMode = "SignAndEncrypt",
        dataSetWriterId = "MyWriterId",
        dataSetWriterGroup = "MyWriterGroup",
        dataSetPublishingInterval = 2000,
        opcNodes = new[]
        {
            new
            {
                id = "i=2258",
                dataSetFieldId = "Time0",
                opcSamplingInterval = 1000
            },
        }
    }), MqttQualityOfServiceLevel.AtMostOnce).ConfigureAwait(false);
if (response.Length > 1)
{
    var error = Encoding.UTF8.GetString(response);
    Console.WriteLine(error);
}
// Update the writer and add additional nodes
for (var i = 0; i < 10; i++)
{
    response = await mqttRpcClient.ExecuteAsync(TimeSpan.FromSeconds(60), "AddOrUpdateNodes",
        JsonSerializer.Serialize(new
        {
            dataSetWriterId = "MyWriterId",
            dataSetWriterGroup = "MyWriterGroup",
            opcNodes = new[]
            {
                new
                {
                    id = "i=2258",
                    dataSetFieldId = "Time" + i,
                    opcSamplingInterval = 1000
                }
            }
        }), MqttQualityOfServiceLevel.AtMostOnce).ConfigureAwait(false);
    if (response.Length > 1)
    {
        var error = Encoding.UTF8.GetString(response);
        Console.WriteLine(error);
    }
}

// Show the nodes in the writer
response = await mqttRpcClient.ExecuteAsync(TimeSpan.FromSeconds(60), "GetNodes",
    JsonSerializer.Serialize(new
    {
        dataSetWriterGroup = "MyWriterGroup",
        dataSetWriterId = "MyWriterId"
    }), MqttQualityOfServiceLevel.AtMostOnce).ConfigureAwait(false);

try
{
    var indented = new JsonSerializerOptions { WriteIndented = true };
    var nodesJson = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(response), indented);
    Console.WriteLine(nodesJson);
}
catch
{
    var error = Encoding.UTF8.GetString(response);
    Console.WriteLine(error);
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
