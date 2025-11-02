[Serverless Orchestration Demo](README.md) \

# Monitor

The **monitor** pattern in Azure Durable is used to repeatedly check the status of an external process or system until a specific condition is met. This pattern is ideal for scenarios where you need to pool an external service or resource at regular intervals and take action based on its status.

#### How It Works

1. **Orchestrator Function**: The orchestrator function initiates the monitoring process and repeatedly calls an activity function to check the status of the external system.
2. **Activity Function**: This function performs the actual status check. It communicates with the external system or resource and returns the status to the orchestrator function.
3. **Polling**: The orchestrator function waits for a specific delay between each status check to avoid overloading the external system.
4. **Completion or Timeout**: The monitor process continues until a specific condition is met (e.g. a particular status is achieved) or a timeout occurs.

#### Example

Consider a scenario where you need to monitor the completion of a long-running task in an external system:

##### Orchestrator Function

```c#
[FunctionName("MonitorExternalTaskOrchestrator")]
public static async Task Run(
    [OrchestrationTrigger] IDurableOrchestrationContext context)
{
    string taskId = context.GetInput<string>();
    DateTime expiryTime = context.CurrentUtcDateTime.AddMinutes(30);
    bool isCompleted = false;

    while (context.CurrentUtcDateTime < expiryTime && !isCompleted)
    {
        isCompleted = await context.CallActivityAsync<bool>("CheckTaskStatus", taskId);

        if (!isCompleted)
        {
            // Wait for a specified interval before checking again
            var nextCheck = context.CurrentUtcDateTime.AddSeconds(30);
            await context.CreateTimer(nextCheck, CancellationToken.None);
        }
    }

    if (!isCompleted)
    {
        throw new TimeoutException("Monitoring operation timed out.");
    }
}
```

##### Activity Function

```C#
[FunctionName("CheckTaskStatus")]
public static async Task<bool> CheckTaskStatus([ActivityTrigger] string taskId, ILogger log)
{
    log.LogInformation($"Checking status of task {taskId}...");
    
    // Simulate status check (replace with actual status check logic)
    await Task.Delay(1000);
    bool isCompleted = new Random().Next(0, 2) == 1; // Simulate a random completion status
    
    return isCompleted;
}
```

##### Workflow

1. **Start Monitoring**: The orchestrator function is triggered with the identifier of the task to be monitored.
2. **Repeated Status Checks**: The orchestrator function repeatedly calls the `CheckTaskStatus` activity function at specific intervals (e.g. every 30 seconds) to check if task is completed.
3. **Wait and Retry**: Between each status check, the orchestrator function waits for the specific interval using the `CreateTimer` method.
4. **Completion or Timeout**: The monitoring process continues until the task is completed or the specific timeout period (e.g., 30 minutes) is reached.

#### Benefits

- **Efficiency**: By using a delay between status checks, the Monitor pattern avoid overwhelming the external system with frequent requests.
- **Reliability**: The orchestrator function ensures that the status checks are performed exactly once per iteration, even in the event of failures or restarts.
- **State Management**: The orchestrator function automatically maintains the state of the monitoring process, enabling seamless recovery from failures.

The Monitor pattern is particularly useful for scenarios like monitoring external APIs, long-running batch jobs, or other asynchronous processes.

---

## Section A: Add the Durable Function Logic

For this demonstration, we will will implement a website availability monitoring scenario where our Azure Durable Function will periodically check the status of a website or API. The function can log the availability and response time, and send alerts it the site is down or performing poorly.

First we will update our Function App to include the orchestrator, activity, and starter functions for the website availability monitoring scenario.

### Step A1: Prepare the Function App for the Monitor Demo

1. We will be using the **Azure.Data.Tables** NuGet package so that we can store telemetry data and the **Azure.Communication.Email** NuGet package in order to use Azure Communication Services to send out emails if a website is down.

   ```powershell
   Install-Package Azure.Data.Tables
   Install-Package Azure.Communication.Email
   ```

