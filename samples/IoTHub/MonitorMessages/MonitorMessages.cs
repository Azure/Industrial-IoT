// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Text;
using Azure.Messaging.EventHubs.Consumer;
using CommandLine;

Parameters? parameters = null;
// Parse application parameters
var result = Parser.Default.ParseArguments<Parameters>(args)
    .WithParsed(parsedParams => parameters = parsedParams)
    .WithNotParsed(errors => Environment.Exit(1));

// Either the connection string must be supplied, or the set of endpoint, name, and shared access key must be.
if (string.IsNullOrWhiteSpace(parameters?.EventHubConnectionString)
    && (string.IsNullOrWhiteSpace(parameters?.EventHubCompatibleEndpoint)
        || string.IsNullOrWhiteSpace(parameters.EventHubName)
        || string.IsNullOrWhiteSpace(parameters.SharedAccessKey)))
{
    Console.WriteLine(CommandLine.Text.HelpText.AutoBuild(result, null, null));
    Environment.Exit(1);
}

Console.WriteLine("Read all messages from IoT Hub. Ctrl-C to exit.\n");

// Set up a way for the user to gracefully shutdown
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
    Console.WriteLine("Exiting...");
};

await ReceiveMessagesFromDeviceAsync(parameters, cts.Token).ConfigureAwait(false);

// Asynchronously create a PartitionReceiver for a partition and then start
// reading any messages sent from the simulated client.
static async Task ReceiveMessagesFromDeviceAsync(Parameters parameters, CancellationToken ct)
{
    var connectionString = parameters.GetEventHubConnectionString();

    // Create the consumer using the default consumer group using a direct connection to the service.
    // Information on using the client with a proxy can be found in the README for this quick start, here:
    // https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/main/iot-hub/Quickstarts/ReadD2cMessages/README.md#websocket-and-proxy-support
    var consumer = new EventHubConsumerClient(
            EventHubConsumerClient.DefaultConsumerGroupName,
            connectionString,
            parameters.EventHubName);
    await using (consumer.ConfigureAwait(false))
    {
        Console.WriteLine("Listening for messages on all partitions.");
        try
        {
            // Begin reading events for all partitions, starting with the first event in each partition and waiting indefinitely for
            // events to become available. Reading can be canceled by breaking out of the loop when an event is processed or by
            // signaling the cancellation token.
            //
            // The "ReadEventsAsync" method on the consumer is a good starting point for consuming events for prototypes
            // and samples. For real-world production scenarios, it is strongly recommended that you consider using the
            // "EventProcessorClient" from the "Azure.Messaging.EventHubs.Processor" package.
            //
            // More information on the "EventProcessorClient" and its benefits can be found here:
            //   https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/eventhub/Azure.Messaging.EventHubs.Processor/README.md
            await foreach (PartitionEvent partitionEvent in consumer.ReadEventsAsync(ct).ConfigureAwait(false))
            {
                Console.WriteLine($"\nMessage received on partition {partitionEvent.Partition.PartitionId}:");

                var data = Encoding.UTF8.GetString(partitionEvent.Data.Body.ToArray());
                Console.WriteLine($"\tMessage body: {data}");

                Console.WriteLine("\tApplication properties (set by device):");
                foreach (var prop in partitionEvent.Data.Properties)
                {
                    PrintProperties(prop);
                }

                Console.WriteLine("\tSystem properties (set by IoT hub):");
                foreach (var prop in partitionEvent.Data.SystemProperties)
                {
                    PrintProperties(prop);
                }
            }
        }
        catch (TaskCanceledException)
        {
            // This is expected when the token is signaled; it should not be considered an
            // error in this scenario.
        }
    }
}

static void PrintProperties(KeyValuePair<string, object> prop)
{
    var propValue = prop.Value is DateTime time
        ? time.ToString("O") // using a built-in date format here that includes milliseconds
        : prop.Value.ToString();
    Console.WriteLine($"\t\t{prop.Key}: {propValue}");
}

/// <summary>
/// Parameters for the application.
/// </summary>
internal sealed class Parameters
{
    [Option(
        'e',
        "EventHubCompatibleEndpoint",
        HelpText = "The event hub-compatible endpoint from your IoT hub instance. Use `az iot hub show --query properties.eventHubEndpoints.events.endpoint --name {your IoT hub name}` to fetch via the Azure CLI.")]
    public string? EventHubCompatibleEndpoint { get; set; }

    [Option(
        'n',
        "EventHubName",
        HelpText = "The event hub-compatible name of your IoT hub instance. Use `az iot hub show --query properties.eventHubEndpoints.events.path --name {your IoT hub name}` to fetch via the Azure CLI.")]
    public string? EventHubName { get; set; }

    [Option(
        's',
        "SharedAccessKey",
        HelpText = "A primary or shared access key from your IoT hub instance, with the 'service' permission. Use `az iot hub policy show --name service --query primaryKey --hub-name {your IoT hub name}` to fetch via the Azure CLI.")]
    public string? SharedAccessKey { get; set; }

    [Option(
        'c',
        "EventHubConnectionString",
        HelpText = "The connection string to the event hub-compatible endpoint. Use the Azure portal to get this parameter. If this value is provided, all the others are not necessary.")]
    public string? EventHubConnectionString { get; set; }

    internal string GetEventHubConnectionString()
    {
        const string iotHubSharedAccessKeyName = "service";
        return EventHubConnectionString ?? $"Endpoint={EventHubCompatibleEndpoint};SharedAccessKeyName={iotHubSharedAccessKeyName};SharedAccessKey={SharedAccessKey}";
    }
}
