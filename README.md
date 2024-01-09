## Project structure
- Controllers: classes derived from ``ControlerBase``, implementing API endpoints
    - ``PrimaryMissionController``: responsible for providing client-ready data related to the primary mission, provides the following GET-only SSE endpoints:
        - ``api/primary/temperature``
        - ``api/primary/pressure``
        - ``api/primary/altitude``
    - ``SecondaryMissionController``: responsible for providing client-ready data related to the secondary mission, provides the following GET-only SSE endpoints:
        - ``api/secondary/raw``
        - ``api/seconddary/ndvi``
    - ``GeneralController``: responsible for providing client-ready data which ins't related to any particular mission, provides the following GET-only SSE endpoints:
        - ``api/general/time``
        - ``api/general/acceleration``
        - ``api/general/position``
        - ``api/general/raw``
- Models: classes defining data models for the app to work with
    - ``DataStamp``: general information attached to all data sent, e.g., timestamp and coordinates
- Services: Modular classes implementing internal functionality

## Data flow
- All data is encapsulated in ``EventData`` objects, which pack the actual data together with a ``DataStamp``, which in turn contains mandatory information (like timestamp and GPS coordinates)
- ``IDataProvider``s emit an event when they get data
- ``IDataProcessor``s subscribe to providers, process their data and send a new event
- The main controller subscribes to the final providers (processors are also providers) and communicates their data to the client

## Notes and useful things
Most browsers have a limit of 6 connections per domain. Since each SSE endpoint represents a connection that stays open indeterminatly, we have to be very careful when subscribing to SSEs. However, response bodies consist of ``data`` tags and ``event`` tags, so we can have a single endpoint which sends all the data. Thus the endpoints specified above become internal spearations which all write to the same endpoint.