using System.IO.Ports;
using backend24.Extensions;
using backend24.Services;
using backend24.Services.DataProcessors;
using backend24.Services.DataProcessors.DataExtractors;
using backend24.Services.DataProviders;
using backend24.Services.EventFinalizers;
using NReco.Logging.File;

namespace backend24
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create a builder, using the arguments passed from the command line.
            var builder = WebApplication.CreateBuilder(args);
            // Reset logging to the console
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddFile("Logs/log.txt");
            // Get the name of the serial port where data is arriving
            Console.WriteLine(
                "Enter the name of the serial port where the APC220 module is connected.\nAvailable ports are:"
            );
            Console.Write(
                string.Concat(SerialPort.GetPortNames().Select(x => "\t" + x + "\n")) + "> "
            );
            string serialPortName = Console.ReadLine()!;

            // Add services to the container.
            // Register internal services, using keyed services
            builder
                .Services.AddKeyedSingleton<
                    IDataProvider<Dictionary<SerialProvider.DataLabel, string>>,
                    SerialProvider
                >(
                    ServiceKeys.SerialProvider,
                    (serviceProvider, _) =>
                        ActivatorUtilities.CreateInstance<SerialProvider>(
                            serviceProvider,
                            serialPortName,
                            19200,
                            Parity.None
                        )
                )
                .AddKeyedSingleton<IDataProvider<float>, PressureExtractor>(
                    ServiceKeys.PressureExtractor
                )
                .AddFinalizer<PressureFinalizer>()
                .AddKeyedSingleton<IDataProvider<float>, TemperatureExtractor>(
                    ServiceKeys.TemperatureExtractor
                )
                .AddFinalizer<TemperatureFinalizer>()
                .AddKeyedSingleton<IDataProvider<float>, AltitudeExtractor>(
                    ServiceKeys.AltitudeExtractor
                )
                .AddFinalizer<AltitudeFinalizer>()
                .AddKeyedSingleton<IDataProvider<float>, AltitudeGPSExtractor>(
                    ServiceKeys.AltitudeGPSExtractor
                )
                .AddFinalizer<AltitudeGPSFinalizer>()
                .AddKeyedSingleton<IDataProvider<float>, AltitudeDeltaProcessor>(
                    ServiceKeys.AltitudeDeltaProcessor
                )
                .AddFinalizer<AltitudeDeltaFinalizer>()
                .AddKeyedSingleton<IDataProvider<float>, VelocityProcessor>(
                    ServiceKeys.VelocityProcessor
                )
                .AddFinalizer<VelocityFinalizer>();

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
            var app = builder.Build();

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
