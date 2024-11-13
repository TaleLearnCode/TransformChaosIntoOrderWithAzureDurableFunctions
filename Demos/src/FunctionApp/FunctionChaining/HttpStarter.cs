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
		string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(Orchestrator.FunctionChainingOrchestrator));
		logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);
		return await client.CreateCheckStatusResponseAsync(request, instanceId);
	}

}