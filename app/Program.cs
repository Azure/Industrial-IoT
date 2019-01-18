// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Utils;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseApplicationInsights()
                .ConfigureAppConfiguration((context, config) =>
                {
                    var builtConfig = config.Build();
                    if (builtConfig["KeyVault"] != null)
                    {
                        config.AddAzureKeyVault(
                            $"{builtConfig["KeyVault"]}",
                            builtConfig["AzureAD:ClientId"],
                            builtConfig["AzureAD:ClientSecret"],
                            new PrefixKeyVaultSecretManager("App")
                            );
                    }
                })
                .UseStartup<Startup>();
    }
}
