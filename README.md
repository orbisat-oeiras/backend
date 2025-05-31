## Project structure

- Controllers: classes derived from `ControllerBase`, implementing API endpoints
  - `ServerEventsController`: responsible for opening an SSE channel and sending tagged data to the client. Provides the following endpoint for subscribing to SSE: `api/sse`. All data sent will be collected from objects marked with `EventFinalizerAttribute`, which will be automatically registered at startup.
- Models: classes defining data models for the app to work with
  - `GPSCoords`: represents GPS coordinates (latitude, longitude, altitude)
  - `DataStamp`: general metadata attached to all data sent, e.g., timestamp and coordinates
  - `EventData`: wrapper for data flowing through the app, packs the main data with a `DataStamp` object
- Services: Modular classes implementing internal functionality
  - `DataProviders`: classes responsible for emitting data. All data providers must implement `IDataProvider`. A consumer class can subscribe to a provider's `OnDataProvided` to be notified whenever new data is available.
    - `RandomProvider`: provides random floats on a configurable interval.
    - `SerialProvider`: provides string arrays from a serial port.
  - `DataProcessors`: classes responsible for transforming data. Notably, every processor both subscribes to a provider to get new data to transform, and is itself a provider, emitting a new event after it has transformed the data.
    - `DataExtractors`: classes responsible for extracting a specific piece of data from `SerialProvider` data.
  - `EventFinalizers`: classes responsible for finalizing an event, i.e., collecting the necessary data and tagging it properly. Finalizers must be `IDataProviders` (though they'll usually be processors) marked with `EventFinalizerAttribute`. Each finalizer is responsible for only one tagged events. The following tags are (or will be) provided, along with their respective finalizers.
    - `pressure` by `PressureFinalizer`
    - `temperature` by `TemperatureFinalizer`
    - `altitude` by `AltitudeFinalizer`

## Data flow

- All data is encapsulated in `EventData` objects, which pack the actual data together with a `DataStamp`, which in turn contains mandatory information (like timestamp and GPS coordinates)
- `IDataProvider`s emit an event when they get data
- `IDataProcessor`s subscribe to providers, process their data and send a new event
- The main controller finds and subscribes to finalizers and communicates their data to the client through Server Sent Events (SSE)

## Notes and useful things

Most browsers have a limit of 6 connections per domain. Since each SSE endpoint represents a connection that stays open indeterminately, we have to be very careful when subscribing to SSEs. However, response bodies consist of `data` tags and `event` tags, so we can have a single endpoint which sends all the data. Thus the endpoints specified above become internal separations which all write to the same endpoint.

### CanSat binary data file analysis

By using the `--read-data` argument followed by the `filePath` of the raw binary data from the CanSat log files, you will be able to quickly analyse offline data, without the use of any external scripts.
