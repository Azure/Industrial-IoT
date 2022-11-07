# IoT Edge Deployment Manifest

[Home](howto-install-iot-edge.md)

An example manifest for the released Industrial-IoT IoT Edge modules included in this repository and required by the platform.  It deploys [Discovery](../modules/discovery.md) module, [OPC Publisher](../modules/publisher.md) and [OPC Twin](../modules/twin.md) to a [Linux](#Linux) or [Windows](#Windows) IoT Edge gateway:

## Linux

```json
{
  "modulesContent": {
    "$edgeAgent": {
      "properties.desired": {
        "schemaVersion": "1.1",
        "runtime": {
          "type": "docker",
          "settings": {
            "minDockerVersion": "v1.25",
            "loggingOptions": "",
            "registryCredentials": {}
          }
        },
        "systemModules": {
          "edgeAgent": {
            "type": "docker",
            "settings": {
              "image": "mcr.microsoft.com/azureiotedge-agent:1.4",
              "createOptions": ""
            }
          },
          "edgeHub": {
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "settings": {
              "image": "mcr.microsoft.com/azureiotedge-hub:1.4",
              "createOptions": "{\"HostConfig\":{\"PortBindings\":{\"5671/tcp\":[{\"HostPort\":\"5671\"}], \"8883/tcp\":[{\"HostPort\":\"8883\"}],\"443/tcp\":[{\"HostPort\":\"443\"}]}}}"
            },
            "env": {
              "SslProtocols": {
                "value": "tls1.2"
              }
            }
          }
        },
        "modules": {
          "discovery": {
            "version": "1.0",
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "settings": {
              "image": "mcr.microsoft.com/iotedge/discovery:latest",
              "createOptions": "{\"Hostname\":\"discovery\",\"NetworkingConfig\":{\"EndpointsConfig\":{\"host\":{}}},\"HostConfig\":{\"NetworkMode\":\"host\",\"CapAdd\":[\"NET_ADMIN\"],\"CapDrop\":[\"CHOWN\",\"SETUID\"]}}"
            }
          },
          "twin": {
            "version": "1.0",
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "settings": {
              "image": "mcr.microsoft.com/iotedge/opc-twin:latest",
              "createOptions": "{\"Hostname\":\"twin\",\"HostConfig\":{\"CapDrop\":[\"CHOWN\",\"SETUID\"]}}"
            }
          },
          "publisher": {
            "version": "1.0",
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "settings": {
              "image": "mcr.microsoft.com/iotedge/opc-publisher:latest",
              "createOptions": "{\"Hostname\":\"publisher\",\"HostConfig\":{\"CapDrop\":[\"CHOWN\",\"SETUID\"]}}"
            }
          }
        }
      }
    },
    "$edgeHub": {
      "properties.desired": {
        "schemaVersion": "1.0",
        "routes": {
          "twinToUpstream": "FROM /messages/modules/twin/* INTO $upstream",
          "discoveryToUpstream": "FROM /messages/modules/discovery/* INTO $upstream",
          "publisherToUpstream": "FROM /messages/modules/publisher/* INTO $upstream",
          "leafToUpstream": "FROM /messages/* WHERE NOT IS_DEFINED($connectionModuleId) INTO $upstream"
        },
        "storeAndForwardConfiguration": {
          "timeToLiveSecs": 7200
        }
      }
    }
  }
}
```

## Windows

```json
{
  "modulesContent": {
    "$edgeAgent": {
      "properties.desired": {
        "schemaVersion": "1.1",
        "runtime": {
          "type": "docker",
          "settings": {
            "minDockerVersion": "v1.25",
            "loggingOptions": "",
            "registryCredentials": {}
          }
        },
        "systemModules": {
          "edgeAgent": {
            "type": "docker",
            "settings": {
              "image": "mcr.microsoft.com/azureiotedge-agent:1.4",
              "createOptions": ""
            }
          },
          "edgeHub": {
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "settings": {
              "image": "mcr.microsoft.com/azureiotedge-hub:1.4",
              "createOptions": "{\"HostConfig\":{\"PortBindings\":{\"5671/tcp\":[{\"HostPort\":\"5671\"}], \"8883/tcp\":[{\"HostPort\":\"8883\"}],\"443/tcp\":[{\"HostPort\":\"443\"}]}}}"
            },
            "env": {
              "SslProtocols": {
                "value": "tls1.2"
              }
            }
          }
        },
        "modules": {
          "discovery": {
            "version": "1.0",
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "settings": {
              "image": "mcr.microsoft.com/iotedge/discovery:latest",
              "createOptions":"{\"Hostname\":\"discovery\",\"HostConfig\":{\"CapDrop\":[\"CHOWN\",\"SETUID\"]}}"
            }
          },
          "twin": {
            "version": "1.0",
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "settings": {
              "image": "mcr.microsoft.com/iotedge/opc-twin:latest",
              "createOptions": "{\"Hostname\":\"twin\",\"HostConfig\":{\"CapDrop\":[\"CHOWN\",\"SETUID\"]}}"
            }
          },
          "publisher": {
            "version": "1.0",
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "settings": {
              "image": "mcr.microsoft.com/iotedge/opc-publisher:latest",
              "createOptions": "{\"Hostname\":\"publisher\",\"HostConfig\":{\"CapDrop\":[\"CHOWN\",\"SETUID\"]}}"
            }
          }
        }
      }
    },
    "$edgeHub": {
      "properties.desired": {
        "schemaVersion": "1.0",
        "routes": {
          "twinToUpstream": "FROM /messages/modules/twin/* INTO $upstream",
          "discoveryToUpstream": "FROM /messages/modules/discovery/* INTO $upstream",
          "publisherToUpstream": "FROM /messages/modules/publisher/* INTO $upstream",
          "leafToUpstream": "FROM /messages/* WHERE NOT IS_DEFINED($connectionModuleId) INTO $upstream"
        },
        "storeAndForwardConfiguration": {
          "timeToLiveSecs": 7200
        }
      }
    }
  }
}
```

## Next steps

- [Deploy this manifest using az](howto-deploy-modules-az.md)
- [Deploy and monitor Edge modules at scale](https://docs.microsoft.com/azure/iot-edge/how-to-deploy-monitor)
- [Learn more about Azure IoT Edge for Visual Studio Code](https://github.com/microsoft/vscode-azure-iot-edge)
