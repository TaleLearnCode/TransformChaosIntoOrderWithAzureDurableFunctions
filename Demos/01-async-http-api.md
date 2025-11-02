[Serverless Orchestration Demo](README.md) \

# Async HTTP API

The **Async HTTP API** pattern in Azure Durable Functions is used to manage long-running operations that are triggered by HTTP requests. This pattern is particularly useful when you need to coordinate the state of these operations with external clients.

#### How It Works

1. **Http Trigger**: An HTTP endpoint triggers the long-running operation. This endpoint starts an orchestration function that manages the workflow.
2. **Status Endpoint**: The client is redirected to a status endpoint that they can poll to check the status of the operation.
3. **Pooling**: The client periodically sends requests to the status endpoint to get updates on the progress of the operation.
4. **Completion**: Once the operation is complete, the status endpoint returns the final result to the client.

#### Example

Here is a simplified example in C#:

##### HTTP Start Function

```C#
[FunctionName("HttpStart")]
public static async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orchestrators/{functionName}")] HttpRequest req,
    [DurableClient] IDurableClient starter,
    string functionName,
    ILogger log)
{
    string instanceId = await starter.StartNewAsync(functionName, req.Body);
    log.LogInformation($"Started orchestration with ID = {instanceId}.");
    return starter.CreateCheckStatusResponse(req, instanceId);
}
```

##### Http Status Function

```C#
[FunctionName("HttpStatus")]
public static async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orchestrators/{functionName}/{instanceId}")] HttpRequest req,
    [DurableClient] IDurableOrchestrationClient starter,
    string functionName,
    string instanceId,
    ILogger log)
{
    var status = await starter.GetStatusAsync(instanceId);
    if (status == OrchestrationRuntimeStatus.Completed)
    {
        return new OkObjectResult("Operation completed successfully.");
    }
    else if (status == OrchestrationRuntimeStatus.Failed)
    {
        return new BadRequestObjectResult("Operation failed.");
    }
    else
    {
        return new OkObjectResult($"Operation is in progress. Status: {status}");
    }
}
```

#### Benefits

- **Client Coordination**: Allows clients to coordinate with long-running operations without needing to implement complex state management.
- **Scalability**: Azure Functions can handle multiple concurrent HTTP requests and orchestrate them efficiently.
- **Flexibility**: Can be used with various client applications, include web app, mobile apps, or other services.
- **Simplicity**: Simplifies the client-side implementation by providing a standard way to trigger and monitor long-running operations.

This pattern is ideal for scenarios where you need to perform tasks that take a significant amount of time and need to keep the client informed about the progress.

---

## Section A: Add the Durable Function Logic

First, we will will update our Function App to include the logic for our Async HTTP API demo. Suppose you need to perform time-consuming data operations, like data migration or complex querying. You can trigger the operation, get the status via HTTP, and notify when it is complete. In this demonstration, we are going to simulate that scenario.

### Step A1: Prepare the Function App for the Async HTTP API Demo

1. Add a folder named `AsyncHttpApi` to the **FunctionApp** project.

### Step A2: Implement the Activity Function

In the `AsyncHttpApi` folder, create a new class named `PerformDatabaseOperationActivity.cs` and replace the default code with the following:

```c#
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp.AsyncHttpApi;

public static class PerformDatabaseOperationActivity
{
	[Function(nameof(PerformDatabaseOperation))]
	public static async Task PerformDatabaseOperation([ActivityTrigger] string operationId, FunctionContext functionContext)
	{
		ILogger logger = functionContext.GetLogger(nameof(PerformDatabaseOperation));
		logger.LogInformation("Performing long-running database operation for {operationId}...", operationId);
		await Task.Delay(30000); // Simulate a 30-second database operation
		logger.LogInformation("Database operation for {operationId} completed.", operationId);
	}
}
```

### Step A3: Implement the Orchestrator Function

The **orchestrator** function coordinates the execution of the activities that make up the long-running API.

In the `AsyncHttpApi` folder, create a new class named `Orchestrator.cs` and replace the default code with the following:

```C#
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace FunctionApp.AsyncHttpApi;

public static class Orchestrator
{

	[Function(nameof(DatabaseOperationOrchestrator))]
	public static async Task DatabaseOperationOrchestrator(
	[OrchestrationTrigger] TaskOrchestrationContext context)
	{
		ILogger logger = context.CreateReplaySafeLogger(nameof(Orchestrator));
		string? operationId = context.GetInput<string>();
		logger.LogInformation("Starting database operation for {operationId}...", operationId);
		await context.CallActivityAsync(nameof(PerformDatabaseOperationActivity.PerformDatabaseOperation), operationId);
		logger.LogInformation("Database operation for {operationId} completed.", operationId);
	}

}
```

### Step A4: Implement the HTTP Starter

We will need an HTTP-triggered Azure Function to kick off the asynchronous API.

In the `AsyncHttpApi` folder, create a new class named `HttpStarter.cs` and replace the default code with the following:

```c#
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;

namespace FunctionApp.AsyncHttpApi;

public static class HttpStarter
{

	[Function(nameof(AsyncApiHttpStarter))]
	public static async Task<HttpResponseData> AsyncApiHttpStarter(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "async-http-api")] HttpRequestData request,
		[DurableClient] DurableTaskClient client,
		FunctionContext executionContext)
	{
		ILogger logger = executionContext.GetLogger("HttpStarter");

		string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
		Dictionary<string, string>? data = JsonConvert.DeserializeObject<Dictionary<string, string>>(requestBody);
		string? operationId = data?["operationId"];
		if (string.IsNullOrWhiteSpace(operationId))
			return request.CreateResponse(HttpStatusCode.BadRequest);

		string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(Orchestrator.DatabaseOperationOrchestrator), operationId);
		logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);
		return await client.CreateCheckStatusResponseAsync(request, instanceId);
	}

}
```

## Section B: Add Console App Logic

To make it easier to run the demonstration, we will add logic to our console app to execute the demo for us.

### Step B1: Add the Fan-Out/Fan-In Demo Logic

Update the `ExecuteAsyncHttpApisDemo` method in the **Program.cs** class with the following code:

```c#
async Task ExecuteAsyncHttpApisDemo()
{
	string operationId = Guid.NewGuid().ToString();
	StringContent requestBody = new(JsonSerializer.Serialize(new { operationId }), Encoding.UTF8, "application/json");
	await ExecuteDemo("Async HTTP APIs", "async-http-api", requestBody);
}
```

In this method, we define an operation identifier that could be used as a coloration identifier between the client and the API. This operation identifier is sent to the asynchronous API and then the client waits for the operation to be completed.

## Section C: Locally Execute the Function Chaining Demo

Now that we have all the code written, we can execute the **Async HTTP API** demo.

1. From Visual Studio, start the solution by clicking the **F5** key.

2. Two console windows will be opened:

   - In the first, you will see the list of functions within the function app. This includes the accessible functions (**AsyncApiHttpStarter**), the orchestrator (**DatabaseOperationOrchestrator**), and the activity function (**PerformDatabaseOperation**).

     > If you completed any of the other labs before this one, you will also see the functions created in those labs.

   - In the second, you will see our console application which will allow you to execute the demonstrations.

3. From the **Serverless Orchestration** console window, select the **Async HTTP API** demonstration.

4. Observe the workflow from the two console windows.