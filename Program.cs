using PacketSpotting.Services;

namespace PacketSpotting;

class Program
{
    private static PacketCaptureService captureService = new();
    static void Main(string[] args)
    {
        captureService.OnLogMessage += message => Console.WriteLine($"[INFO] {message}");
        
        try
        {
            captureService.ListDevices();
            Console.Write("Choose device index: ");
            if (!int.TryParse(Console.ReadLine(), out int deviceIndex))
            {
                Console.WriteLine("Invalid input.");
                return;
            }

            if (!captureService.StartCapture(deviceIndex))
            {
                Console.WriteLine("Failed to start capture.");
                return;
            }
            Console.WriteLine("Capture started - press Ctrl+C to stop.");

            Console.CancelKeyPress += OnCancelKeyPress;
            
            Thread.Sleep(Timeout.Infinite);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
        }
        finally
        {
            captureService.StopCapture();
        }
    }

    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        Console.WriteLine("\nStopping capture...");
        captureService.StopCapture();
        Environment.Exit(0);
    }
    
}