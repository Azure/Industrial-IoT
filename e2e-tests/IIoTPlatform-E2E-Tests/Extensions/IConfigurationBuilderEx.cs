// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Extensions {

    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Helper extension methods for IConfigurationBuilder
    /// </summary>
    public static class IConfigurationBuilderEx {

        /// <summary>
        /// Add environment variables of all profiles from launchSettings.json.
        /// </summary>
        /// <param name="configurationBuilder"></param>
        public static IConfigurationBuilder AddAllEnvVarsFromLaunchSettings(
            this IConfigurationBuilder configurationBuilder
        ) {
            const string launchSettingsPath = "Properties/launchSettings.json";
            using (var file = File.OpenText(launchSettingsPath)) {
                var reader = new JsonTextReader(file);
                var jObject = JObject.Load(reader);

                var variables = jObject
                    .GetValue("profiles")
                    //select a proper profile here
                    .SelectMany(profiles => profiles.Children())
                    .SelectMany(profile => profile.Children<JProperty>())
                    .Where(prop => prop.Name == "environmentVariables")
                    .SelectMany(prop => prop.Value.Children<JProperty>())
                    .ToList();

                var envVarList = new List<KeyValuePair<string, string>>();
                foreach (var variable in variables) {
                    envVarList.Add(new KeyValuePair<string, string>(variable.Name, variable.Value.ToString()));
                }

                configurationBuilder.AddInMemoryCollection(envVarList);
                return configurationBuilder;
            }
        }
    }
}
