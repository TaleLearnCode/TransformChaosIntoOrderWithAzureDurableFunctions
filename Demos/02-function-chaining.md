[Serverless Orchestration Demo](README.md) \

# Function Chaining

The **function chaining** pattern in Azure Durable Functions is a way to execute a sequence of functions in a specific order, where the output of one function can be passed as input to the next function. This pattern is useful for orchestrating complex workflows where each step depends on the result of the previous one.

Here is a simplified example to illustrate the concept:

1. **Orchestrator Function**: This function coordinates the execution of the sequence. It calls activity functions in a specific order and stores their outputs.
2. **Activity Functions**: These are individual functions that perform specific tasks. There outputs are used as inputs for the next function in the chain.

#### Example

Here is a basic example in C#:

```c#
[FunctionName("E1_HelloSequence")]
public static async Task<List<string>> Run(
    [OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var outputs = new List<string>();
    outputs.Add(await context.CallActivityAsync<string>("E1_SayHello", "Tokyo"));
    outputs.Add(await context.CallActivityAsync<string>("E1_SayHello", "Seattle"));
    outputs.Add(await context.CallActivityAsync<string>("E1_SayHello", "London"));
    return outputs;
}

[FunctionName("E1_SayHello")]
public static string SayHello([ActivityTrigger] string name)
{
    return $"Hello {name}!";
}
```

In this example:

- The `H1_HellowSequence` orchestrator function calls the `E1_SayHello` activity function three times with different inputs ("Tokyo", "Seattle", "London").
- The `E1_SayHello` activity function prepends "Hello" to the input name and returns the result.

The outputs of each call are stored in a list and returned by the orchestrator function.

#### Benefits

1. **Simplified Workflow Management**: The function chaining pattern allows you to manage complex workflows with multiple steps easily. The orchestrator function takes care of coordinating the sequence, making the code more readable and maintainable.
2. **Reliable Execution**: Azure Durable Functions ensure that each activity function is executed exactly once, even in the face of failures or restarts. This guarantees reliable execution of each step in the workflow.
3. **Scalability**: Each activity function runs independently, allowing Azure to scale them individually based on demand. This makes the function chaining pattern highly scalable and efficient.
4. **State Management**: The orchestrator function automatically checkpoints its progress. If an orchestrator function instance fails or is terminated, it can be restarted from the last checkpoint, ensuing no loss of progress.
5. **Cost-Effective**: You only pay for the time your functions are running. Azure Functions provide a consumption-based pricing model that can be very cost-effective for workflows with irregular or unpredictable workloads.

By using the function chaining pattern, you can orchestrate a sequence of tasks in a clear and maintainable way, leveraging the benefits of Azure's durable and scalable infrastructure.

Let's now build an Azure Functions App that uses the **function chaining** pattern for report generation requiring multiple, discrete activities.

## Section A: Add the Durable Function Logic

First, we will update our Function App to include the logic for our Function Chaining demo. For this demonstration, we will implement a report generation workflow that gathers data from various sources, processes it, and generates a report. Each step (data collection, processing, report generation) is a separate activity function.

### Step A1: Prepare Durable Function App for Function Chaining Demo

1. Add the necessary Azure Storage NuGet package:

   ```powershell
   Install-Package Azure.Storage.Blobs
   ```

2. Open the `local.settings.json` and add the following values:

   | Key                              | Value                            |
   | -------------------------------- | -------------------------------- |
   | ReportsContainerConnectionString | UseDevelopmentStorage=true       |
   | ReportsContainerName             | serverless-orchestration-reports |

3. Add a folder named `FunctionChaining` to the **FunctionApp** project.

### Step A2: Implement the Fetch Data from Location 1 Activity Function

**Activity Functions** perform the specific tasks such as data collection, processing, and report generation. In this step, we are going to create the activity function that performs the first step of our workflow - retrieving data from the first data location for the report.

In the `FunctionChaining` folder, create a new class named `FetchDataFromLocation1Activity.cs` and replace the default code with the following:

