using System.Text.Json.Serialization;

namespace ConsoleApp.Responses;

public class DurableOrchestrationStartedResponse
{

	[JsonPropertyName("id")]

	public required string Id { get; set; }

	[JsonPropertyName("sendEventPostUri")]
	public required string SendEventPostUri { get; set; }

	[JsonPropertyName("statusQueryGetUri")]
	public required string StatusQueryGetUri { get; set; }

	[JsonPropertyName("terminatePostUri")]
	public required string TerminatePostUri { get; set; }

	[JsonPropertyName("suspendPostUri")]
	public required string SuspendPostUri { get; set; }

	[JsonPropertyName("resumePostUri")]
	public required string ResumePostUri { get; set; }

}
