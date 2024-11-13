using Azure.Data.Tables;
using Azure.Identity;
using ConsoleApp;
using ConsoleApp.Responses;
using Spectre.Console;
using System.Text;
using System.Text.Json;

using HttpClient _httpClient = new();
string _functionAppUrl = "http://localhost:7071/orchestrations/";
int _waitTimeInSeconds = 2;
string _tableStorageEndpointOrConnectionString = "UseDevelopmentStorage=true";
string _telemetryTableName = "ServerlessOrchestrationTelemetry";
string _environment = "local";

string demoSelection = string.Empty;
do
{

	Console.Clear();

	AnsiConsole.Write(new FigletText("Serverless Orchestration").Centered().Color(Color.Green));

	demoSelection = AnsiConsole.Prompt(
		new SelectionPrompt<string>()
			.Title("Which demo do you want to run?")
			.PageSize(10)
			.MoreChoicesText("[grey](Move up and down to reveal more demos)[/]")
			.AddChoices([
				"Function Chaining",
			"Fan-Out/Fan-In",
			"Async HTTP API",
			"Monitoring",
			"Human Interaction",
			"Aggregator (Stateful Entities)",
			"Exit Demo App"]
			));

	switch (demoSelection)
	{
		case "Function Chaining":
			await ExecuteFunctionChainingDemo();
			break;
		case "Fan-Out/Fan-In":
			await ExecuteFanOutFanInDemo();
			break;
		case "Async HTTP API":
			await ExecuteAsyncHttpApisDemo();
			break;
		case "Monitoring":
			await ExecuteMonitoringDemo();
			break;
		case "Human Interaction":
			await ExecuteHumanInteractionDemo();
			break;
		case "Aggregator (Stateful Entities)":
			await ExecuteAggregatorDemo();
			break;
		case "Exit Demo App":
			break;
		default:
			break;
	}

	if (demoSelection != "Exit Demo App")
	{
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[yellow]Press any key to return to the main menu...[/]");
		Console.ReadKey(true);
	}

} while (demoSelection != "Exit Demo App");

async Task ExecuteFunctionChainingDemo()
{
	Tuple<bool, object?> demoResult = await ExecuteDemo("Function Chaining", "function-chaining");
	if (demoResult.Item1)
	{
		string? result = JsonSerializer.Deserialize<string>(JsonSerializer.Serialize(demoResult.Item2));
		if (result is not null)
			AnsiConsole.MarkupLine($"[gray]Report URL:[/] {result}");
		else
			AnsiConsole.MarkupLine("[red]Failed to get the report URL[/]");
	}
}

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

async Task ExecuteAsyncHttpApisDemo()
{
	string operationId = Guid.NewGuid().ToString();
	StringContent requestBody = new(JsonSerializer.Serialize(new { operationId }), Encoding.UTF8, "application/json");
	await ExecuteDemo("Async HTTP API", "async-http-api", requestBody);
}

async Task ExecuteMonitoringDemo()
{
	await ExecuteMonitoringDemoRunner(["https://updateconference.net"]);
}

async Task ExecuteHumanInteractionDemo()
{
	AnsiConsole.MarkupLine("[red]The [/][bold pink1]Human Interaction[/] [red]demo has not been implemented yet[/]");
}

async Task ExecuteAggregatorDemo()
{
	AnsiConsole.MarkupLine("[red]The [/][bold pink1]Aggregator (Stateful Entities)[/] [red]demo has not been implemented yet[/]");
}

