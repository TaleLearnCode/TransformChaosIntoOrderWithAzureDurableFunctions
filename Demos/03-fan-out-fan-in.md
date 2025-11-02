[Serverless Orchestration Demo](README.md) \

# Fan-Out/Fan-In

The **fan-out/fan-in** pattern in Azure Durable Functions is used to handle multiple tasks in parallel and then aggregate the results. This pattern is useful when you need to process a large number of independent tasks concurrently and then merge the results once all tasks are completed.

#### Fan-Out/Fan-In Pattern Explained

1. **Fan-Out**: The orchestrator function dispatches multiple activity functions simultaneously, allowing them to run in parallel. This reduces the overall execution time for tasks that can be performed concurrently.
2. **Fan-In**: After all the parallel tasks are completed, the orchestrator function collects and processes the results from each activity function, combining them into a final result.

#### Example

Consider a scenario where you need to process a large number of images:

1. **Orchestrator Function**: Manages the workflow, distributing the image processing tasks and then aggregating the results.
2. **Activity Functions**: Each function processes an individual image.

Here is an example in C#:

```c#
[FunctionName("ImageProcessingOrchestrator")]
public static async Task<List<string>> Run(
    [OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var images = context.GetInput<List<string>>();
    var tasks = new List<Task<string>>();

    foreach (var image in images)
    {
        tasks.Add(context.CallActivityAsync<string>("ProcessImage", image));
    }

    // Fan-in: Wait for all tasks to complete
    await Task.WhenAll(tasks);

    // Collect results
    var results = tasks.Select(task => task.Result).ToList();
    return results;
}

[FunctionName("ProcessImage")]
public static async Task<string> ProcessImage([ActivityTrigger] string image)
{
    // Simulate image processing
    await Task.Delay(1000);
    return $"Processed {image}";
}
```

**Workflow**

1. **Fan-Out**: The `ImageProcessingOrchestrator` function receives a list of images and calls for `ProcessImage` function for each image in parallel.
2. **Tasks**: Each `ProcessImage` Function simulates processing an image by waiting for a second then returning a processed image result.
3. **Fan-In**: The orchestrator waits for all the tasks to complete using `Task.WhenAll` and then collects the results into a list.

#### Benefits

- **Efficiency**: Running tasks in parallel significantly reduces the total processing time.
- **Scalability**: Azure Durable Functions automatically scale to handle large numbers of parallel tasks.
- **Simplicity**: The orchestrator function handles the complexity of tasks coordination and result aggregation.

This pattern is powerful for scenarios like batch processing, parallel computations, and aggregating data from multiple sources.

---

## Section A: Add the Durable Function Logic

First, we will update our Function App to include the logic for our Fan-Out/Fan-In demo. For this demonstration, we will implement a web scraper. In this scenario, we need to scrape data from multiple websites (webpages). We will kick off multiple scrapping functions in parallel, each targeting a different website or webpage, and then combine the collected data into a single result.

### Step A1: Prepare the Function App for the Fan-Out/Fan-In Demo

1. We will use the **HtmlAgilityPack** NuGet package to help parse webpages that we are scraping.

   ```powershell
   Install-Package HtmlAgilityPack
   ```

   

2. Add a folder named `FanOutFanIn` to the **FunctionApp** project.

### Step A2: Implement the Web Scraping Activity Function

In the `FanOutFanIn` folder, create a new class named `ScrapeWebpageActivity.cs` and replace the default code with the following:

```C#
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
```

### Step A3: Implement the Orchestrator Function

The **orchestrator** function coordinates the scrapping of multiple websites by dispatching an activity functions for each listed website simultaneously, allowing them to run in parallel. This reduces the overall execution time for tasks that can be performed concurrently.

In the `FanOutFanIn` folder, create a new class named `Orchestrator.cs` and replace the default code with the following:

