namespace backend24.Extensions
{
	public static class ASPExtensions
	{
		public static IServiceCollection AddFinalizer<TFinalizer>(this IServiceCollection services) where TFinalizer : class, IFinalizedProvider {
			return services.AddSingleton<TFinalizer>().AddSingleton(provider => (IFinalizedProvider)provider.GetRequiredService<TFinalizer>());
		}

		public static Task WriteJSONAsync(this HttpResponse response, object value, CancellationToken cancellationToken = default(CancellationToken)) {
			ArgumentNullException.ThrowIfNull(response);
			ArgumentNullException.ThrowIfNull(value);

			return response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(value), cancellationToken);
		}
	}
}
