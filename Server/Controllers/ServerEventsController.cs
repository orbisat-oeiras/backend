using backend.Library.Extensions;
using backend.Library.Services.EventFinalizers;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace backend.Server.Controllers
{
    /// <summary>
    /// Controller for handling server-sent events.
    /// </summary>
    [ApiController]
    [Route("api/[action]")]
    [EnableCors]
    public class ServerEventsController : ControllerBase
    {
        // Logger provided by DI, used for printing information to all logging providers at once
        private readonly ILogger<ServerEventsController> _logger;

        // List of registered event finalizers, which provide ready-to-send events
        private readonly IEnumerable<IFinalizedProvider> _eventFinalizers;

        // Cancellation token for graceful shutdown
        // This is used to stop the SSE loop when the server is shutting down
        private readonly CancellationToken cancellationToken = Program.ShutdownTokenSource.Token;

        /// <summary>
        /// Create a new instance of ServerEventsController
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="eventFinalizers"></param>
        public ServerEventsController(
            ILogger<ServerEventsController> logger,
            IEnumerable<IFinalizedProvider> eventFinalizers
        )
        {
            _logger = logger;
            _eventFinalizers = eventFinalizers;
            _logger.LogInformation(
                "DI provided {evtFinalizerCount} event finalizers.",
                _eventFinalizers.Count().ToString()
            );
        }

        /// <summary>
        /// Provide a GET endpoint for connecting to the SSE channel
        /// </summary>
        /// <returns>Good question</returns>
        [HttpGet()]
        public async Task SSE()
        {
            // Set the response headers; this tells the client we're initiating SSE
            Response.Headers.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";

            foreach (IFinalizedProvider eventFinalizer in _eventFinalizers)
            {
                // Subscribe to finalizers
                eventFinalizer.OnDataProvided += async payload =>
                {
                    try
                    {
                        // Leaving these here just in case...
                        //_logger.LogInformation("Sending event provided by {evtFinalizerType}.", eventFinalizer.GetType().Name);
                        // _logger.LogInformation(
                        //     "Tag: {tag}\nContent: {content}",
                        //     payload.Data.tag,
                        //     payload.Data.content
                        // );

                        // Send the tagged event in a properly formatted way
                        await Response.WriteAsync($"event: {payload.Data.tag}\n");
                        await Response.WriteAsync($"data: ");
                        // Convert the content to JSON
                        await Response.WriteJSONAsync(payload.Data.content);
                        await Response.WriteAsync("@");
                        await Response.WriteJSONAsync(payload.DataStamp);
                        await Response.WriteAsync("\n\n");
                        await Response.Body.FlushAsync();
                    }
                    catch (ObjectDisposedException)
                    {
                        // _logger.LogWarning(
                        //     "Error in SSE connection. The client may have disconnected."
                        // );
                    }
                };
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Wait infinetely for the cancellation token to be triggered
                    await Task.Delay(-1, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Exit the loop
                    break;
                }
            }
        }
    }
}
