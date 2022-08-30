using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SampleApplicationMetricsSource.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }
        static ActivitySource myActivitySource = new ActivitySource("TestWebApplication");

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            using var activity = myActivitySource.StartActivity("SayHello");
            activity?.SetTag("foo", 1);
            activity?.SetTag("bar", "Hello, World!");
            activity?.SetTag("baz", new int[] { 1, 2, 3 });

            var prevActivity = Activity.Current;
            Activity.Current = null;
            using var activity2 = myActivitySource.StartActivity("NewTypeOf", ActivityKind.Internal, string.Empty);
            activity2?.SetTag("KeyCustom", 1);
            activity2?.SetCustomProperty("transaction.type", "incrementStuff");
            activity2?.SetTag("transaction.type", "internalMac");
            await Task.Delay(200);
            activity2?.Dispose();
            


            Activity.Current = prevActivity;

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}