// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Autofac {
    using Autofac.Core.Registration;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Diagnostics.AppInsights.Default;
    using Serilog;
    using System;

    /// <summary>
    /// Register loggers
    /// </summary>
    public static class AppInsightsContainerBuilderEx {

        /// <summary>
        /// Register telemetry client diagnostics logger
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="config"></param>
        /// <param name="log"></param>
        /// <param name="addConsole"></param>
        /// <returns></returns>
        public static IModuleRegistrar AddDiagnostics(this ContainerBuilder builder,
            IDiagnosticsConfig config, LoggerConfiguration log = null, bool addConsole = true) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            if (config == null) {
                throw new ArgumentNullException(nameof(config));
            }
            // Register metrics logger
            builder.RegisterType<ApplicationInsightsMetrics>()
                .AsImplementedInterfaces();
            builder.RegisterType<HealthCheckRegistrar>()
                .AsImplementedInterfaces().SingleInstance();
            return builder.RegisterModule(
                new LoggerProviderModule(new ApplicationInsightsLogger(config, log, addConsole)));
        }

        /// <summary>
        /// Add dependency tracking for Application Insights. This method should be used for .NET
        /// Core applications. ASP.NET Core applicatoins should rely on AddApplicationInsightsTelemetry()
        /// extension method for IServiceCollection.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="diagnosticsConfig"></param>
        /// <param name="processIdentity"></param>
        /// <returns></returns>
        public static ContainerBuilder AddDependencyTracking(this ContainerBuilder builder,
            IDiagnosticsConfig diagnosticsConfig, IProcessIdentity processIdentity) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            if (diagnosticsConfig == null) {
                throw new ArgumentNullException(nameof(diagnosticsConfig));
            }
            if (processIdentity == null) {
                throw new ArgumentNullException(nameof(processIdentity));
            }

            var telemetryInitializer = new ApplicationInsightsTelemetryInitializer(processIdentity);

            var telemetryConfig = TelemetryConfiguration.CreateDefault();
#pragma warning disable CS0618 // Type or member is obsolete
            telemetryConfig.InstrumentationKey = diagnosticsConfig.InstrumentationKey;
#pragma warning restore CS0618 // Type or member is obsolete
            telemetryConfig.TelemetryInitializers.Add(telemetryInitializer);

            var depModule = new DependencyTrackingTelemetryModule();

            // Prevent Correlation Id to be sent to certain endpoints.
            depModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.windows.net");
            depModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.chinacloudapi.cn");
            depModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.cloudapi.de");
            depModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.usgovcloudapi.net");
            depModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("localhost");
            depModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("127.0.0.1");

            // Enable known dependency tracking, note that in future versions, we will extend this list.
            // Please check default settings in https://github.com/microsoft/ApplicationInsights-dotnet-server/blob/develop/WEB/Src/DependencyCollector/DependencyCollector/ApplicationInsights.config.install.xdt

            depModule.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
            depModule.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.EventHubs");

            // Initialize the module.
            depModule.Initialize(telemetryConfig);

            builder.RegisterInstance(depModule)
                .AsImplementedInterfaces()
                .SingleInstance();

            return builder;
        }
    }
}