```c#
using FunctionApp.FunctionChaining;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace FunctionApp.FanOutFanIn;

public static class Ochestrator
{

	[Function(nameof(FanInFanOutOrcestrator))]
	public static async Task<Dictionary<string, string>> FanInFanOutOrcestrator(
	[OrchestrationTrigger] TaskOrchestrationContext context)
	{
		ILogger logger = context.CreateReplaySafeLogger(nameof(Orchestrator));

		List<string>? urls = context.GetInput<List<string>>();
		if (urls == null)
		{
			logger.LogError("Input URLs cannot be null");
			return [];
		}

		List<Task<KeyValuePair<string, string>>> tasks = [];
		foreach (string url in urls)
			tasks.Add(context.CallActivityAsync<KeyValuePair<string, string>>(nameof(ScapeWebpageActivity.ScrapWebpage), url));

		logger.LogInformation("Starting fan-out/fan-in activities...");
		KeyValuePair<string, string>[] results = await Task.WhenAll(tasks);
		logger.LogInformation("Fan-out/fan-in activities completed.");

		Dictionary<string, string> scrapedData = [];
		foreach (KeyValuePair<string, string> result in results)
			scrapedData.Add(result.Key, result.Value);

		return scrapedData;

	}

}
```

### Step A4: Implement the HTTP Starter

Durable Functions require a standard Azure Function using any of the non-durable triggers. Here, we are building a starter Azure Function using an HTTP trigger.

In the `FanOutFanIn` folder, create a new class named `HttpStarter.cs` and replace the default code with the following:

```c#
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;

namespace FunctionApp.FanOutFanIn;

public static class HttpStarter
{

	[Function(nameof(StartWebScrapping))]
	public static async Task<HttpResponseData> StartWebScrapping(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "fan-out-fan-in")] HttpRequestData request,
		[DurableClient] DurableTaskClient client,
		FunctionContext executionContext)
	{
		ILogger logger = executionContext.GetLogger("HttpStarter");

		string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
		List<string>? urls = JsonConvert.DeserializeObject<List<string>>(requestBody);
		if (urls == null || urls.Count == 0)
			return request.CreateResponse(HttpStatusCode.BadRequest);

		string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(Ochestrator.FanInFanOutOrcestrator), urls);
		logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);
		return await client.CreateCheckStatusResponseAsync(request, instanceId);
	}

}
```

## Section B: Add Console App Logic

To make it easier to run the demonstration, we will add logic to our console app to execute the demo for us.

### Step B1: Add the Fan-Out/Fan-In Demo Logic

Update the `ExecuteFanOutFanInDemo` method in the **Program.cs** class with the following code:

