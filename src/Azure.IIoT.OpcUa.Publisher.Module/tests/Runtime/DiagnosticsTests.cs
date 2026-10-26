// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using OpenTelemetry.Metrics;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public class DiagnosticsTests
    {
        [Theory]
        [InlineData("iiot_edge_publisher_is_connection_ok", MetricType.LongGauge)]
        [InlineData("iiot_edge_publisher_messages", MetricType.LongSum)]
        [InlineData("iiot_edge_publisher_send_queue_size", MetricType.LongSumNonMonotonic)]
        public async Task RemovingMeterDoesNotStopOtherWriterGroupMetricsAsync(
            string metricName, MetricType metricType)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics
                .AddMeter(Diagnostics.Namespace)
                .AddPrometheusExporter(options =>
                    options.ScrapeResponseCacheDurationMilliseconds = 0));
            await using var app = builder.Build();
            app.UseOpenTelemetryPrometheusScrapingEndpoint();
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var a = CreateWriterGroup("A");
            using var b = CreateWriterGroup("B");
            using var c = CreateWriterGroup("C");
            await CollectAsync("A", "B", "C");

            c.Dispose();
            await CollectAsync("A", "B");
            await CollectAsync("A", "B");

            using var replacement = CreateWriterGroup("C");
            await CollectAsync("A", "B", "C");
            replacement.Dispose();
            await CollectAsync("A", "B");
            await CollectAsync("A", "B");

            using var d = CreateWriterGroup("D");
            await CollectAsync("A", "B", "D");
            await CollectAsync("A", "B", "D");
            a.Dispose();
            await CollectAsync("B", "D");
            await CollectAsync("B", "D");

            Meter CreateWriterGroup(string name)
            {
                var meter = Diagnostics.NewMeter();
                Assert.Equal(Diagnostics.Namespace, meter.Name);
                Assert.Equal(Diagnostics.Version, meter.Version);
                var tag = new KeyValuePair<string, object>("writerGroupName", name);
                switch (metricType)
                {
                    case MetricType.LongGauge:
                        meter.CreateObservableGauge(metricName,
                            () => new Measurement<long>(1, tag));
                        break;
                    case MetricType.LongSum:
                        meter.CreateObservableCounter(metricName,
                            () => new Measurement<long>(1, tag));
                        break;
                    case MetricType.LongSumNonMonotonic:
                        meter.CreateObservableUpDownCounter(metricName,
                            () => new Measurement<long>(1, tag));
                        break;
                }
                return meter;
            }

            async Task CollectAsync(params string[] expected)
            {
                var response = await client.GetStringAsync(new Uri("/metrics", UriKind.Relative));
                var samples = response.Split('\n')
                    .Where(line => line.StartsWith(metricName, StringComparison.Ordinal))
                    .ToList();
                Assert.Equal(expected.Length, samples.Count);
                foreach (var name in expected)
                {
                    Assert.Single(samples, sample => sample.Contains(
                        $"writerGroupName=\"{name}\"", StringComparison.Ordinal));
                }
            }
        }
    }
}
