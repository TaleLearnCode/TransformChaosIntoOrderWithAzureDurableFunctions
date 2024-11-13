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