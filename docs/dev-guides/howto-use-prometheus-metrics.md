# How to use Prometheus metrics

[Home](readme.md)

All the cloud microservices of Industrial IoT platform have been instrumented with Prometheus already. Edge modules are instrumented with Prometheus as well.

Four types of metrics are available in Prometheus: Counter, Gauge, Summary and Histogram. Please check the official documentation on [metric types](http://prometheus.io/docs/concepts/metric_types/) and [instrumentation best practices](http://prometheus.io/docs/practices/instrumentation/#counter-vs.-gauge-vs.-summary) to learn what each is good for.

**The Metrics class is the main entry point to the API of this library.** The most common practice in C# code is to have a `static readonly` field for each metric that you wish to export from a given class.

To begin using it, please have a look at the following examples:

## Counters

Counters only increase in value and reset to zero when the process restarts.

```csharp
private static readonly Counter ProcessedJobCount = Metrics
    .CreateCounter("myapp_jobs_processed_total", "Number of processed jobs.");

...

ProcessJob();
ProcessedJobCount.Inc();
```

## Gauges

Gauges can have any numeric value and change arbitrarily.

```csharp
// Example 1

private static readonly Gauge JobsInQueue = Metrics
    .CreateGauge("myapp_jobs_queued", "Number of jobs waiting for processing in the queue.");

...

jobQueue.Enqueue(job);
JobsInQueue.Inc();

...

var job = jobQueue.Dequeue();
JobsInQueue.Dec();



// Example 2

private static readonly Gauge EndpointsAdded = Metrics
    .CreateGauge("myapp_endpoints_added", "Number of endpoints added.");

...

EndpointsAdded.Set(42);
```

### Summary

Summaries track the trends in events over time (10 minutes by default).  A summary consists of two counters, and optionally some gauges. Summary metrics are used to track the size of events, usually how long they take, via their `observe` method.

```csharp
private static readonly Summary RequestSizeSummary = Metrics
    .CreateSummary("myapp_request_size_bytes", "Summary of request sizes (in bytes) over last 10 minutes.");

...

RequestSizeSummary.Observe(request.Length);
```

 For more information, refer to the [Prometheus documentation on summaries and histograms](https://prometheus.io/docs/practices/histograms/).

## Histogram

Histograms track the size and number of events in buckets. This allows for aggregatable calculation of quantiles.

```csharp
private static readonly Histogram OrderValueHistogram = Metrics
    .CreateHistogram("myapp_order_value_usd", "Histogram of received order values (in USD).",
        new HistogramConfiguration
        {
            // We divide measurements in 10 buckets of $100 each, up to $1000.
            Buckets = Histogram.LinearBuckets(start: 100, width: 100, count: 10)
        });

...

OrderValueHistogram.Observe(order.TotalValueUsd);
```

## Track operation duration

Timers can be used to report the duration of an operation (in seconds) to a Summary, Histogram, Gauge or Counter. Wrap the operation you want to measure in a using block.

```csharp
private static readonly Histogram LoginDuration = Metrics
    .CreateHistogram("myapp_login_duration_seconds", "Histogram of login call processing durations.");

...

using (LoginDuration.NewTimer())
{
    IdentityManager.AuthenticateUser(Request.Credentials);
}
```

Official documentation for the Prometheus .NET Client is available [here](https://github.com/prometheus-net/prometheus-net/blob/master/README.md).

***Please Note***:

Specific to Kubernetes deployment:

To mark the metrics in the cloud microservice to be pulled by the Log Analytics agent in Kubernetes, please add the following annotation in the deployment file as shown below:

```yaml
annotations:  
        prometheus.io/scrape: 'true'  
        prometheus.io/port: [port]
```

Other Prometheus specific parameters which could be used:

```yaml
When monitor_kubernetes_pods = true, replicaset will scrape Kubernetes pods for the following prometheus annotations:
  - prometheus.io/scrape: Enable scraping for this pod
  - prometheus.io/scheme: If the metrics endpoint is secured then you will need to set this to `https` & most likely set the tls config.
  - prometheus.io/path: If the metrics path is not /metrics, define it with this annotation.
  - prometheus.io/port: If port is not 9102 use this annotation
```

## Next steps

* [Learn about edge diagnostics using prometheus](../modules/metricscollector.md)
* [Learn how to install iot edge](../deploy/howto-install-iot-edge.md)