# How to use Application Insights metrics

[Home](readme.md)

All the cloud microservices of Industrial IoT platform have been instrumented with Application Insights already. The instrumentation monitors your app and sends telemetry data to Application Insights.

To send custom metrics from your microservice, first get an instance of `IMetricsLogger` by using constructor injection. A class `ApplicationInsightsMetrics` implements this interface and provides the necessary methods to create custom metrics. A singleton instance of `ApplicationInsightsMetrics` is already registered in the `DependencyInjection` container, which shares `TelemetryConfiguration` with rest of the telemetry.

Currently we abstract out and provide support to use three types of metrics : Counter, Gauge and Operation Duration.

Available methods to provide above metrics are :

- Counter - `TrackEvent(string name)`
- Gauge - `TrackValue(string name, int value)`
- Operation Duration - `TrackDuration(string watchName)` 

**The IMetricsLogger class is the main entry point.** The most common practice in C# code is to have a `private readonly IMetricsLogger _metrics` field and use it to define your metrics.

To begin using it, please have a look at the following examples:

## Counters

Each invocation of `TrackEvent` would set a value of 1.

```csharp
using Microsoft.Azure.IIoT.Diagnostics;

public class EndpointSecurityAlerter : IEndpointRegistryListener, IApplicationRegistryListener {
    private readonly IMetricsLogger _metrics;
    .
    .

    // Use constructor injection to get a IMetricsLogger instance.
    public EndpointSecurityAlerter(IIoTHubTelemetryServices client,
            IMetricsLogger metrics, ILogger logger) {
        .
        .
         _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    public Task OnApplicationUpdatedAsync() {
        .
        .
        // Call the required TrackXXX method.
        _metrics.TrackEvent("ApplicationUpdated"); //name is prefixed with "trackEvent-"
        return Task.CompletedTask;
    }
}
```

## Gauges

Gauges can have any numeric value and change arbitrarily.

```csharp
using Microsoft.Azure.IIoT.Diagnostics;

public class EndpointSecurityAlerter : IEndpointRegistryListener, IApplicationRegistryListener {
    private readonly IMetricsLogger _metrics;
    .
    .

    // Use constructor injection to get a IMetricsLogger instance.
    public EndpointSecurityAlerter(IIoTHubTelemetryServices client,
            IMetricsLogger metrics, ILogger logger) {
        .
        .
         _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    public Task OnApplicationsAddedAsync() {
        .
        .
        // Call the required TrackXXX method.
        _metrics.TrackValue("ApplicationsUpdated", 7); //name is prefixed with "trackValue-"
        return Task.CompletedTask;
    }
}
```

## Track operation duration

Timers can be used to report the duration of an operation(in milliseconds). Wrap the operation you want to measure in a using block.

```csharp
using Microsoft.Azure.IIoT.Diagnostics;

public class EndpointSecurityAlerter : IEndpointRegistryListener, IApplicationRegistryListener {
    private readonly IMetricsLogger _metrics;
    .
    .

    // Use constructor injection to get an IMetricsLogger instance.
    public EndpointSecurityAlerter(IIoTHubTelemetryServices client,
            IMetricsLogger metrics, ILogger logger) {
        .
        .
         _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    public Task OnEndpointAddedAsync() {
        using (_metrics.TrackDuration(nameof(OnEndpointAddedAsync))) { 
            // name is prefixed with "processingTime-"
            // Do your operation here
            .
            .
            return CheckEndpointInfoAsync(endpoint);
        }
    }
}
```

***Please Note***:

Not only does `ApplicationInsights` provides a way to send custom metrics, it also sends logs. To learn more about how to view logs and metrics in `ApplicationInsights`, please have a look [here](../tutorials/tut-applicationinsights.md).
