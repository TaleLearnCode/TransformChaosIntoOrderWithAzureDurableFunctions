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