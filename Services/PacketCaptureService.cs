using SharpPcap;
using PacketDotNet;
namespace PacketSpotting.Services;


public class PacketCaptureService
{
    private ICaptureDevice? _device;
    private int _packetIndex = 0;
    private bool _isCapturing = false;

    public event Action<string>? OnLogMessage;
    public event Action<PacketCapture>? OnPacketCaptured;

    public void ListDevices()
    {
        var devices = CaptureDeviceList.Instance;
        if (devices.Count < 1)
        {
            LogMessage("No devices found. Install Npcap/WinPcap or run with permissions.");
            return;
        }

        LogMessage("Available devices:");
        for (int i = 0; i < devices.Count; i++)
        {
            var device = devices[i];
            LogMessage($"{i}: {device.Name ?? device.Description}");
        }
    }

    public bool StartCapture(int deviceIndex)
    {
        var devices = CaptureDeviceList.Instance;
        if (deviceIndex < 0 || deviceIndex >= devices.Count)
        {
            LogMessage("Invalid device index.");
            return false;
        }

        _device = devices[deviceIndex];
        
        try
        {
            _device.OnPacketArrival += Device_OnPacketArrival;
            _device.Open(DeviceModes.Promiscuous, 1000);
            _device.StartCapture();
            
            _isCapturing = true;
            LogMessage($"Started capture on {_device.Description}");
            return true;
        }
        catch (Exception ex)
        {
            LogMessage($"Error starting capture: {ex.Message}");
            return false;
        }
    }

    public void StopCapture()
    {
        if (_device == null || !_isCapturing) return;

        try
        {
            _device.StopCapture();
            _device.Close();
            _device.OnPacketArrival -= Device_OnPacketArrival;
            _device = null;
            _isCapturing = false;
            
            LogMessage("Capture stopped.");
        }
        catch (Exception ex)
        {
            LogMessage($"Error stopping capture: {ex.Message}");
        }
    }

    private void Device_OnPacketArrival(object sender, PacketCapture e)
    {
        OnPacketCaptured?.Invoke(e);
        
        var rawPacket = e.GetPacket();
        if (rawPacket.LinkLayerType == LinkLayers.Ethernet)
        {
            var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            var ethernetPacket = (EthernetPacket)packet;

            LogMessage($"{_packetIndex} At: {rawPacket.Timeval.Date:HH:mm:ss.fff}: " +
                      $"MAC:{ethernetPacket.SourceHardwareAddress} -> " +
                      $"MAC:{ethernetPacket.DestinationHardwareAddress}");
            _packetIndex++;
        }
    }

    private void LogMessage(string message)
    {
        OnLogMessage?.Invoke(message);
    }
}