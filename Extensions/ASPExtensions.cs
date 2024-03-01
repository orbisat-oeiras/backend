using backend24.Services.EventFinalizers;

namespace backend24.Extensions
{
	public static class ASPExtensions
	{
		public static IServiceCollection AddFinalizer<TFinalizer>(this IServiceCollection services) where TFinalizer : class, IFinalizedProvider {
			return services.AddSingleton<TFinalizer>().AddSingleton(provider => (IFinalizedProvider)provider.GetRequiredService<TFinalizer>());
		}
	}
}
