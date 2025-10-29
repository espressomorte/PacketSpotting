using SharpPcap;
using PacketDotNet;
namespace ConsoleApp;
class Program
{
    static void Main()
    {
        var devices = CaptureDeviceList.Instance;
        if (devices.Count < 1) {
            Console.WriteLine("No devices found. Install Npcap/WinPcap or run with permissions.");
            return;
        }

        Console.WriteLine("Available devices:");
        for (int i = 0; i < devices.Count; i++)
            Console.WriteLine($"{i}: { devices[i].Name?? devices[i].Description}");

        Console.Write("Choose device index: ");
        if (!int.TryParse(Console.ReadLine(), out int idx) || idx < 0 || idx >= devices.Count) return;
        var device = devices[idx];
        device.OnPacketArrival += device_OnPacketArrival;
        device.Open(DeviceModes.Promiscuous, 1000);
        Console.WriteLine($"Listening on {device.Description} - press Ctrl+C to stop.");
        device.StartCapture();
        Console.CancelKeyPress += (s,e) => {
            e.Cancel = true;
            Console.WriteLine("Stopping capture...");
            device.StopCapture();
            device.Close();
            Environment.Exit(0);
        };

        // Block
        System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
    }
    private static int packetIndex = 0;
    private static void device_OnPacketArrival(object sender, PacketCapture e)
    {
        //var device = (ICaptureDevice)sender;

        // write the packet to the file
        var rawPacket = e.GetPacket();
        //captureFileWriter.Write(rawPacket);
        Console.WriteLine("Packet dumped to file.");

        if (rawPacket.LinkLayerType == PacketDotNet.LinkLayers.Ethernet)
        {
            var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            var ethernetPacket = (EthernetPacket)packet;

            Console.WriteLine("{0} At: {1}:{2}: MAC:{3} -> MAC:{4}",
                packetIndex,
                rawPacket.Timeval.Date.ToString(),
                rawPacket.Timeval.Date.Millisecond,
                ethernetPacket.SourceHardwareAddress,
                ethernetPacket.DestinationHardwareAddress);
            packetIndex++;
        }
    }

}