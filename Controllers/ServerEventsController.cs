using System.Reflection;

using backend24.Extensions;
using backend24.Services.DataProviders;
using backend24.Services.EventFinalizers;

using Microsoft.AspNetCore.Mvc;

namespace backend24.Controllers
{
	/// <summary>
	/// Controller for handling server-sent events.
	/// </summary>
	[ApiController]
	[Route("api/[action]")]
	public class ServerEventsController : ControllerBase
	{
		// Logger provided by DI, used for printing information to all logging providers at once
		private readonly ILogger<ServerEventsController> _logger;
		// List of registered event finalizers, which provide ready-to-send events
		private readonly IEnumerable<IFinalizedProvider> _eventFinalizers;

		/// <summary>
		/// Create a new instance of ServerEventsController
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="eventFinalizers"></param>
		public ServerEventsController(ILogger<ServerEventsController> logger, IEnumerable<IFinalizedProvider> eventFinalizers) { 
			_logger = logger;
			_eventFinalizers = eventFinalizers;
			_logger.LogInformation("DI provided {evtFinalizerCount} event finalizers.", _eventFinalizers.Count().ToString());
		}

		/// <summary>
		/// Provide a GET endpoint for connecting to the SSE channel
		/// </summary>
		/// <returns>Good question</returns>
		[HttpGet()]
		public async Task SSE() {
			// Set the response headers; this tells the client we're initiating SSE
			Response.Headers.ContentType = "text/event-stream";
			Response.Headers.CacheControl = "no-cache";
			Response.Headers.Connection = "keep-alive";

            foreach (var eventFinalizer in _eventFinalizers)
            {
				// Subscribe to finalizers
				eventFinalizer.OnDataProvided += async payload => {
					// Leaving these here just in case...
					//_logger.LogInformation("Sending event provided by {evtFinalizerType}.", eventFinalizer.GetType().Name);
					//_logger.LogDebug("Tag: {tag}\nContent: {content}", payload.Data.tag, payload.Data.content);

					// Send the tagged event in a properly formatted way
					await Response.WriteAsync($"event: {payload.Data.tag}\n");
					await Response.WriteAsync($"data: ");
					// Convert the content to JSON
					await Response.WriteJSONAsync(payload.Data.content);
					await Response.WriteAsync("@");
					await Response.WriteJSONAsync(payload.DataStamp);
					await Response.WriteAsync("\n\n");
					await Response.Body.FlushAsync();
				};
            }

			// Keep the server alive
			// This feels very dodgy
            while (true) {
				await Task.Delay(1000);
			}
		}
	}
}
