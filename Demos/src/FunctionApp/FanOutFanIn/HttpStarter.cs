using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;

namespace FunctionApp.FanOutFanIn;

public static class HttpStarter
{

	[Function(nameof(StartWebScrapping))]
	public static async Task<HttpResponseData> StartWebScrapping(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "fan-out-fan-in")] HttpRequestData request,
		[DurableClient] DurableTaskClient client,
		FunctionContext executionContext)
	{
		ILogger logger = executionContext.GetLogger("HttpStarter");

		string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
		List<string>? urls = JsonConvert.DeserializeObject<List<string>>(requestBody);
		if (urls == null || urls.Count == 0)
			return request.CreateResponse(HttpStatusCode.BadRequest);

		string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(Ochestrator.FanInFanOutOrcestrator), urls);
		logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);
		return await client.CreateCheckStatusResponseAsync(request, instanceId);
	}

}