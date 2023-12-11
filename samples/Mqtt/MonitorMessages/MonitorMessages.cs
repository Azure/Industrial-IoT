// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet;
using System.Text.Json;

// Connect to mqtt broker
var mqttFactory = new MqttFactory();
using var mqttClient = mqttFactory.CreateMqttClient();
var mqttClientOptions = new MqttClientOptionsBuilder()
    .WithProtocolVersion(MqttProtocolVersion.V500) // Important!!
    .WithTcpServer("localhost", 1883)
    .Build();
await mqttClient.ConnectAsync(mqttClientOptions).ConfigureAwait(false);
var indented = new JsonSerializerOptions() { WriteIndented = true };
mqttClient.ApplicationMessageReceivedAsync += args =>
{
    var json = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(
        args.ApplicationMessage.PayloadSegment), indented);
    Console.WriteLine($"{args.ApplicationMessage.Topic}:{json}");
    return Task.CompletedTask;
};
Console.WriteLine("Press key to exit");
Console.WriteLine();
const string PublisherId = "Microsoft";
await mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
    .WithTopicFilter(new MqttTopicFilterBuilder()
        .WithTopic($"{PublisherId}/#")
        .Build())
    .Build()).ConfigureAwait(false);
Console.ReadKey();
