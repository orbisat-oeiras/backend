namespace backend24
{
	public class Program
	{
		public static void Main(string[] args) {
			// Create a builder, using the arguments passed from the command line.
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
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
			app.UseAuthorization();
			app.MapControllers();

			// Start the app.
			app.Run();
		}
	}
}