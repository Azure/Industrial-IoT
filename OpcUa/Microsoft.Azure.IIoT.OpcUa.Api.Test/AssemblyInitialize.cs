// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.IIoT.OpcUa.Api.Twin;
using WebService.Test.helpers;

namespace WebService.Test
{
    public sealed class AssemblyInitialize : IDisposable
    {
        public string WsHostname { get; }
        private readonly IWebHost host;

        static AssemblyInitialize()
        {
            Current = new AssemblyInitialize();
        }

        public static AssemblyInitialize Current { get; private set; }

        internal static void Run()
        {
        }

        private AssemblyInitialize()
        {
            this.WsHostname = WebServiceHost.GetBaseAddress();
            this.host = new WebHostBuilder()
                .UseUrls(this.WsHostname)
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();
            this.host.Start();
        }

        ~AssemblyInitialize()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            this.host?.Dispose();
        }
    }
}
