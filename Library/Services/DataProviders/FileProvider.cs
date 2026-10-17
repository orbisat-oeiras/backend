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
        private readonly string directory,
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
            byte[]? extractedPacket;

            while ((extractedPacket = _packetBuffer.ExtractFirstValidPacket()) != null)
            {
                if (packetNumber % 100 == 0)
                    Console.WriteLine("Processing packet number: {0}", packetNumber);

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

                // Flush the completed time-window group whenever a packet falls outside it,
                // matching the incremental approach used by SerialProvider.
                bool fitsCurrentBatch = _packetResync.AddPacket(packet);
                if (!fitsCurrentBatch)
                {
                    List<Packet>? completedGroup = _packetResync.GetNextGroup(
                        pendingPacket: packet
                    );
                    if (completedGroup != null)
                        SendGroup(completedGroup);
                }

                packetNumber++;
            }

            Console.WriteLine("Total packets processed: {0}", packetNumber);

            // Flush any packets still in the buffer after the last time window
            List<Packet>? trailingGroup = _packetResync.GetNextGroup(pendingPacket: null);
            if (trailingGroup != null)
                SendGroup(trailingGroup);
        }

        private void SendGroup(List<Packet> list)
        {
            _currentData.Clear();
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

            bool hasGps =
                _currentData.TryGetValue(SerialProvider.DataLabel.Gps, out byte[]? gpsBytes)
                && gpsBytes!.Length >= 20;

            GPSCoords coords = new()
            {
                Latitude = hasGps ? BitConverter.ToDouble(gpsBytes!, 0) : double.NaN,
                Longitude = hasGps ? BitConverter.ToDouble(gpsBytes!, 8) : double.NaN,
                Altitude = hasGps ? BitConverter.ToSingle(gpsBytes!, 16) : float.NaN,
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
                    Data = new Dictionary<SerialProvider.DataLabel, byte[]>(_currentData),
                }
            );
        }
    }
}
