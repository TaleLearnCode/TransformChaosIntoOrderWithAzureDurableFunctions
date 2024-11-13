using FunctionApp.FunctionChaining;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace FunctionApp.FanOutFanIn;

public static class Ochestrator
{

	[Function(nameof(FanInFanOutOrcestrator))]
	public static async Task<Dictionary<string, string>> FanInFanOutOrcestrator(
	[OrchestrationTrigger] TaskOrchestrationContext context)
	{
		ILogger logger = context.CreateReplaySafeLogger(nameof(Orchestrator));

		List<string>? urls = context.GetInput<List<string>>();
		if (urls == null)
		{
			logger.LogError("Input URLs cannot be null");
			return [];
		}

		List<Task<KeyValuePair<string, string>>> tasks = [];
		foreach (string url in urls)
			tasks.Add(context.CallActivityAsync<KeyValuePair<string, string>>(nameof(ScrapeWebpageActivity.ScrapeWebpage), url));

		logger.LogInformation("Starting fan-out/fan-in activities...");
		KeyValuePair<string, string>[] results = await Task.WhenAll(tasks);
		logger.LogInformation("Fan-out/fan-in activities completed.");

		Dictionary<string, string> scrapedData = [];
		foreach (KeyValuePair<string, string> result in results)
			scrapedData.Add(result.Key, result.Value);

		return scrapedData;

	}

}