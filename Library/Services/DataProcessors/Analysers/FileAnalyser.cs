using backend.Library.Models;
using backend.Library.Services.DataProviders;
using Orbipacket;

namespace backend.Library.Services.DataProcessors.Analyzers
{
    /// <summary>
    /// Raw binary file analyser for directly accesing log files
    /// and producing csv data files.
    /// </summary>
    public sealed class FileAnalyser : IDataProvider<Dictionary<SerialProvider.DataLabel, byte[]>>
    {
        public event Action<
            EventData<Dictionary<SerialProvider.DataLabel, byte[]>>
        >? OnDataProvided;
        private readonly PacketResync packetResync = new();
        private readonly PacketBuffer _packetBuffer = new();
        private readonly Dictionary<SerialProvider.DataLabel, byte[]> _currentData = new();

        /// <summary>
        /// Analyse the contents of a binary file and extract packets.
        /// </summary>
        /// <param name="filepath">Path to the binary file.</param>

        public void AnalyseFileContents(string filepath)
        {
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException("The specified file does not exist.", filepath);
            }

            byte[] fileBytes = File.ReadAllBytes(filepath);

            // This number is really trial and error, too much and it accumulates too many packets,
            // too little and it zeroes out packet groups. 175 was the best for me
            // You can fiddle around with it, I really should study but here I am
            int byteWindow = 175;

            for (int i = 0; i < fileBytes.Length; i += byteWindow)
            {
                if (i + byteWindow > fileBytes.Length)
                {
                    Console.WriteLine("Reached end of file.");
                    break;
                }

                byte[] deliveredBytes = [.. fileBytes.Skip(i).Take(byteWindow)];
                _packetBuffer.Add(deliveredBytes);
                // Console.WriteLine("Analysing bytes from {0} to {1}", i, i + byteWindow);
                byte[]? extractedPacket;

                while ((extractedPacket = _packetBuffer.ExtractFirstValidPacket()) != null)
                {
                    Packet? packet = Decode.GetPacketInformation(extractedPacket);
                    if (packet == null || packet.Payload?.Value == null)
                    {
                        Console.WriteLine("Warning: Invalid or corrupted packet.");
                        continue;
                    }

                    // Console.WriteLine(
                    //     "Packet DeviceId: {0}, Timestamp: {1}, Payload Length: {2}",
                    //     packet.DeviceId,
                    //     packet.Timestamp,
                    //     packet.Payload.Value.Length
                    // );
                    packetResync.AddPacket(packet);
                }

                List<Packet>? list;
                Console.WriteLine("Getting next group of packets...");
                list = packetResync.GetNextGroup();
                if (list.Count == 0)
                {
                    Console.WriteLine("No packets in the current group.");
                    continue;
                }
                foreach (Packet packet in list)
                {
                    SerialProvider.DataLabel label = packet.DeviceId switch
                    {
                        DeviceId.PressureSensor => SerialProvider.DataLabel.Pressure,
                        DeviceId.TemperatureSensor => SerialProvider.DataLabel.Temperature,
                        DeviceId.HumiditySensor => SerialProvider.DataLabel.Humidity,
                        DeviceId.System => SerialProvider.DataLabel.System,
                        DeviceId.Unknown => SerialProvider.DataLabel.Unknown,
                        DeviceId.GPS => SerialProvider.DataLabel.GPSData,
                        DeviceId.Accelerometer => SerialProvider.DataLabel.AccelerationData,
                        _ => throw new NotImplementedException(),
                    };
                    _currentData[label] = packet.Payload.Value ?? BitConverter.GetBytes(1.0f);
                }

                Dictionary<SerialProvider.DataLabel, byte[]> dict = new(_currentData);
                ulong timestamp = list[0].Timestamp;

                GPSCoords coords = new()
                {
                    Latitude = _currentData.TryGetValue(
                        SerialProvider.DataLabel.GPSData,
                        out byte[]? latBytes
                    )
                        ? BitConverter.ToDouble(latBytes, 0)
                        : double.NaN,
                    Longitude = _currentData.TryGetValue(
                        SerialProvider.DataLabel.GPSData,
                        out byte[]? lonBytes
                    )
                        ? BitConverter.ToDouble(lonBytes, 8)
                        : double.NaN,
                };

                OnDataProvided?.Invoke(
                    new EventData<Dictionary<SerialProvider.DataLabel, byte[]>>
                    {
                        DataStamp = new DataStamp { Timestamp = timestamp, Coordinates = coords },
                        Data = dict,
                    }
                );
                _currentData.Clear();
            }
        }
    }
}
