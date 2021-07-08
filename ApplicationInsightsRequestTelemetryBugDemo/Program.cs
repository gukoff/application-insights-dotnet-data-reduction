namespace ApplicationInsightsDataROI
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.ApplicationInsights.Extensibility;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var token = new CancellationTokenSource().Token;
            await RunAsync(token);
        }

        /// <summary/>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        private static async Task RunAsync(CancellationToken token)
        {
            var configuration = new TelemetryConfiguration
            {
                ConnectionString = "YOUR_CONNECTION_STRING",
            };

            // automatically collect dependency calls
            var dependencies = new DependencyTrackingTelemetryModule();
            dependencies.Initialize(configuration);

            // automatically correlate all telemetry data with request
            configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());

            var client = new TelemetryClient(configuration);

            var iteration = 0;
            var http = new HttpClient();

            while (!token.IsCancellationRequested)
            {
                // After running the app, search AppInsights for operations with this id:
                using var operation = client.StartOperation<RequestTelemetry>("Experiment1");
                operation.Telemetry.Properties["Iteration"] = iteration.ToString();

                client.TrackEvent("IterationStarted", properties: new Dictionary<string, string> { { "iteration", iteration.ToString() } });
                client.TrackTrace($"Iteration {iteration} started", SeverityLevel.Information);

                // Do some work
                await http.GetStringAsync("http://bing.com");
                await Task.Delay(10000, token);

                switch (iteration % 3)
                {
                    case 0:
                        // successful operation - gets into AppInsights
                        break;
                    case 1:
                        // BUG!
                        // unsuccessful operation w/o response code - doesn't get into AppInsights
                        // BUG!
                        operation.Telemetry.Success = false;
                        break;
                    case 2:
                        // unsuccessful operation with response code - gets into AppInsights
                        operation.Telemetry.Success = false;
                        operation.Telemetry.ResponseCode = "500";
                        break;
                }

                Console.WriteLine($"Iteration {iteration}");
                iteration++;
            }
        }
    }
}
