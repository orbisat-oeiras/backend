using System.Reflection;

using backend24.Services.EventFinalizers;

using Microsoft.AspNetCore.Mvc;

namespace backend24.Controllers
{
	[ApiController]
	[Route("api/[action]")]
	public class ServerEventsController : ControllerBase
	{
		private readonly ILogger<ServerEventsController> _logger;
		private readonly IEnumerable<EventFinalizerBase<object>> _eventFinalizers;

		public ServerEventsController(ILogger<ServerEventsController> logger, IEnumerable<EventFinalizerBase<object>> eventFinalizers) { 
			_logger = logger;
			_eventFinalizers = eventFinalizers;
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
