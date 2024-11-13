using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FunctionApp.Monitor;

public static class CheckWebsiteStatusActivity
{

	private static Func<HttpClient> HttpClientFactory { get; set; } = () => new HttpClient();

	[Function(nameof(CheckWebsiteStatus))]
	public static async Task<bool> CheckWebsiteStatus([ActivityTrigger] string url, FunctionContext functionContext)
	{

		ILogger logger = functionContext.GetLogger(nameof(CheckWebsiteStatus));
		logger.LogInformation("Checking Website Status: {url}", url);

		using HttpClient httpClient = HttpClientFactory();

		Uri uri = new(url);

		bool isWebsiteUp = false;
		string? errorMessage = null;
		string? statusCode = null;
		long responseTimeMs = 0;

		try
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			HttpResponseMessage response = await httpClient.GetAsync(uri);
			stopwatch.Stop();
			isWebsiteUp = response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			logger.LogError("Error checking website {url}: {errorMessage}", url, ex.Message);
			errorMessage = ex.Message;
		}

		try
		{
			TableClient tableClient = GetTableClient();

			await tableClient.AddEntityAsync(new TelemetryTableEntity
			{
				PartitionKey = uri.Host,
				RowKey = Guid.NewGuid().ToString(),
				Url = url,
				IsUp = isWebsiteUp,
				ErrorMessage = errorMessage,
				StatusCode = statusCode,
				ResponseTimeMs = responseTimeMs
			});
		}
		catch (Exception ex)
		{
			logger.LogError("Error saving telemetry for website {url}: {errorMessage}", url, ex.Message);
		}

		return isWebsiteUp;

	}

	private static TableClient GetTableClient()
	{
		string environment = Environment.GetEnvironmentVariable("Environment") ?? "Non-Local";
		DefaultAzureCredential credential = new();
		TableServiceClient tableServiceClient;
		if (!environment.Equals("local", StringComparison.InvariantCultureIgnoreCase))
			tableServiceClient = new(new Uri(Environment.GetEnvironmentVariable("TableStorageEndpoint")!), credential);
		else
			tableServiceClient = new TableServiceClient(Environment.GetEnvironmentVariable("TelemetryTableConnectionString")!);
		tableServiceClient.CreateTableIfNotExists(Environment.GetEnvironmentVariable("TelemetryTableName")!); // In a real-world application, you should throw an exception if the table does not already exists.
		return tableServiceClient.GetTableClient(Environment.GetEnvironmentVariable("TelemetryTableName")!);
	}


}