// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests
{
    using Autofac;
    using Furly.Azure;
    using Furly.Azure.IoT;
    using Furly.Azure.IoT.Mock.Services;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Text;

    /// <summary>
    /// Startup class for tests
    /// </summary>
    public class TestStartup : Startup
    {
        /// <summary>
        /// Create startup
        /// </summary>
        /// <param name="env"></param>
        /// <param name="configuration"></param>
        public TestStartup(IWebHostEnvironment env, IConfiguration configuration) :
            base(env, configuration)
        {
        }

        /// <inheritdoc/>
        public override void ConfigureContainer(ContainerBuilder builder)
        {
            // Override real IoT hub and edge services with the mocks.
            builder.RegisterType<IoTHubMock>()
                .AsImplementedInterfaces().SingleInstance();
            builder.Configure<IoTHubServiceOptions>(options =>
                options.ConnectionString = ConnectionString.CreateServiceConnectionString(
                    "test.test.org", "iothubowner", Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()))).ToString());

            builder.RegisterInstance(IMetricsContext.Empty)
                .AsImplementedInterfaces();

            base.ConfigureContainer(builder);

            // Re-add mock
            builder.RegisterType<IoTHubMock>()
                .AsImplementedInterfaces().SingleInstance();
            // Add publisher module
            builder.RegisterType<PublisherModule>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
        }
    }
}
