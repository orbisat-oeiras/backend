﻿namespace backend24.Extensions
{
	public static class ASPExtensions
	{
		/// <summary>
		/// Add a finalizer service to an IServiceCollection.
		/// </summary>
		/// <typeparam name="TFinalizer">Type of the finalizer to be added</typeparam>
		/// <param name="services"></param>
		/// <returns></returns>
		public static IServiceCollection AddFinalizer<TFinalizer>(this IServiceCollection services) where TFinalizer : class, IFinalizedProvider {
			// Two singletons are added to expose the same object as both a TFinalizer, whatever it might be, and an IFinalizedProvider
			// At least I think that's the reason...
			return services.AddSingleton<TFinalizer>().AddSingleton(provider => (IFinalizedProvider)provider.GetRequiredService<TFinalizer>());
		}

		/// <summary>
		/// Write an object encoded as JSON to an HttpResponse
		/// </summary>
		/// <param name="response"></param>
		/// <param name="value">The object to be encoded and written</param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public static Task WriteJSONAsync(this HttpResponse response, object value, CancellationToken cancellationToken = default) {
			// WriteAsync does this, so it's done here too
			ArgumentNullException.ThrowIfNull(response);
			ArgumentNullException.ThrowIfNull(value);

			return response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(value), cancellationToken);
		}
	}
}