async Task<Tuple<bool, object?>> ExecuteDemo(
	string demoName,
	string demoRoute,
	StringContent? requestBody = null,
	string? queryString = null)
{

	Tuple<bool, object?> result = new(false, null);

	try
	{

		Table demoResulsTable = new();
		demoResulsTable.HideHeaders();

		await AnsiConsole.Live(demoResulsTable)
			.StartAsync(async ctx =>
			{
				demoResulsTable.Title($"{demoName} Demo").LeftAligned();
				ctx.Refresh();
				demoResulsTable.AddColumn("Orchestration Status");
				demoResulsTable.Columns[0].NoWrap();
				ctx.Refresh();
				demoResulsTable.AddRow("Starting the orchestration...");
				ctx.Refresh();

				Uri httpStartUri = new($"{_functionAppUrl}{demoRoute}");
				if (!string.IsNullOrEmpty(queryString))
					httpStartUri = new Uri($"{httpStartUri}?{queryString}");
				HttpResponseMessage response = await _httpClient.PostAsync(httpStartUri, requestBody);
				string responseBody = await response.Content.ReadAsStringAsync();

				if (!string.IsNullOrEmpty(responseBody))
				{
					DurableOrchestrationStartedResponse? statusQueryResult = JsonSerializer.Deserialize<DurableOrchestrationStartedResponse>(responseBody);
					if (statusQueryResult != null)
					{

						string statusQueryUri = statusQueryResult.StatusQueryGetUri;
						demoResulsTable.AddEmptyRow();
						demoResulsTable.AddRow(new Markup($"[green]Orchestration started.[/] [darkviolet]Orchestration ID:[/] [purple_1]{statusQueryResult.Id}[/]"));
						ctx.Refresh();

						object? output = await CheckStatusAsync(statusQueryUri, demoResulsTable, ctx);
						demoResulsTable.AddEmptyRow();
						demoResulsTable.AddRow(new Markup($"[green]Orchestration completed.[/]"));
						ctx.Refresh();

						result = new(true, output);
					}
					else
					{
						demoResulsTable.AddEmptyRow();
						demoResulsTable.AddRow(new Markup("[red]Failed to start the orchestration[/]"));
						ctx.Refresh();
						result = new(false, null);
					}
				}
				else
				{
					demoResulsTable.AddEmptyRow();
					demoResulsTable.AddRow(new Markup("[red]Failed to start the orchestration[/]"));
					ctx.Refresh();
					result = new(false, null);
				}

			});


		AnsiConsole.WriteLine();
		return result;

	}
	catch (Exception ex)
	{
		AnsiConsole.WriteException(ex);
		return new(false, null);
	}
}

async Task<object?> CheckStatusAsync(string statusQueryUri, Table table, LiveDisplayContext ctx)
{
	while (true)
	{
		string response = await _httpClient.GetStringAsync(statusQueryUri);
		DurableOrchestrationStatusReponse? status = JsonSerializer.Deserialize<DurableOrchestrationStatusReponse>(response);

		if (status is null)
		{
			throw new Exception("Failed to get orchestration status");
		}
		else
		{

			if (status.RuntimeStatus == "Completed")
				return status.Output;
			else if (status.RuntimeStatus == "Failed" || status.RuntimeStatus == "Terminated")
				throw new Exception("Orchestration failed or was terminated");

			table.AddEmptyRow();
			table.AddRow(new Markup($"[green]Orchestration status:[/] {status.RuntimeStatus}. [gray]Checking again in {_waitTimeInSeconds} seconds...[/]"));
			ctx.Refresh();
			await Task.Delay(_waitTimeInSeconds * 1000);

		}
	}
}

async Task ExecuteMonitoringDemoRunner(List<string> urlsToMonitor)
{

	string demoName = "Monitoring";
	string demoRoute = "monitor";

	string? queryString = null;
	StringContent? requestBody = null;
	if (urlsToMonitor.Count > 1)
		requestBody = new StringContent(JsonSerializer.Serialize(urlsToMonitor), Encoding.UTF8, "application/json");
	else
		queryString = "url=https://chadgreen.com";

	try
	{
		bool webMonitoringStarted = await StartWebMonitoringAsync(_httpClient, _functionAppUrl, demoName, demoRoute, queryString, requestBody);
		AnsiConsole.WriteLine();
		if (webMonitoringStarted)
			await MonitorTelemtryAsync(urlsToMonitor, _environment);
	}
	catch (Exception ex)
	{
		AnsiConsole.WriteException(ex);
	}

	async Task MonitorTelemtryAsync(List<string> urlsToMonitor, string _environment)
	{
		Table monitoringResultsTable = new();

		TableClient tableClient = GetTableClient();

		await AnsiConsole.Live(monitoringResultsTable)
			.StartAsync(async ctx =>
			{
				monitoringResultsTable.Title($"Web Monitoring Results").LeftAligned();
				ctx.Refresh();
				monitoringResultsTable.AddColumn("Monitored URL");
				monitoringResultsTable.AddColumn("Is Up");
				monitoringResultsTable.AddColumn("Status Code");
				monitoringResultsTable.AddColumn("Response Time (ms)");
				ctx.Refresh();
				await Task.Delay(TimeSpan.FromSeconds(30));
				while (true)
				{
					foreach (string urlToMonitor in urlsToMonitor)
					{
						TelemetryTableEntity telemetryData = GetLatestTelemetryData(tableClient, urlToMonitor)!;
						if (telemetryData is not null)
						{
							monitoringResultsTable.AddEmptyRow();
							monitoringResultsTable.AddRow(urlToMonitor, telemetryData.IsUp.ToString(), telemetryData.StatusCode ?? string.Empty, telemetryData.ResponseTimeMs.ToString());
							ctx.Refresh();
						}
					}
					await Task.Delay(TimeSpan.FromMinutes(5));
				}
			});
	}
}

