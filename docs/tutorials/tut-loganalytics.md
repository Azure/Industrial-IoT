# How to view metrics in Log Analytics Workspace

[Home](readme.md)

Azure Industrial IoT Platform is instrumented with Prometheus metrics for Edge modules and Microservices hosted in AKS cluster.

To learn more about how to use Prometheus metrics in the code, please refer [here](../dev-guides/howto-use-prometheus-metrics.md).

This document describes how you could view your metrics in Log Analytics Workspace in the resource group on Azure portal.

### View metrics in Log Analytics Workspace

1. Go to **Log Analytics workspace** resource in your resource group.
   ![log analytics](../media/loganalytics1.png)
   
   
   
2. Go to **Logs** under **General** section. 

   a. To check the metrics from microservices hosted in AKS cluster. Click on **InsightsMetrics** eye button under **ContainerInsights** table.

   ![metrics](../media/loganalytics2.png)

   

   b. To check the metrics from Edge modules, click on **promMetrics_CL** eye button under **Custom Logs** table.
   ![metrics](../media/loganalytics3.png)

   

3. Next, click on **See in query editor**.

   ![metrics](../media/loganalytics4.png)

   

4. Select Time Range and click Run.

![run](../media/loganalytics5.png)



5. Log Analytics Workspace and Application Insights queries are based on Kusto Query Language(KQL). Search for the specific log traces by writing your log queries. Learn more about writing queries [here](<https://docs.microsoft.com/en-us/azure/azure-monitor/log-query/log-query-overview>).

![run](../media/loganalytics6.png)



6. It is possible to filter on the results of the query on any column.

![filter](../media/loganalytics7.png)



7. You could also view the query results in chart format where you could choose from different types of charts in addition to choosing x-axis, y-axis, etc.

![chart](../media/loganalytics8.png)


### Learn More

- If you want to create alerts based on metric values or log search entries, please refer [here](https://docs.microsoft.com/en-us/azure/azure-monitor/platform/alerts-overview?toc=%2Fazure%2Fazure-monitor%2Ftoc.json).
- Official documentation of logs in Azure Monitor is [here](https://docs.microsoft.com/en-us/azure/azure-monitor/platform/data-platform-logs).