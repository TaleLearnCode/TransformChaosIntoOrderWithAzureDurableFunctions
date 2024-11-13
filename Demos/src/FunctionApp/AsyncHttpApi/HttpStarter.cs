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