static TelemetryTableEntity? GetLatestTelemetryData(TableClient tableClient, string url)
{
	Uri uri = new(url);
	Azure.Pageable<TelemetryTableEntity> queryResults = tableClient.Query<TelemetryTableEntity>(entity => entity.PartitionKey == uri.Host);
	return queryResults.OrderByDescending(e => e.Timestamp).FirstOrDefault();
}

TableClient GetTableClient()
{
	DefaultAzureCredential credential = new();
	TableServiceClient tableServiceClient;
	if (!_environment.Equals("local", StringComparison.InvariantCultureIgnoreCase))
		tableServiceClient = new(new Uri(_tableStorageEndpointOrConnectionString), credential);
	else
		tableServiceClient = new TableServiceClient(_tableStorageEndpointOrConnectionString);
	tableServiceClient.CreateTableIfNotExists(_telemetryTableName); // In a real-world application, you should throw an exception if the table does not already exists.
	return tableServiceClient.GetTableClient(_telemetryTableName);
}

static async Task<bool> StartWebMonitoringAsync(HttpClient _httpClient, string _functionAppUrl, string demoName, string demoRoute, string? queryString, StringContent? requestBody)
{
	bool webMonitoringStarted = false;

	Table demoResulsTable = new();
	demoResulsTable.HideHeaders();

	await AnsiConsole.Live(demoResulsTable)
		.StartAsync(async ctx =>
		{
			demoResulsTable.Title($"{demoName} Demo").LeftAligned();
			ctx.Refresh();
			demoResulsTable.AddColumn("Orchestration Status");
			demoResulsTable.Columns[0].NoWrap();
			ctx.Refresh();
			demoResulsTable.AddRow("Starting the orchestration...");
			ctx.Refresh();

			Uri httpStartUri = new($"{_functionAppUrl}{demoRoute}");
			if (!string.IsNullOrEmpty(queryString))
				httpStartUri = new Uri($"{httpStartUri}?{queryString}");
			HttpResponseMessage response = await _httpClient.PostAsync(httpStartUri, requestBody);
			string responseBody = await response.Content.ReadAsStringAsync();

			if (!string.IsNullOrEmpty(responseBody))
			{
				DurableOrchestrationStartedResponse? statusQueryResult = JsonSerializer.Deserialize<DurableOrchestrationStartedResponse>(responseBody);
				if (statusQueryResult != null)
				{

					string statusQueryUri = statusQueryResult.StatusQueryGetUri;
					demoResulsTable.AddEmptyRow();
					demoResulsTable.AddRow(new Markup($"[green]Orchestration started.[/] [darkviolet]Orchestration ID:[/] [purple_1]{statusQueryResult.Id}[/]"));
					ctx.Refresh();
					webMonitoringStarted = true;
				}
				else
				{
					demoResulsTable.AddEmptyRow();
					demoResulsTable.AddRow(new Markup("[red]Failed to start the orchestration[/]"));
					ctx.Refresh();
				}
			}
			else
			{
				demoResulsTable.AddEmptyRow();
				demoResulsTable.AddRow(new Markup("[red]Failed to start the orchestration[/]"));
				ctx.Refresh();
			}

		});
	return webMonitoringStarted;
}