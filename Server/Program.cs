using System.IO.Ports;
using System.Runtime.CompilerServices;
using backend.Library.Extensions;
using backend.Library.Services;
using backend.Library.Services.DataProcessors;
using backend.Library.Services.DataProcessors.Analyzers;
using backend.Library.Services.DataProcessors.DataExtractors;
using backend.Library.Services.DataProviders;
using backend.Library.Services.EventFinalizers;
using backend.Server.Controllers;
using Microsoft.Extensions.Logging.Console;
using NReco.Logging.File;
using Spectre.Console;

namespace backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create a builder, using the arguments passed from the command line.
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            // Reset logging to the console
            builder.Logging.ClearProviders();
            builder.Logging.AddSimpleConsole(options =>
            {
                options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss]";
            });
            builder.Logging.AddFile($"Logs/log{DateTimeOffset.UtcNow:yyyy-MM-dd-HH-mm-ss}");

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                AnsiConsole.MarkupLine("[red]Shutting down...[/]");
                Environment.Exit(0);
            };

            if (args.Length > 1 && args[0] == "--read-file")
            {
                builder.Services.AddKeyedSingleton<
                    IDataProvider<Dictionary<SerialProvider.DataLabel, byte[]>>,
                    FileAnalyser
                >(ServiceKeys.DataProvider);
                // Register all the internal services, no idea if this is the best practice but uhh
                // it works

                // TODO: See if I can just use the same services as the serial provider

                // This shouldn't really be a problem, because the person who is analysing a file
                // isn't really expected to be using the serial data.

                builder
                    .Services.AddKeyedSingleton<IDataProvider<float>, PressureExtractor>(
                        ServiceKeys.PressureExtractor
                    )
                    .AddFinalizer<PressureFinalizer>();

                builder
                    .Services.AddKeyedSingleton<IDataProvider<float>, HumidityExtractor>(
                        ServiceKeys.HumidityExtractor
                    )
                    .AddFinalizer<HumidityFinalizer>();

                builder
                    .Services.AddKeyedSingleton<IDataProvider<float>, TemperatureExtractor>(
                        ServiceKeys.TemperatureExtractor
                    )
                    .AddFinalizer<TemperatureFinalizer>();

                builder
                    .Services.AddKeyedSingleton<IDataProvider<float>, AltitudeExtractor>(
                        ServiceKeys.AltitudeExtractor
                    )
                    .AddFinalizer<AltitudeFinalizer>();

                builder
                    .Services.AddKeyedSingleton<IDataProvider<float>, VelocityProcessor>(
                        ServiceKeys.VelocityProcessor
                    )
                    .AddFinalizer<VelocityFinalizer>();

                builder.Services.AddHostedService<CsvController>();

                WebApplication fileApp = builder.Build();

                fileApp.Start();

                if (
                    fileApp.Services.GetKeyedService<
                        IDataProvider<Dictionary<SerialProvider.DataLabel, byte[]>>
                    >(ServiceKeys.DataProvider)
                    is not FileAnalyser fileAnalyser
                )
                {
                    Console.WriteLine("Failed to resolve FileAnalyser.");
                    return;
                }

                string filepath = args[1];

                if (!File.Exists(filepath))
                {
                    Console.WriteLine($"File {filepath} does not exist.");
                    return;
                }

                fileAnalyser.AnalyseFileContents(filepath);

                Console.WriteLine($"File {filepath} processed successfully.");
            }
            // Get the name of the serial port where data is arriving
            string serialPortName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(
                        "Please select the serial port where the [blue]APC220 module[/] is connected."
                    )
                    .PageSize(10)
                    .AddChoices(SerialPort.GetPortNames())
                    .AddChoices("Mock Serial Data")
                    .HighlightStyle(new Style(foreground: Color.White, background: Color.Blue))
            );
            Console.WriteLine($"Selected port: {serialPortName}");
            // Add services to the container.
            // Register internal services, using keyed services
            if (serialPortName == "Mock Serial Data")
            {
                builder.Services.AddKeyedSingleton<
                    IDataProvider<Dictionary<SerialProvider.DataLabel, string>>,
                    MockDataProvider
                >(ServiceKeys.DataProvider);
            }
            else
            {
                builder.Services.AddKeyedSingleton<
                    IDataProvider<Dictionary<SerialProvider.DataLabel, byte[]>>,
                    SerialProvider
                >(
                    ServiceKeys.DataProvider,
                    (serviceProvider, _) =>
                        ActivatorUtilities.CreateInstance<SerialProvider>(
                            serviceProvider,
                            serialPortName,
                            19200,
                            Parity.None
                        )
                );
            }
            builder
                .Services.AddKeyedSingleton<IDataProvider<float>, PressureExtractor>(
                    ServiceKeys.PressureExtractor
                )
                .AddFinalizer<PressureFinalizer>();

            builder
                .Services.AddKeyedSingleton<IDataProvider<float>, HumidityExtractor>(
                    ServiceKeys.HumidityExtractor
                )
                .AddFinalizer<HumidityFinalizer>();

            builder
                .Services.AddKeyedSingleton<IDataProvider<float>, TemperatureExtractor>(
                    ServiceKeys.TemperatureExtractor
                )
                .AddFinalizer<TemperatureFinalizer>();

            builder
                .Services.AddKeyedSingleton<IDataProvider<float>, AltitudeExtractor>(
                    ServiceKeys.AltitudeExtractor
                )
                .AddFinalizer<AltitudeFinalizer>();
            builder
                .Services.AddKeyedSingleton<IDataProvider<float>, AccelerationZExtractor>(
                    ServiceKeys.AccelerationZExtractor
                )
                .AddFinalizer<AccelerationZFinalizer>();
            builder
                .Services.AddKeyedSingleton<IDataProvider<float>, AccelerationXExtractor>(
                    ServiceKeys.AccelerationXExtractor
                )
                .AddFinalizer<AccelerationXFinalizer>();
            builder
                .Services.AddKeyedSingleton<IDataProvider<float>, AccelerationYExtractor>(
                    ServiceKeys.AccelerationYExtractor
                )
                .AddFinalizer<AccelerationYFinalizer>();

            builder
                .Services.AddKeyedSingleton<IDataProvider<float>, AltitudeGPSExtractor>(
                    ServiceKeys.AltitudeGPSExtractor
                )
                .AddFinalizer<AltitudeGPSFinalizer>();

            builder
                .Services.AddKeyedSingleton<IDataProvider<float>, AltitudeDeltaProcessor>(
                    ServiceKeys.AltitudeDeltaProcessor
                )
                .AddFinalizer<AltitudeDeltaFinalizer>();

            builder
                .Services.AddKeyedSingleton<IDataProvider<float>, VelocityProcessor>(
                    ServiceKeys.VelocityProcessor
                )
                .AddFinalizer<VelocityFinalizer>();

            builder.Services.AddHostedService<CsvController>();
            // This will register all classes annotated with ApiController
            builder.Services.AddControllers();
            // Set up Swagger/OpenAPI (learn more at https://aka.ms/aspnetcore/swashbuckle)
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddCors(options =>
                options.AddDefaultPolicy(policy =>
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
                )
            );

            // Build an app from the configuration.
            WebApplication app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors();
            app.UseAuthorization(); // TODO: Research this - is it necessary?
            app.MapControllers();

            // Start the app.
            app.Run();
        }
    }
}
