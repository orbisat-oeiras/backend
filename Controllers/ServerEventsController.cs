using System.Reflection;

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
		// List of registered event finalizers, which provided ready-to-send events
		private readonly IEnumerable<IFinalizedProvider> _eventFinalizers;

		public ServerEventsController(ILogger<ServerEventsController> logger, IEnumerable<IFinalizedProvider> eventFinalizers) { 
			_logger = logger;
			_eventFinalizers = eventFinalizers;
			_logger.LogInformation("DI provided {evtFinalizerCount} event finalizers.", _eventFinalizers.Count().ToString());
		}

		[HttpGet()]
		public async Task SSE() {
			Response.Headers["Content-Type"] = "text/event-stream";
			Response.Headers["Cache-Control"] = "no-cache";
			Response.Headers["Connection"] = "keep-alive";

			while(true) {
				await Response.WriteAsync($"data: Controller at {DateTime.Now}\r\r");
				await Response.Body.FlushAsync();
				await Task.Delay(1000);
			}
		}
	}
}
