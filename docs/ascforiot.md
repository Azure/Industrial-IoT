# Azure Security of Things for IoT and Sentinel Integration

### Pre-requites

* Log Analytics workspace
	* Information is stored by default in your Log Analytics Workspace by ASC for IoT
 * IoT Hub 
 * Supported service regions for ASC for IoT
	* Central US
	* Northern Europe
	* Southeast Asia
 * Azure Sentinel supports  workspaces created in the following regions only:
	* Southeast, Canada Central, Central India, East U.S., East U.S. 2 EUAP (Canary), Japan  East, Southeast Asia, UK South, West Europe, and West U.S. 2    	


### Steps
1. Enable ASC for IoT
* Open your IoT Hub in Azure portal
* Under the Security menu, click Overview, then click Start preview
* Choose Enable IoT Security
* Provide your Log Analytics Workspace details (workspace should NOT be the default one)
	* Elect to store raw events in addition to the default information types of storage by leaving the raw event toggle On
	* Elect to enable twin collection by leaving the twin collection toggle On
* Click Save

2. Add Azure Sentinel to Log Analytics Workspace

3. Create Alert rules


