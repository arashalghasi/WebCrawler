using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace SimpleWebScraper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();  // Create a new configuration builder.

            buildConfig(builder);  // Configure the builder using the buildConfig method defined earlier.

            // Configure the Serilog logger with the configuration from the builder.
            // It reads the logging configuration from the builder's configuration.
            // It enriches the log context, writes logs to the console, and creates the logger.
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Build())  // Read logging configuration from the builder.
                .Enrich.FromLogContext()  // Enrich log events with context information.
                .WriteTo.Console()  // Write log events to the console.
                .CreateLogger();  // Create the Serilog logger.

            Log.Logger.Information("Application Starting!");  // Log an information message.

            // Create a host for the application using Host.CreateDefaultBuilder().
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Configure services to be used in the application.
                    services.AddTransient<IWebScraper, WebScraper>();  // Register GreenService as a transient service.
                })
                .UseSerilog()  // Use the Serilog logger for logging.
                .Build();  // Build the host.

            // The provided code sets up configuration, logging, and service registration for an application.


            // Create an instance of the GreenService class using dependency injection.
            // The host.Services provides access to the application's registered services.
            var svc = ActivatorUtilities.CreateInstance<WebScraper>(host.Services);

            // Call the Run method of the GreenService instance.
            svc.Run();


        }

        static void buildConfig(IConfigurationBuilder builder)
        {
            // Set the base path for configuration sources to the current directory.
            builder.SetBasePath(Directory.GetCurrentDirectory())

            // Add a mandatory JSON configuration file: "appsettings.json".
            // If the file doesn't exist, it will throw an exception.
            // The configuration will automatically reload if the file changes.
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)

            // Add an environment-specific JSON configuration file based on the
            // "ASPNETCORE_ENVIRONMENT" environment variable. If the variable is not set,
            // it defaults to "Production". This file is optional, and no exception will
            // be thrown if it doesn't exist.
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)

            // Add environment variables as a configuration source, allowing access to
            // configuration values stored in environment variables.
            .AddEnvironmentVariables();
        }

    }
}