using System.Text.Json.Serialization;

namespace ConsoleApp.Responses;

public class DurableOrchestrationStatusReponse
{
	[JsonPropertyName("name")]
	public required string Name { get; set; }

	[JsonPropertyName("instanceId")]
	public required string InstanceId { get; set; }

	[JsonPropertyName("runtimeStatus")]
	public required string RuntimeStatus { get; set; }

	[JsonPropertyName("input")]
	public object? Input { get; set; }

	[JsonPropertyName("customStatus")]
	public object? CustomStatus { get; set; }

	[JsonPropertyName("output")]
	public object? Output { get; set; }

	[JsonPropertyName("createdTime")]
	public DateTime CreatedTime { get; set; }

	[JsonPropertyName("lastUpdatedTime")]
	public DateTime LastUpdatedTime { get; set; }

}