```c#
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp.FunctionChaining;

public static class FetchDataFromLocation1Activity
{
	[Function(nameof(FetchDataFromLocation1))]
	public static async Task<List<string>> FetchDataFromLocation1([ActivityTrigger] FunctionContext functionContext)
	{
		ILogger logger = functionContext.GetLogger(nameof(FetchDataFromLocation1));
		logger.LogInformation("Fetching data from Location 1...");
		await Task.Delay(1000);
		return ["Data1_Location1", "Data2_Location1"];
	}
}
```

### Step A3: Implement the Fetch Data from Location 2 Activity Function

Now we will add another **activity function**. This one will gather data from the second data location.

In the `FunctionChaining` folder, create a new class named `FetchDataFromLocation2Activity.cs` and replace the default code with the following:

```C#
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp.FunctionChaining;

public static class FetchDataFromLocation2Activity
{
	[Function(nameof(FetchDataFromLocation2))]
	public static async Task<List<string>> FetchDataFromLocation2([ActivityTrigger] string name, FunctionContext functionContext)
	{
		ILogger logger = functionContext.GetLogger(nameof(FetchDataFromLocation2));
		logger.LogInformation("Fetching data from Location 2...");
		await Task.Delay(1000);
		return ["Data1_Location2", "Data2_Location2"];
	}
}
```

### Step A4: Implement the Process Data Activity Function

Now we will add the **activity function** that will process the data gathered in the two previous steps. Notice here we are passing data into the activity function.

In the `FunctionChaining` folder, create a new class named `ProcessDataActivity.cs` and replace the default code with the following:

```C#
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp.FunctionChaining;

public static class ProcessDataActivity
{
	[Function(nameof(ProcessData))]
	public static async Task<string> ProcessData([ActivityTrigger] List<List<string>> reportData, FunctionContext functionContext)
	{
		ILogger logger = functionContext.GetLogger(nameof(ProcessData));
		logger.LogInformation("Processing the fetched data...");
		await Task.Delay(1000);
		string processedData = string.Join(",", reportData.SelectMany(x => x));
		return processedData;
	}
}
```

### Step A5: Implement the Generate Report Activity Function

This final **activity function** will take the processed data and generate the report.

In the `FunctionChaining` folder, create a new class named `GenerateReportActivity.cs` and replace the default code with the following:

```c#
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;

namespace FunctionApp.FunctionChaining;

public static class GenerateReportActivity
{

	[Function(nameof(GenerateReport))]
	public static async Task<string> GenerateReport([ActivityTrigger] string processedData, FunctionContext functionContext)
	{

		ILogger logger = functionContext.GetLogger(nameof(GenerateReport));
		logger.LogInformation("Generating report...");

		// Simulate report generation
		await Task.Delay(1000);
		string reportContent = $"Report Content: {processedData}";

		// Save the report to Blob Storage
		BlobContainerClient containerClient = GetBlobContainerClient();
		string blobName = $"report_{Guid.NewGuid()}.txt";
		await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob); // In a real-world application, you should throw an exception if the container does not already exists.
		BlobClient blobClient = containerClient.GetBlobClient(blobName);
		using MemoryStream stream = new(Encoding.UTF8.GetBytes(reportContent));
		await blobClient.UploadAsync(stream, true);

		return blobClient.Uri.ToString();

	}

	private static BlobContainerClient GetBlobContainerClient()
	{
		string environment = Environment.GetEnvironmentVariable("Environment") ?? "Non-Local";
		DefaultAzureCredential credential = new();
		BlobServiceClient blobServiceClient;
		if (!environment.Equals("local", StringComparison.InvariantCultureIgnoreCase))
			blobServiceClient = new(new Uri(Environment.GetEnvironmentVariable("BlobStorageEndpoint")!), credential);
		else
			blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("ReportsContainerConnectionString")!);
		return blobServiceClient.GetBlobContainerClient(Environment.GetEnvironmentVariable("ReportsContainerName")!);
	}

}
```

### Step A6: Implement the Orchestrator Function

The **orchestrator function** manages the workflow by calling the activity functions in sequence. In the orchestrator function below, we will:

- Gather data from the first data source
- Gather data from the second data source
- Process the fetched data
- Build the report using the processed data

At the end of the orchestrator, we will return the URL of the generated report.