```c#
async Task ExecuteFanOutFanInDemo()
{
	var urls = new List<string>
	{
		"https://www.updateconference.net/en/2024/speaker/rachel-appel",
		"https://www.updateconference.net/en/2024/speaker/tejas-chopra",
		"https://www.updateconference.net/en/2024/speaker/devlin-duldulao",
		"https://www.updateconference.net/en/2024/speaker/anjuli-jhakry",
		"https://www.updateconference.net/en/2024/speaker/konrad-kokosa",
		"https://www.updateconference.net/en/2024/speaker/cecilia-wir%C3%A9n",
		"https://www.updateconference.net/en/2024/speaker/damian-antonowicz",
		"https://www.updateconference.net/en/2024/speaker/roger-barreto",
		"https://www.updateconference.net/en/2024/speaker/ruby-jane-cabagnot",
		"https://www.updateconference.net/en/2024/speaker/wesley-cabus",
		"https://www.updateconference.net/en/2024/speaker/lou%C3%ABlla-creemers",
		"https://www.updateconference.net/en/2024/speaker/giorgi-dalakishvili",
		"https://www.updateconference.net/en/2024/speaker/suzanne-daniels",
		"https://www.updateconference.net/en/2024/speaker/dennis-doomen",
		"https://www.updateconference.net/en/2024/speaker/kajetan-duszy%C5%84ski",
		"https://www.updateconference.net/en/2024/speaker/loek-duys",
		"https://www.updateconference.net/en/2024/speaker/barbara-forbes",
		"https://www.updateconference.net/en/2024/speaker/adam-furmanek",
		"https://www.updateconference.net/en/2024/speaker/chad-green",
		"https://www.updateconference.net/en/2024/speaker/roland-guijt",
		"https://www.updateconference.net/en/2024/speaker/chet-husk",
		"https://www.updateconference.net/en/2024/speaker/yuliia-kovalova",
		"https://www.updateconference.net/en/2024/speaker/jan-krivanek",
		"https://www.updateconference.net/en/2024/speaker/daniel-krzyczkowski",
		"https://www.updateconference.net/en/2024/speaker/amaury-lev%C3%A9",
		"https://www.updateconference.net/en/2024/speaker/isaac-levin",
		"https://www.updateconference.net/en/2024/speaker/rockford-lhotka",
		"https://www.updateconference.net/en/2024/speaker/mabrouk-mahdhi",
		"https://www.updateconference.net/en/2024/speaker/mike-martin",
		"https://www.updateconference.net/en/2024/speaker/codrina-merigo",
		"https://www.updateconference.net/en/2024/speaker/jeremy-miller",
		"https://www.updateconference.net/en/2024/speaker/jared-parsons",
		"https://www.updateconference.net/en/2024/speaker/stefan-p%C3%B6lz",
		"https://www.updateconference.net/en/2024/speaker/al-rodriguez",
		"https://www.updateconference.net/en/2024/speaker/spencer-schneidenbach",
		"https://www.updateconference.net/en/2024/speaker/rainer-sigwald",
		"https://www.updateconference.net/en/2024/speaker/niels-tanis",
		"https://www.updateconference.net/en/2024/speaker/alex-thissen",
		"https://www.updateconference.net/en/2024/speaker/andr%C3%A1s-velv%C3%A1rt",
		"https://www.updateconference.net/en/2024/speaker/youssef-victor",
		"https://www.updateconference.net/en/2024/speaker/chris-woodruff",
		"https://www.updateconference.net/en/2024/speaker/martin-zikmund"
	};
	Tuple<bool, object?> demoResult = await ExecuteDemo("Fan-Out/Fan-In", "fan-out-fan-in", new StringContent(JsonSerializer.Serialize(urls), Encoding.UTF8, "application/json"));
	if (demoResult.Item1)
	{
		Dictionary<string, string>? results = JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(demoResult.Item2));
		if (results is not null)
		{
			Table scrappingResults = new();
			scrappingResults.Title($"Scrapping Results");
			scrappingResults.AddColumn("URL");
			scrappingResults.AddColumn("Speaker");
			foreach (var result in results)
				scrappingResults.AddRow(result.Key, result.Value.Replace(" | Update Conference Prague 2024", ""));
			AnsiConsole.Write(scrappingResults);
		}
	}
}
```

In this method, we are putting together a list of all the speaker web pages on the **Update Conf** website and then sending those to the **Fan-Out/Fan-In** orchestration so that the webpages can be scraped. Once scraped, the method displays the URLs along with the speaker.

## Section C: Locally Execute the Function Chaining Demo

Now that we have all the code written, we can execute the **Fan-Out/Fan-In** demo.

1. From Visual Studio, start the solution by clicking the **F5** key.

2. Two console windows will be opened:

   - In the first, you will see the list of functions within the function app. This includes the accessible functions (**StartWebScrapping**), the orchestrator (**FanInFanOutOrchestrator**), and the activity function (**ScrapeWebpage**).

     > If you completed any of the other labs before this one, you will also see the functions created in those labs.

   - In the second, you will see our console application which will allow you to execute the demonstrations.

3. From the **Serverless Orchestration** console window, select the **Fan-Out/Fan-In** demonstration.

4. Observe the workflow from the two console windows.

5. Once the orchestration has completed; review the list of webpages that were scraped for content.