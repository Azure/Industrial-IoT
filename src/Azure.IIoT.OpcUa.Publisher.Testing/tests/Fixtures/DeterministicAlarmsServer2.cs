// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Fixtures
{
    using Furly.Extensions.Logging;
    using Microsoft.Extensions.Logging;
    using Opc.Ua.Server;
    using Opc.Ua.Test;
    using System.Collections.Generic;

    /// <summary>
    /// Alarms server fixture
    /// </summary>
    public class DeterministicAlarmsServer2 : BaseServerFixture
    {
        public static readonly string Config = @"
{
  ""folders"": [
    {
      ""name"": ""VendingMachines"",
      ""sources"": [
        {
          ""objectType"": ""BaseObjectState"",
          ""name"": ""VendingMachine1"",
          ""alarms"": [
            {
              ""objectType"": ""TripAlarmType"",
              ""name"": ""VendingMachine1_DoorOpen"",
              ""id"": ""V1_DoorOpen""
            }
          ]
        }
      ]
    }
  ],
  ""script"": {
    ""waitUntilStartInSeconds"": 5,
    ""isScriptInRepeatingLoop"": false,
    ""runningForSeconds"": 36000,
    ""steps"": [
      {
        ""event"": {
          ""alarmId"": ""V1_DoorOpen"",
          ""reason"": ""Door Open"",
          ""severity"": ""High"",
          ""eventId"": ""V1_DoorOpen-1"",
          ""stateChanges"": [
            {
              ""stateType"": ""Enabled"",
              ""state"": true
            },
            {
              ""stateType"": ""Activated"",
              ""state"": true
            }
          ]
        }
      },
      {
        ""sleepInSeconds"": 5
      },
      {
        ""event"": {
          ""alarmId"": ""V1_DoorOpen"",
          ""reason"": ""Door Closed"",
          ""severity"": ""Medium"",
          ""eventId"": ""V1_DoorOpen-2"",
          ""stateChanges"": [
            {
              ""stateType"": ""Enabled"",
              ""state"": false
            },
            {
              ""stateType"": ""Activated"",
              ""state"": false
            }
          ]
        }
      }
    ]
  }
}
";
        /// <inheritdoc/>
        public static IEnumerable<INodeManagerFactory> DeterministicAlarms2(
            ILoggerFactory? factory, TimeService timeservice)
        {
            var logger = (factory ?? Log.ConsoleFactory())
                .CreateLogger<DeterministicAlarms.DeterministicAlarmsServer>();
            yield return new DeterministicAlarms.DeterministicAlarmsServer(
                timeservice, Config, logger);
        }

        /// <inheritdoc/>
        public DeterministicAlarmsServer2()
            : base(DeterministicAlarms2)
        {
        }

        /// <inheritdoc/>
        private DeterministicAlarmsServer2(ILoggerFactory loggerFactory)
            : base(DeterministicAlarms2, loggerFactory)
        {
        }

        /// <inheritdoc/>
        public static DeterministicAlarmsServer2 Create(ILoggerFactory loggerFactory)
        {
            return new DeterministicAlarmsServer2(loggerFactory);
        }
    }
}
