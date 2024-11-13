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