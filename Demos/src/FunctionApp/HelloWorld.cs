using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp;

public class HelloWorld(ILogger<HelloWorld> logger)
{
	private readonly ILogger<HelloWorld> _logger = logger;

	[Function("HelloWorld")]
	public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "hello-world")] HttpRequest request)
	{
		_logger.LogInformation("C# HTTP trigger function processed a request.");
		return new OkObjectResult("Welcome to Azure Functions!");
	}
}
