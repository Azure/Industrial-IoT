// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//

using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Utils;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
            try
            {
                var host = CreateWebHostBuilder(args).Build();
                Log.Information("Web Host Ready to run.");
                host.Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    var builtConfig = config.Build();
                    var keyVault = builtConfig["KeyVault"];
                    if (keyVault != null)
                    {
                        var prefix = new PrefixKeyVaultSecretManager("App");
                        var clientSecret = builtConfig["AzureAD:ClientSecret"];
                        var clientId = builtConfig["AzureAD:ClientId"];
                        if (String.IsNullOrWhiteSpace(clientSecret) ||
                            String.IsNullOrWhiteSpace(clientId))
                        {
                            // try managed service identity
                            var azureServiceTokenProvider = new AzureServiceTokenProvider();
                            var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                            config.AddAzureKeyVault(
                                keyVault,
                                keyVaultClient,
                                prefix
                            );
                        }
                        else
                        {
                            config.AddAzureKeyVault(
                                keyVault,
                                clientId,
                                clientSecret,
                                prefix
                            );
                        }
                    }
                })
                .UseStartup<Startup>()
                .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                        .ReadFrom.Configuration(hostingContext.Configuration)
                        .Enrich.FromLogContext()
                        .WriteTo.Console());
        }
    }
}