In the `FunctionChaining` folder, create a new class named `Orchestrator.cs` and replace the default code with the following:

```C#
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace FunctionApp.FunctionChaining;

public class Orchestrator
{

	[Function(nameof(FunctionChainingOrchestrator))]
	public static async Task<string> FunctionChainingOrchestrator(
		[OrchestrationTrigger] TaskOrchestrationContext context)
	{
		ILogger logger = context.CreateReplaySafeLogger(nameof(Orchestrator));
		logger.LogInformation("Starting report generation.");

		List<string> data1 = await context.CallActivityAsync<List<string>>(nameof(FetchDataFromLocation1Activity.FetchDataFromLocation1));
		List<string> data2 = await context.CallActivityAsync<List<string>>(nameof(FetchDataFromLocation2Activity.FetchDataFromLocation2));
		string processedData = await context.CallActivityAsync<string>(nameof(ProcessDataActivity.ProcessData), new List<List<string>> { data1, data2 });
		string reportUrl = await context.CallActivityAsync<string>(nameof(GenerateReportActivity.GenerateReport), processedData);

		return reportUrl;

	}

}
```

### Step A7: Implement the HTTP Starter

Durable Functions require a standard Azure Function using any of the non-durable triggers. Here, we are building a starter Azure Function using an HTTP trigger.

In the `FunctionChaining` folder, create a new class named `HttpStarter.cs` and replace the default code with the following:

```C#
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace FunctionApp.FunctionChaining;

public static class HttpStarter
{

	[Function(nameof(FunctionChainingHttpStarter))]
	public static async Task<HttpResponseData> FunctionChainingHttpStarter(
	[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "function-chaining")] HttpRequestData request,
	[DurableClient] DurableTaskClient client,
	FunctionContext executionContext)
	{
		ILogger logger = executionContext.GetLogger("HttpStarter");
		string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(Orchestrator.RunOrchestrator));
		logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);
		return await client.CreateCheckStatusResponseAsync(request, instanceId);
	}

}
```

> [!TIP]
>
> Its a good idea to include an HTTP triggered starter when using other trigger types (such as the timer, queue, and so on triggers). This makes it easier for debugging and those types you need to manually trigger the orchestration.

---

## Section B: Add Console App Logic

To make it easier to run the demonstration, we will add logic to our console app to execute the demo for us.

### Step B1: Add the Function Chaining Demo Logic

Update the `ExecuteFunctionChainingDemo` method in the **Program.cs** class with the following code:+

```c#
async Task ExecuteFunctionChainingDemo()
{
	Tuple<bool, object?> demoResult = await ExecuteDemo("Function Chaining", "function-chaining");
	if (demoResult.Item1)
	{
		string? result = JsonSerializer.Deserialize<string>(JsonSerializer.Serialize(demoResult.Item2));
		if (result is not null)
			AnsiConsole.MarkupLine($"[gray]Report URL:[/] {result}");
		else
			AnsiConsole.MarkupLine("[red]Failed to get the report URL[/]");
	}
}
```

In this method, we are going to execute the **Function Chaining** orchestration demonstration and display the URL to the generated report.

---

## Section C: Locally Execute the Function Chaining Demo

Now that we have all the code written, we can execute the **Function Chaining** demo.

1. From Visual Studio, start the solution by clicking the **F5** key.

2. Two console windows will be opened:

   - In the first, you will see the list of functions within the function app. This includes the accessible functions (**FunctionChainingHttpStarter**), the orchestrator (**FunctionChainingOrchestrator**), and the activity functions (**FetchDataFromLocation1**, **FetchDataFromLocation2**, **GenerateReport**, and **ProcessData**).

     > If you completed any of the other labs before this one, you will also see the functions created in those labs.

   - In the second, you will see our console application which will allow you to execute the demonstrations.

3. Expand the width of the **Serverless Orchestration** console window.

   > While building the demonstration, it was observed that the link returned at the end of the demonstration does not work unless the whole link address is displayed on the same line in the console.

4. From the **Serverless Orchestration** console window, select the **Function Chaining** demonstration.

5. Observe the workflow from the two console windows.

6. Once the orchestration has completed; control-click the link to the report from the **Serverless Orchestration** and review the generated report.