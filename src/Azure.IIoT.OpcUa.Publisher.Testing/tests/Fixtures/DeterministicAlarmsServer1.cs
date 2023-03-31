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
    public class DeterministicAlarmsServer1 : BaseServerFixture
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
            },
            {
              ""objectType"": ""LimitAlarmType"",
              ""name"": ""VendingMachine1_TemperatureHigh"",
              ""id"": ""V1_TemperatureHigh""
            }
          ]
        },
        {
          ""objectType"": ""BaseObjectState"",
          ""name"": ""VendingMachine2"",
          ""alarms"": [
            {
              ""objectType"": ""TripAlarmType"",
              ""name"": ""VendingMachine2_DoorOpen"",
              ""id"": ""V2_DoorOpen""
            },
            {
              ""objectType"": ""OffNormalAlarmType"",
              ""name"": ""VendingMachine2_LightOff"",
              ""id"": ""V2_LightOff""
            }
          ]
        }
      ]
    }
  ],
  ""script"": {
    ""waitUntilStartInSeconds"": 9,
    ""isScriptInRepeatingLoop"": true,
    ""runningForSeconds"": 22,
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
          ""alarmId"": ""V2_LightOff"",
          ""reason"": ""Light Off in machine"",
          ""severity"": ""Medium"",
          ""eventId"": ""V2_LightOff-1"",
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
        ""sleepInSeconds"": 7
      },
      {
        ""event"": {
          ""alarmId"": ""V1_DoorOpen"",
          ""reason"": ""Door Closed"",
          ""severity"": ""Medium"",
          ""eventId"": ""V1_DoorOpen-2"",
          ""stateChanges"": [
            {
              ""stateType"": ""Activated"",
              ""state"": false
            }
          ]
        }
      },
      {
        ""sleepInSeconds"": 4
      },
      {
        ""event"": {
          ""alarmId"": ""V1_TemperatureHigh"",
          ""reason"": ""Temperature is HIGH"",
          ""severity"": ""High"",
          ""eventId"": ""V1_TemperatureHigh-1"",
          ""stateChanges"": [
            {
              ""stateType"": ""Activated"",
              ""state"": true
            },
            {
              ""stateType"": ""Enabled"",
              ""state"": true
            }
          ]
        }
      }
    ]
  }
}
";
        /// <inheritdoc/>
        public static IEnumerable<INodeManagerFactory> DeterministicAlarms1(
            ILoggerFactory? factory, TimeService timeservice)
        {
            var logger = (factory ?? Log.ConsoleFactory())
                .CreateLogger<DeterministicAlarms.DeterministicAlarmsServer>();
            yield return new DeterministicAlarms.DeterministicAlarmsServer(
                timeservice, Config, logger);
        }

        /// <inheritdoc/>
        public DeterministicAlarmsServer1()
            : base(DeterministicAlarms1)
        {
        }

        /// <inheritdoc/>
        private DeterministicAlarmsServer1(ILoggerFactory loggerFactory)
            : base(DeterministicAlarms1, loggerFactory)
        {
        }

        /// <inheritdoc/>
        public static DeterministicAlarmsServer1 Create(ILoggerFactory loggerFactory)
        {
            return new DeterministicAlarmsServer1(loggerFactory);
        }
    }
}