2. Add the following values to the `local.settings.json`:

   | Key                                | Value                                                        |
   | ---------------------------------- | ------------------------------------------------------------ |
   | TelemetryTableConnectionString     | UseDevelopmentStorage=true                                   |
   | TelemetryTableName                 | ServerlessOrchestrationTelemetry                             |
   | AzureCommunicationServicesEndpoint | https://acs-adf-prep-use2.unitedstates.communication.azure.com |
   | SendEmailFrom                      | DoNotReply@05627ab0-fd3a-4ce1-93a9-f8606da9ded3.azurecomm.net |
   | WebsiteMonitoringDefaultInternval  | 1                                                            |

3. Add a folder named `Monitor` to the **FunctionApp** project.

### Step A2: Add the Telemetry Table Entity Type

We will store website monitoring telemetry data in an Azure Storage Table. In order to do so, we will need a `TelemetryTableEntity` type to represent those records. In the `Monitor` folder, create a new class named `TelemetryTableEntity.cs` and replace the default code with the following:

```c#
using Azure;
using Azure.Data.Tables;

namespace FunctionApp.Monitor;

public class TelemetryTableEntity : ITableEntity
{

	public string PartitionKey { get; set; } = null!;
	public string RowKey { get; set; } = null!;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	public string Url { get; set; } = null!;
	public bool IsUp { get; set; }
	public string? ErrorMessage { get; set; }
	public string? StatusCode { get; set; }
	public long ResponseTimeMs { get; set; }
}
```

### Step A3: Implement the Check Website Status Activity Function

In the `Monitor` folder, create a new class named `CheckWebsiteStatusActivity.cs` and replace the default code with the following:

```C#
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FunctionApp.Monitor;

public static class CheckWebsiteStatusActivity
{

	private static Func<HttpClient> HttpClientFactory { get; set; } = () => new HttpClient();

	[Function(nameof(CheckWebsiteStatus))]
	public static async Task<bool> CheckWebsiteStatus([ActivityTrigger] string url, FunctionContext functionContext)
	{

		ILogger logger = functionContext.GetLogger(nameof(CheckWebsiteStatus));
		logger.LogInformation("Checking Website Status: {url}", url);

		using HttpClient httpClient = HttpClientFactory();

		Uri uri = new(url);

		bool isWebsiteUp = false;
		string? errorMessage = null;
		string? statusCode = null;
		long responseTimeMs = 0;

		try
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			HttpResponseMessage response = await httpClient.GetAsync(uri);
			stopwatch.Stop();
			isWebsiteUp = response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			logger.LogError("Error checking website {url}: {errorMessage}", url, ex.Message);
			errorMessage = ex.Message;
		}

		try
		{
			TableClient tableClient = GetTableClient();

			await tableClient.AddEntityAsync(new TelemetryTableEntity
			{
				PartitionKey = uri.Host,
				RowKey = Guid.NewGuid().ToString(),
				Url = url,
				IsUp = isWebsiteUp,
				ErrorMessage = errorMessage,
				StatusCode = statusCode,
				ResponseTimeMs = responseTimeMs
			});
		}
		catch (Exception ex)
		{
			logger.LogError("Error saving telemetry for website {url}: {errorMessage}", url, ex.Message);
		}

		return isWebsiteUp;

	}

	private static TableClient GetTableClient()
	{
		string environment = Environment.GetEnvironmentVariable("Environment") ?? "Non-Local";
		DefaultAzureCredential credential = new();
		TableServiceClient tableServiceClient;
		if (!environment.Equals("local", StringComparison.InvariantCultureIgnoreCase))
			tableServiceClient = new(new Uri(Environment.GetEnvironmentVariable("TableStorageEndpoint")!), credential);
		else
			tableServiceClient = new TableServiceClient(Environment.GetEnvironmentVariable("TelemetryTableConnectionString")!);
		tableServiceClient.CreateTableIfNotExists(Environment.GetEnvironmentVariable("TelemetryTableName")!); // In a real-world application, you should throw an exception if the table does not already exists.
		return tableServiceClient.GetTableClient(Environment.GetEnvironmentVariable("TelemetryTableName")!);
	}


}
```

