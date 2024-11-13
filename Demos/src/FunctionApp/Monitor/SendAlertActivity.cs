using Azure;
using Azure.Communication.Email;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp.Monitor;

public static class SendAlertActivity
{

	[Function(nameof(SendAlert))]
	public static void SendAlert([ActivityTrigger] string url, FunctionContext functionContext)
	{

		ILogger logger = functionContext.GetLogger(nameof(SendAlert));
		logger.LogInformation("Sending alert of down website - {url}...", url);

		EmailClient emailClient = GetEmailClient();
		EmailMessage emailMessage = new(
			senderAddress: Environment.GetEnvironmentVariable("SendEmailFrom")!,
			content: new EmailContent("Website Down Alert")
			{
				PlainText = $"The website `{url} is down.",
				Html = $@"<html><body><h1>Website Down Alert</h1><p>The website `{url}' is down.</body></html>"
			},
			recipients: new EmailRecipients([new EmailAddress("chadgreen@chadgreen.com")])
		);
		EmailSendOperation emailSendOperation = emailClient.Send(WaitUntil.Started, emailMessage);

	}

	private static EmailClient GetEmailClient()
	{
		DefaultAzureCredential credential = new();
		//EmailClient emailClient = new(new Uri(Environment.GetEnvironmentVariable("AzureCommunicationServicesEndpoint")!), credential);
		EmailClient emailClient = new(Environment.GetEnvironmentVariable("AzureCommunicationServicesEndpoint")!);
		return emailClient;
	}

}