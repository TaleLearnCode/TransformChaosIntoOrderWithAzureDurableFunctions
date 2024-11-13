using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp.FunctionChaining;

public static class FetchDataFromLocation2Activity
{
	[Function(nameof(FetchDataFromLocation2))]
	public static async Task<List<string>> FetchDataFromLocation2([ActivityTrigger] FunctionContext functionContext)
	{
		ILogger logger = functionContext.GetLogger(nameof(FetchDataFromLocation2));
		logger.LogInformation("Fetching data from Location 2...");
		await Task.Delay(1000);
		return ["Data1_Location2", "Data2_Location2"];
	}
}