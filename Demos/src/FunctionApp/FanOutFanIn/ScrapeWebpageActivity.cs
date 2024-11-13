using HtmlAgilityPack;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp.FanOutFanIn;

public static class ScrapeWebpageActivity
{

	private static Func<HttpClient> HttpClientFactory { get; set; } = () => new HttpClient();

	[Function(nameof(ScrapeWebpage))]
	public static async Task<KeyValuePair<string, string>> ScrapeWebpage([ActivityTrigger] string url, FunctionContext functionContext)
	{
		ILogger logger = functionContext.GetLogger(nameof(ScrapeWebpage));
		logger.LogInformation("Scraping webpage: {url}", url);

		using HttpClient httpClient = HttpClientFactory();

		HttpResponseMessage response = await httpClient.GetAsync(url);
		response.EnsureSuccessStatusCode();

		string htmlContent = await response.Content.ReadAsStringAsync();
		HtmlDocument htmlDocument = new();
		htmlDocument.LoadHtml(htmlContent);

		HtmlNode titleNode = htmlDocument.DocumentNode.SelectSingleNode("//title");
		string title = titleNode != null ? titleNode.InnerText : "No Title";

		return new KeyValuePair<string, string>(url, title);

	}

}