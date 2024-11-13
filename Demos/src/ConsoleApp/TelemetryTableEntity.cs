using Azure;
using Azure.Data.Tables;

namespace ConsoleApp;

public class TelemetryTableEntity : ITableEntity
{

	public string PartitionKey { get; set; } = null!;
	public string RowKey { get; set; } = null!;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	public string Url { get; set; } = null!;
	public bool IsUp { get; set; }
	public string? ErrorMessage { get; set; }
	public string? StatusCode { get; set; }
	public long ResponseTimeMs { get; set; }
}