using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp.FunctionChaining;

public static class ProcessDataActivity
{
	[Function(nameof(ProcessData))]
	public static async Task<string> ProcessData(
		[ActivityTrigger] List<List<string>> reportData, FunctionContext functionContext)
	{
		ILogger logger = functionContext.GetLogger(nameof(ProcessData));
		logger.LogInformation("Processing the fetched data...");
		await Task.Delay(1000);
		string processedData = string.Join(",", reportData.SelectMany(x => x));
		return processedData;
	}
}