### Step A4: Implement the Send Alert Activity Function

In the `Monitor` folder, create a new class named `SendAlertActivity.cs` and replace the default code with the following:

```c#
using Azure;
using Azure.Communication.Email;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp.Monitor;

public static class SendAlertActivity
{

	[Function(nameof(SendAlert))]
	public static void SendAlert([ActivityTrigger] string url, FunctionContext functionContext)
	{

		ILogger logger = functionContext.GetLogger(nameof(SendAlert));
		logger.LogInformation("Sending alert of down website - {url}...", url);

		EmailClient emailClient = GetEmailClient();
		EmailMessage emailMessage = new(
			senderAddress: Environment.GetEnvironmentVariable("SendEmailFrom")!,
			content: new EmailContent("Website Down Alert")
			{
				PlainText = $"The website `{url} is down.",
				Html = $@"<html><body><h1>Website Down Alert</h1><p>The website `{url}' is down.</body></html>"
			},
			recipients: new EmailRecipients([new EmailAddress("chadgreen@chadgreen.com")])
		);
		EmailSendOperation emailSendOperation = emailClient.Send(WaitUntil.Started, emailMessage);

	}

	private static EmailClient GetEmailClient()
	{
		DefaultAzureCredential credential = new();
		EmailClient emailClient = new(new Uri(Environment.GetEnvironmentVariable("AzureCommunicationServicesEndpoint")!), credential);
		return emailClient;
	}

}
```

### Step A5: Implement the Orchestrator Function

The **orchestrator** function coordinates the scrapping of multiple websites by dispatching an activity functions for each listed website simultaneously, allowing them to run in parallel. This reduces the overall execution time for tasks that can be performed concurrently.

In the `Monitor` folder, create a new class named `Orchestrator.cs` and replace the default code with the following:

```c#
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace FunctionApp.Monitor;

public static class Orchestrator
{

	[Function(nameof(MonitorOrchestrator))]
	public static async Task MonitorOrchestrator(
		[OrchestrationTrigger] TaskOrchestrationContext context)
	{

		ILogger logger = context.CreateReplaySafeLogger(nameof(Orchestrator));
		logger.LogInformation("Starting website monitor service...");

		string? websiteUrl = context.GetInput<string>();
		TimeSpan checkInterval = TimeSpan.FromMinutes(Convert.ToDouble(Environment.GetEnvironmentVariable("WebsiteMonitoringDefaultInternval")!));

		while (context.CurrentUtcDateTime < context.CurrentUtcDateTime.AddHours(24)) // Monitor for 24 hours
		{
			logger.LogInformation("Checking website status for {websiteUrl}...", websiteUrl);
			bool isWebsiteUp = await context.CallActivityAsync<bool>(nameof(CheckWebsiteStatusActivity.CheckWebsiteStatus), websiteUrl);
			if (!isWebsiteUp)
			{
				logger.LogError("Website {websiteUrl} is down. Sending alert...", websiteUrl);
				await context.CallActivityAsync(nameof(SendAlertActivity.SendAlert), websiteUrl);
			}
			else
			{
				logger.LogInformation("Website {websiteUrl} is up. Waiting for next check...", websiteUrl);
			}
			DateTime nextCheck = context.CurrentUtcDateTime.Add(checkInterval);
			await context.CreateTimer(context.CurrentUtcDateTime.Add(checkInterval), CancellationToken.None);
		}

	}

}
```

### Step A6: Implement the HTTP Starter

Durable Functions require a standard Azure Function using any of the non-durable triggers. Here, we are building a starter Azure Function using an HTTP trigger.

In the `Monitor` folder, create a new class named `HttpStarter.cs` and replace the default code with the following:

```c#
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Net;

namespace FunctionApp.Monitor;

public static class HttpStarter
{

