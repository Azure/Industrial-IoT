# Registry Synchronization Service

[Home](readme.md)

## Overview

Namespace: Microsoft.Azure.IIoT.Services.OpcUa.Registry.Sync

This agent has the following responsibilities:

* The agent synchronizes identity tokens on modules that need to connect to a module facing cloud gateway, such as the edge orchestration manager.
* It also synchronizes the front end service url on modules for them to connect to services.
* The agent is also responsible for synchronizing activation state between supervisors and registry.   This is so that should supervisor twins get out of sync this agent can re-apply activation state.   
* It can also swap activation state between supervisors if needed.
* The agent multiplexes device method calls to device and module identities in the system.  It is used to dispatch discovery requests to all active supervisors.  It is used to run discovery on all gateways.
