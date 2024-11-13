using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;

namespace FunctionApp.FunctionChaining;

public static class GenerateReportActivity
{

	[Function(nameof(GenerateReport))]
	public static async Task<string> GenerateReport(
		[ActivityTrigger] string processedData, FunctionContext functionContext)
	{

		ILogger logger = functionContext.GetLogger(nameof(GenerateReport));
		logger.LogInformation("Generating report...");

		// Simulate report generation
		await Task.Delay(1000);
		string reportContent = $"Report Content: {processedData}";

		// Save the report to Blob Storage
		BlobContainerClient containerClient = GetBlobContainerClient();
		string blobName = $"report_{Guid.NewGuid()}.txt";
		await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob); // In a real-world application, you should throw an exception if the container does not already exists.
		BlobClient blobClient = containerClient.GetBlobClient(blobName);
		using MemoryStream stream = new(Encoding.UTF8.GetBytes(reportContent));
		await blobClient.UploadAsync(stream, true);

		return blobClient.Uri.ToString();

	}

	private static BlobContainerClient GetBlobContainerClient()
	{
		string environment = Environment.GetEnvironmentVariable("Environment") ?? "Non-Local";
		DefaultAzureCredential credential = new();
		BlobServiceClient blobServiceClient;
		if (!environment.Equals("local", StringComparison.InvariantCultureIgnoreCase))
			blobServiceClient = new(new Uri(Environment.GetEnvironmentVariable("BlobStorageEndpoint")!), credential);
		else
			blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("ReportsContainerConnectionString")!);
		return blobServiceClient.GetBlobContainerClient(Environment.GetEnvironmentVariable("ReportsContainerName")!);
	}

}