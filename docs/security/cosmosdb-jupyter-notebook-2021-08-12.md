
# Content
This document describes the mitigation of a [security issue in the Azure CosmosDB account's Jupyter notebooks feature](https://msrc-blog.microsoft.com/2021/08/27/update-on-vulnerability-in-the-azure-cosmos-db-jupyter-notebook-feature/).

# Prerequisite
The Industrial-IoT Platform has been deployed as described in the [Industrial-IoT github repository](https://github.com/Azure/Industrial-IoT/blob/main/docs/deploy/readme.md).
The steps below refer to a new full installation of the Industrial-IoT Platform with the simulation and Engineering Tool deployed. 
If the simulation has not been deployed it is possible to use an existing OPC UA server to validate proper operation. In case the Engineering Tool has not been deployed, the OpenAPI (swagger) description of the Industrial-IoT Platform can be used to validate proper operation.

# Validate proper operation
- Open the "Engineering Tool" of the Industrial-IoT Platform of the deployment.
- Goto "Discovery" and "Turn on" "Scanning" on linuxgateway, if it is not already on.
On a new deployment this should result in:
[09/05/2021 16:11:01] linuxgateway0-lbwvpab_module_discovery: Completed.
[09/05/2021 16:11:00] linuxgateway0-lbwvpab_module_discovery: 3/3: ... 3 servers found - 2 endpoints found on opc.tcp://linuxgateway0-lbwvpab-sim0.internal.cloudapp.net:51201/...

- Goto "Browse" and "Turn on" "Activation Action" on "opc.tcp://10.1.8.5:51200/
Should result in:
opc.tcp://10.1.8.5:51200/		SignAndEncrypt		Basic256Sha256		106		Disconnected		Activated		Turn off		Published Nodes

- Select "opc.tcp://10.1.8.5:51200/" in the Engineering Tool and browse to "/Root/Objects/Server/ServerStatus" and "Turn on" publishing of "CurrentTime"
Should result in:
The value of "Current time" in the UI is changing.

- After validated the changing value, "Turn off" publishing in the Engineering Tool.

# Change the Azure Cosmos DB accounts Primary Read-write key
- Log into the Azure Portal

- Goto the "Azure Cosmos DB account" resource of the Industrial-IoT Platform

- Goto "Keys" "Read-write Keys" and "Regenerate Primary Key". Fetch the new "Primary Connection String"

- Goto the "Key Vault" of the Industrial-IoT Platform

- Goto "Secrets" and select "pcs-cosmosdb-connstring" and add a "New Version" of the secret with:
    - Upload options: Manual
    - Name: pcs-cosmosdb-connstring
    - Content type: application/json
    - Set activation date: not checked
    - Set expiration date: not checked
    - Enabled: Yes
    - Tag: 1 tag (as suggested)

- Then select the old secret and "Disable" it.

- As the next step the Industrial-IoT Platform needs to be restarted. Depending on the deployment method this can be achieve by:

    - Full deployment via deploy.sh:
        - Goto "App service" resource of the Industrial-IoT Platform
        - "Restart" the app

    - Deployment in an Azure Kubernetes Service (AKS) cluster via helm chart differentiats between two different restart mechanisms depending on the value of "loadConfFromKeyVault" in values.yaml
        - If "loadConfFromKeyVault" was set to "true", then pods running the following microservices needs to be restarted:
            - OPC Publisher Job Orchestrator Microservice
            - OPC Publisher Microservice
            - Registry Microservice
            - Onboarding Service

        - If "loadConfFromKeyVault" was set to "false" (default), then the value of "azure.cosmosDB.connectionString" parameter should be updated in values.yaml file and the deployment should be upgraded (using the helm upgrade command) with new values.yaml file. After this the microservices will be automatically restarted.

# Validate proper operation
- Open the "Engineering Tool" of the Industrial-IoT Platform of the deployment and sign in.

- Goto "Browse" and select "opc.tcp://10.1.8.5:51200/" in the "Engineering Tool" and browse to "/Root/Objects/Server/ServerStatus" and "Turn on" publishing of "CurrentTime"
Should result in:
The value of "Current time" in the UI is changing.