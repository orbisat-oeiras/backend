﻿using System.Text.Json;
using backend.Library.Extensions;
using backend.Library.Models;
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
        public async Task SSEAsync()
        {
            // Set the response headers; this tells the client we're initiating SSE
            Response.Headers.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";

            var eventHandlers = new List<Action<EventData<(string tag, object content)>>>();

            try
            {
                foreach (IFinalizedProvider eventFinalizer in _eventFinalizers)
                {
                    async void handler(EventData<(string tag, object content)> payload)
                    {
                        // Leaving these here just in case...
                        //_logger.LogInformation("Sending event provided by {evtFinalizerType}.", eventFinalizer.GetType().Name);
                        _logger.LogInformation(
                            "Tag: {tag}\nContent: {content}",
                            payload.Data.tag,
                            payload.Data.content
                        );

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
                    eventFinalizer.OnDataProvided += handler;
                    eventHandlers.Add(handler);
                }
                while (!HttpContext.RequestAborted.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                }
            }
            finally
            {
                foreach (IFinalizedProvider eventFinalizer in _eventFinalizers)
                {
                    foreach (
                        Action<EventData<(string tag, object content)>> handler in eventHandlers
                    )
                    {
                        eventFinalizer.OnDataProvided -= handler;
                    }
                }
            }
        }
    }
}
