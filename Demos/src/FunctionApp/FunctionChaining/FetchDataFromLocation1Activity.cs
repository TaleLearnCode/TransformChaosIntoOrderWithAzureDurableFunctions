using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp.FunctionChaining;

public static class FetchDataFromLocation1Activity
{
	[Function(nameof(FetchDataFromLocation1))]
	public static async Task<List<string>> FetchDataFromLocation1([ActivityTrigger] FunctionContext functionContext)
	{
		ILogger logger = functionContext.GetLogger(nameof(FetchDataFromLocation1));
		logger.LogInformation("Fetching data from Location 1...");
		await Task.Delay(1000);
		return ["Data1_Location1", "Data2_Location1"];
	}
}