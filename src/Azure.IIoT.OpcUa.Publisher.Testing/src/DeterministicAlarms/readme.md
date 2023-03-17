# OPC UA Deterministic Alarm Replayer

## Introduction
This makes it possible to create more deterministic tests of Alarms.

This is done by using a script that defines Alarms and execute them.

## Script
The script has two sections.
* Definition of the Folders, Sources of alarms, and the Alarms themselves.
* The replay steps 
```
{
  // These folders will be children to the Server Object
  "folders": [
    {
      "name": "VendingMachines",
      "sources": [
        {
          // We only support BaseObjectState
          "objectType": "BaseObjectState",
          // The name needs to be unique
          "name": "VendingMachine1",
          "alarms": [
            {
              // We support a subset of current AlarmTypes
              "objectType": "TripAlarmType",
              // This is the name of the Alarm and will be visible in the Address Space
              "name": "VendingMachine1_DoorOpen",
              // This is the Id that will be used later in the script to identify the Alarm
              // This needs to be unique
              "id": "V1_DoorOpen"
            },
            {
              "objectType": "LimitAlarmType",
              "name": "VendingMachine1_TemperatureHigh",
              "id": "V1_TemperatureHigh"
            }
          ]
        },
        {
          "objectType": "BaseObjectState",
          "name": "VendingMachine2",
          "alarms": [
            {
              "objectType": "TripAlarmType",
              "name": "VendingMachine2_DoorOpen",
              "id": "V2_DoorOpen"
            },
            {
              "objectType": "OffNormalAlarmType",
              "name": "VendingMachine2_LightOff",
              "id": "V2_LightOff"
            }
          ]
        }
      ]
    }
  ],
  // This is the start of the replay part
  "script": {
    // This enable to have a waiting period before the replay starts
    "waitUntilStartInSeconds": 10,
    // Enable if the script should be executed in a loop
    "isScriptInRepeatingLoop": true,
    // How long the test should execute
    "runningForSeconds": 6000,
    // The steps
    // Two types of steps exist:
    // 1) Event - where a state is changed for an Alarm
    // 2) Sleep - insert a waiting time
    // For each step both of these values above can be set, but the recommendation are
    // to only set one of these in each step
    "steps": [
      {
        "event": {
          // The Alarm id that was defined above
          "alarmId": "V1_DoorOpen",
          // This is a message that will be shown
          "reason": "Door Open",
          // Severity. Can be one of following values: Min, Low, MediumLow, Medium, MediumHigh,
          // High, Max (From Opc.Ua EventSeverity enum)
          "severity": "High",
          // This is the eventId. This needs to be unique
          // This is, when executed, combined with the number of the current loop
          // Example: "V1_DoorOpen-1 (1)" is the complete EventId for the first loop
          "eventId": "V1_DoorOpen-1",
          // This is a list of State changes that should be triggered in the event
          "stateChanges": [
            {
              // Type of State Change
              // Currently only Enabled and Activated is supported
              "stateType": "Enabled",
              // True or False
              "state": true
            },
            {
              "stateType": "Activated",
              "state": true
            }
          ]
        }
      },
      {
        "sleepInSeconds": 5
      },
      {
        "event": {
          "alarmId": "V2_LightOff",
          "reason": "Light Off in machine",
          "severity": "Medium",
          "eventId": "V2_LightOff-1",
          "stateChanges": [
            {
              "stateType": "Enabled",
              "state": true
            },
            {
              "stateType": "Activated",
              "state": true
            }
          ]
        }
      },
      {
        "sleepInSeconds": 7
      },
      {
        "event": {
          "alarmId": "V1_DoorOpen",
          "reason": "Door Closed",
          "severity": "Medium",
          "eventId": "V1_DoorOpen-2",
          "stateChanges": [
            {
              "stateType": "Activated",
              "state": false
            }
          ]
        }
      },
      {
        "sleepInSeconds": 4
      },
      {
        "event": {
          "alarmId": "V1_TemperatureHigh",
          "reason": "Temperature is HIGH",
          "severity": "High",
          "eventId": "V1_TemperatureHigh-1",
          "stateChanges": [
            {
              "stateType": "Activated",
              "state": true
            },
            {
              "stateType": "Enabled",
              "state": true
            }
          ]
        }
      }
    ]
  }
}
```
