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

var response = await mqttRpcClient.ExecuteAsync(TimeSpan.FromSeconds(60), "GetConfiguredEndpoints",
    default, MqttQualityOfServiceLevel.AtMostOnce).ConfigureAwait(false);
try
{
    var endpoints = JsonSerializer.Deserialize<JsonElement>(response).GetProperty("endpoints").EnumerateArray();
    var indented = new JsonSerializerOptions { WriteIndented = true };
    foreach (var endpoint in endpoints)
    {
        var endpointJson = JsonSerializer.Serialize(endpoint, indented);
        Console.WriteLine(endpointJson);
        response = await mqttRpcClient.ExecuteAsync(TimeSpan.FromSeconds(60), "GetConfiguredNodesOnEndpoint",
            endpointJson, MqttQualityOfServiceLevel.AtMostOnce).ConfigureAwait(false);
        var nodesJson = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(response), indented);
        Console.WriteLine(nodesJson);
    }
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
