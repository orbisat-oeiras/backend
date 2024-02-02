using backend24.Services;
using backend24.Services.DataProcessors;
using backend24.Services.DataProviders;
using backend24.Services.EventFinalizers;

namespace backend24
{
	public class Program
	{
		public static void Main(string[] args) {
			// Create a builder, using the arguments passed from the command line.
			var builder = WebApplication.CreateBuilder(args);
			// Reset logging to the console
			builder.Logging.ClearProviders();
			builder.Logging.AddConsole();

			// Add services to the container.
			// Register internal services, using keyed services
			builder.Services
				.AddSingleton<Random>()
				.AddKeyedSingleton<IDataProvider<float>, RandomProvider>(ServiceKeys.TemperatureProvider)
				.AddKeyedSingleton<IDataProvider<float>, TemperatureScaleProcessor>(ServiceKeys.TemperatureScaleProcessor, (serviceProvider, _) => {
					var tempProviderService = serviceProvider.GetKeyedService<IDataProvider<float>>(ServiceKeys.TemperatureProvider)
							   ?? throw new NullReferenceException($"Missing service of type {typeof(IDataProvider<float>)} with key {nameof(ServiceKeys.TemperatureProvider)}");
					return new TemperatureScaleProcessor(tempProviderService, 0, 100);
				})
				.AddSingleton<TemperatureFinalizer>()
				.AddSingleton(provider => {
					return (IFinalizedProvider)provider.GetRequiredService<TemperatureFinalizer>();
				});

			// This will register all classes annotated with ApiController
			builder.Services.AddControllers();
			// Set up Swagger/OpenAPI (learn more at https://aka.ms/aspnetcore/swashbuckle)
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

			// Build an app from the configuration.
			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if(app.Environment.IsDevelopment()) {
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseHttpsRedirection();
			app.UseAuthorization(); // TODO: Research this - is it necessary?
			app.MapControllers();

			// Start the providers
			(app.Services.GetKeyedService<IDataProvider<float>>(ServiceKeys.TemperatureProvider) as RandomProvider)?.Start(1000);

			// Start the app.
			app.Run();
		}
	}
}