	[Function(nameof(MonitorHttpStarter))]
	public static async Task<HttpResponseData> MonitorHttpStarter(
	[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "monitor")] HttpRequestData request,
	[DurableClient] DurableTaskClient client,
	FunctionContext executionContext)
	{
		ILogger logger = executionContext.GetLogger("HttpStarter");
		string? websiteUrl = request.Query["url"];
		if (string.IsNullOrEmpty(websiteUrl))
			return request.CreateResponse(HttpStatusCode.BadRequest);
		string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(Orchestrator.MonitorOrchestrator), websiteUrl);
		logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);
		return await client.CreateCheckStatusResponseAsync(request, instanceId);
	}

}
```

---

## Section B: Add Console App Logic

To make it easier to run the demonstration, we will add logic to our console app to execute the demo for us.

### Step B1: Add the Monitor Demo Logic

1. At the top of the **Program.cs** file, below `int _waitTimeInSeconds = 2;` add the following:

   ```c#
   string _tableStorageEndpointOrConnectionString = "UseDevelopmentStorage=true";
   string _telemetryTableName = "ServerlessOrchestrationTelemetry";
   string _environment = "local";
   ```

2. Add the `GetTableClient` method to the **Program.cs** file:

   ```c#
   TableClient GetTableClient()
   {
   	DefaultAzureCredential credential = new();
   	TableServiceClient tableServiceClient;
   	if (!_environment.Equals("local", StringComparison.InvariantCultureIgnoreCase))
   		tableServiceClient = new(new Uri(_tableStorageEndpointOrConnectionString), credential);
   	else
   		tableServiceClient = new TableServiceClient(_tableStorageEndpointOrConnectionString);
   	tableServiceClient.CreateTableIfNotExists(_telemetryTableName); // In a real-world application, you should throw an exception if the table does not already exists.
   	return tableServiceClient.GetTableClient(_telemetryTableName);
   }
   ```

3. Add the `GetLatestTelemetryData` method to the **Program.cs** file:

   ```c#
   static TelemetryTableEntity? GetLatestTelemetryData(TableClient tableClient, string url)
   {
   	Uri uri = new(url);
   	Azure.Pageable<TelemetryTableEntity> queryResults = tableClient.Query<TelemetryTableEntity>(entity => entity.PartitionKey == uri.Host);
   	return queryResults.OrderByDescending(e => e.Timestamp).FirstOrDefault();
   }
   ```

   

4. Add the `StartWebMonitoringAsync` method to the **Program.cs** file:

   ```c#
   static async Task<bool> StartWebMonitoringAsync(HttpClient _httpClient, string _functionAppUrl, string demoName, string demoRoute, string? queryString, StringContent? requestBody)
   {
   	bool webMonitoringStarted = false;
   
   	Table demoResulsTable = new();
   	demoResulsTable.HideHeaders();
   
   	await AnsiConsole.Live(demoResulsTable)
   		.StartAsync(async ctx =>
   		{
   			demoResulsTable.Title($"{demoName} Demo").LeftAligned();
   			ctx.Refresh();
   			demoResulsTable.AddColumn("Orchestration Status");
   			demoResulsTable.Columns[0].NoWrap();
   			ctx.Refresh();
   			demoResulsTable.AddRow("Starting the orchestration...");
   			ctx.Refresh();
   
   			Uri httpStartUri = new($"{_functionAppUrl}{demoRoute}");
   			if (!string.IsNullOrEmpty(queryString))
   				httpStartUri = new Uri($"{httpStartUri}?{queryString}");
   			HttpResponseMessage response = await _httpClient.PostAsync(httpStartUri, requestBody);
   			string responseBody = await response.Content.ReadAsStringAsync();
   
   			if (!string.IsNullOrEmpty(responseBody))
   			{
   				DurableOrchestrationStartedResponse? statusQueryResult = JsonSerializer.Deserialize<DurableOrchestrationStartedResponse>(responseBody);
   				if (statusQueryResult != null)
   				{
   
   					string statusQueryUri = statusQueryResult.StatusQueryGetUri;
   					demoResulsTable.AddEmptyRow();
   					demoResulsTable.AddRow(new Markup($"[green]Orchestration started.[/] [darkviolet]Orchestration ID:[/] [purple_1]{statusQueryResult.Id}[/]"));
   					ctx.Refresh();
   					webMonitoringStarted = true;
   				}
   				else
   				{
   					demoResulsTable.AddEmptyRow();
   					demoResulsTable.AddRow(new Markup("[red]Failed to start the orchestration[/]"));
   					ctx.Refresh();
   				}
   			}
   			else
   			{
   				demoResulsTable.AddEmptyRow();
   				demoResulsTable.AddRow(new Markup("[red]Failed to start the orchestration[/]"));
   				ctx.Refresh();
   			}
   
   		});
   	return webMonitoringStarted;
   }
   ```

5. Add the `ExecuteMonitoringDemoRunner` method to the **Program.cs** class:

   ```C#
   async Task ExecuteMonitoringDemoRunner(List<string> urlsToMonitor)
   {
   
   	string demoName = "Monitoring";
   	string demoRoute = "monitor";
   
   	string? queryString = null;
   	StringContent? requestBody = null;
   	if (urlsToMonitor.Count > 1)
   		requestBody = new StringContent(JsonSerializer.Serialize(urlsToMonitor), Encoding.UTF8, "application/json");
   	else
   		queryString = "url=https://chadgreen.com";
   
   	try
   	{
   		bool webMonitoringStarted = await StartWebMonitoringAsync(_httpClient, _functionAppUrl, demoName, demoRoute, queryString, requestBody);
   		AnsiConsole.WriteLine();
   		if (webMonitoringStarted)
   			await MonitorTelemtryAsync(urlsToMonitor, _environment);
   	}
   	catch (Exception ex)
   	{
   		AnsiConsole.WriteException(ex);
   	}
   
   	async Task MonitorTelemtryAsync(List<string> urlsToMonitor, string _environment)
   	{
   		Table monitoringResultsTable = new();
   
   		TableClient tableClient = GetTableClient();
   
   		await AnsiConsole.Live(monitoringResultsTable)
   			.StartAsync(async ctx =>
   			{
   				monitoringResultsTable.Title($"Web Monitoring Results").LeftAligned();
   				ctx.Refresh();
   				monitoringResultsTable.AddColumn("Monitored URL");
   				monitoringResultsTable.AddColumn("Is Up");
   				monitoringResultsTable.AddColumn("Status Code");
   				monitoringResultsTable.AddColumn("Response Time (ms)");
   				ctx.Refresh();
   				await Task.Delay(TimeSpan.FromSeconds(30));
   				while (true)
   				{
   					foreach (string urlToMonitor in urlsToMonitor)
   					{
   						TelemetryTableEntity telemetryData = GetLatestTelemetryData(tableClient, urlToMonitor)!;
   						if (telemetryData is not null)
   						{
   							monitoringResultsTable.AddEmptyRow();
   							monitoringResultsTable.AddRow(urlToMonitor, telemetryData.IsUp.ToString(), telemetryData.StatusCode ?? string.Empty, telemetryData.ResponseTimeMs.ToString());
   							ctx.Refresh();
   						}
   					}
   					await Task.Delay(TimeSpan.FromMinutes(5));
   				}
   			});
   	}
   }
   ```

6. Update the `ExecuteFanOutFanInDemo` method in the **Program.cs** class with the following code:

   ```C#
   async Task ExecuteMonitoringDemo()
   {
   	await ExecuteMonitoringDemoRunner(["https://updateconference.net"]);
   }
   ```

---

## Section C: Locally Execute the Function Chaining Demo

Now that we have all the code written, we can execute the **Monitor** demo.

1. From Visual Studio, start the solution by clicking the **F5** key.

2. Two console windows will be opened:

   - In the first, you will see the list of functions within the function app. This includes the accessible functions (**MonitorHttpStarter**), the orchestrator (**MonitorOrchestrator**), and the activity functions (**CheckWebsiteStatus** and **SendAlert**).

     > If you completed any of the other labs before this one, you will also see the functions created in those labs.

   - In the second, you will see our console application which will allow you to execute the demonstrations.

3. From the **Serverless Orchestration** console window, select the **Monitor** demonstration.

4. Observe the workflow from the two console windows.

5. Once the orchestration has completed; review the list of webpages that were scraped for content.
