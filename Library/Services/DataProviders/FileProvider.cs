using System.Runtime.CompilerServices;
using System.Text;
using backend.Library.Models;
using backend.Library.Services.DataProviders;
using Orbipacket;

namespace backend.Library.Services.DataProcessors.Analyzers
{
    /// <summary>
    /// Raw binary file analyser for directly accesing log files
    /// and producing csv data files.
    /// </summary>
    public sealed class FileProvider : IDataProvider<Dictionary<SerialProvider.DataLabel, byte[]>>
    {
        public event Action<
            EventData<Dictionary<SerialProvider.DataLabel, byte[]>>
        >? OnDataProvided;

        private long offset = 0;
        private ulong t0,
            t2;
        string directory,
            fileName,
            filePath;
        private readonly PacketResync _packetResync = new();
        private readonly PacketBuffer _packetBuffer = new();
        private readonly Dictionary<SerialProvider.DataLabel, byte[]> _currentData = [];

        public FileProvider()
        {
            directory = "SystemMessages";
            fileName = $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}-RAW.txt";

            filePath = Path.GetFullPath(Path.Combine(directory, fileName));
            Directory.CreateDirectory(directory);
        }

        /// <summary>
        /// Analyse the contents of a binary file and extract packets.
        /// </summary>
        /// <param name="filepath">Path to the binary file.</param>
        public void AnalyseFileContents(string filepath)
        {
            int packetNumber = 0;
            byte[] fileBytes = File.ReadAllBytes(filepath);

            _packetBuffer.Add(fileBytes);
            // Console.WriteLine("Analysing bytes from {0} to {1}", i, i + byteWindow);
            byte[]? extractedPacket;

            while ((extractedPacket = _packetBuffer.ExtractFirstValidPacket()) != null)
            {
                if (packetNumber % 100 == 0)
                {
                    Console.WriteLine("Processing packet number: {0}", packetNumber);
                }
                Packet? packet = Decode.GetPacketInformation(extractedPacket);
                if (packet == null || packet.Payload?.Value == null)
                {
                    Console.WriteLine("Warning: Invalid or corrupted packet.");
                    continue;
                }
                if (packet.DeviceId == DeviceId.TimeSync)
                {
                    Console.WriteLine(
                        "Packet number " + packetNumber + " with TimeSync DeviceId found."
                    );
                    try
                    {
                        t0 = BitConverter.ToUInt64(packet.Payload.Value, 0);
                        t2 = BitConverter.ToUInt64(packet.Payload.Value, 16);
                    }
                    catch (System.ArgumentOutOfRangeException)
                    {
                        Console.WriteLine("Error calculating the offset.");
                    }
                    // Here we can't exactly replicate the last t3, so I'm going to consider the
                    // offset as simply t2 - t0
                    offset = (long)(t2 - t0);
                    Console.WriteLine("Offset calculated: " + offset);
                }
                if (packet.DeviceId == DeviceId.System)
                {
                    string payloadString = Encoding.UTF8.GetString(packet.Payload.Value);
                    Console.WriteLine("System data: " + payloadString);
                    File.AppendAllText(
                        filePath,
                        $"Packet number {packetNumber}: {payloadString} \n"
                    );
                }
                _packetResync.AddPacket(packet);

                // Console.WriteLine(
                //     "Packet DeviceId: {0}, Timestamp: {1}, Payload Length: {2}",
                //     packet.DeviceId,
                //     packet.Timestamp,
                //     packet.Payload.Value.Length
                // );
                packetNumber++;
            }
            Console.WriteLine("Total packets processed: {0}", packetNumber);

            List<Packet>? list;
            Console.WriteLine("Getting next group of packets...");

            while ((list = _packetResync.GetNextGroup()).Count > 0)
            {
                foreach (Packet packet in list)
                {
                    SerialProvider.DataLabel label = packet.DeviceId switch
                    {
                        DeviceId.PressureSensor => SerialProvider.DataLabel.Pressure,
                        DeviceId.TemperatureSensor => SerialProvider.DataLabel.Temperature,
                        DeviceId.HumiditySensor => SerialProvider.DataLabel.Humidity,
                        DeviceId.System => SerialProvider.DataLabel.System,
                        DeviceId.Unknown => SerialProvider.DataLabel.Unknown,
                        DeviceId.Gps => SerialProvider.DataLabel.Gps,
                        DeviceId.Accelerometer => SerialProvider.DataLabel.AccelerationData,
                        _ => SerialProvider.DataLabel.Unknown,
                    };
                    _currentData[label] = packet.Payload.Value ?? BitConverter.GetBytes(float.NaN);
                }

                ulong timestamp = list[0].Timestamp;

                GPSCoords coords = new()
                {
                    Latitude = _currentData.TryGetValue(
                        SerialProvider.DataLabel.Gps,
                        out byte[]? latBytes
                    )
                        ? BitConverter.ToDouble(latBytes, 0)
                        : double.NaN,
                    Longitude = _currentData.TryGetValue(
                        SerialProvider.DataLabel.Gps,
                        out byte[]? lonBytes
                    )
                        ? BitConverter.ToDouble(lonBytes, 8)
                        : double.NaN,
                    Altitude = _currentData.TryGetValue(
                        SerialProvider.DataLabel.Gps,
                        out byte[]? altitudeBytes
                    )
                        ? BitConverter.ToSingle(altitudeBytes, 16)
                        : float.NaN,
                };

                OnDataProvided?.Invoke(
                    new EventData<Dictionary<SerialProvider.DataLabel, byte[]>>
                    {
                        DataStamp = new DataStamp
                        {
                            Offset = offset,
                            Timestamp = timestamp,
                            Coordinates = coords,
                        },
                        Data = _currentData,
                    }
                );
                _currentData.Clear();
            }
        }
    }
}
