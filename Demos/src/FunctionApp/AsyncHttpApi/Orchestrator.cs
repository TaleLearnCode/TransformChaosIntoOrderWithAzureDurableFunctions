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