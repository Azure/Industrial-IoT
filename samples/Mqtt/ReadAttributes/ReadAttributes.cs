﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using MQTTnet.Protocol;
using MQTTnet.Formatter;
using MQTTnet;
using MQTTnet.Extensions.Rpc;
using System.Text.Json;
using System.Text;

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
    // Read attributes from server - see NodeRead api in api.md
    var response = await mqttRpcClient.ExecuteAsync(TimeSpan.FromSeconds(60), "NodeRead_V2",
        JsonSerializer.SerializeToUtf8Bytes(new
        {
            connection = new
            {
                endpoint = new
                {
                    url = "opc.tcp://opcplc:50000",
                    securityMode = 2,
                    securityPolicy = "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256"
                }
            },
            request = new
            {
                attributes = new[]
                {
                    new
                    {
                        nodeId = "i=2258",
                        attribute = 3
                    },
                    new
                    {
                        nodeId = "i=2258",
                        attribute = 4
                    },
                    new
                    {
                        nodeId = "i=2258",
                        attribute = 5
                    },
                    new
                    {
                        nodeId = "i=2258",
                        attribute = 13
                    },
                    new
                    {
                        nodeId = "i=2258",
                        attribute = 14
                    },
                    new
                    {
                        nodeId = "i=2258",
                        attribute = 15
                    }
                }
            }
        }), MqttQualityOfServiceLevel.AtMostOnce).ConfigureAwait(false);

    try
    {
        var resopnseJson = JsonSerializer.Deserialize<JsonElement>(response);
        Console.WriteLine();
        foreach (var item in resopnseJson.GetProperty("results").EnumerateArray())
        {
            Console.WriteLine(item.GetProperty("value").ToString());
        }
    }
    catch
    {
        Console.WriteLine(Encoding.UTF8.GetString(response));
    }
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
