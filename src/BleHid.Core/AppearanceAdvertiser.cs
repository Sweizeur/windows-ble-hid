using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace BleHid.Core;

/// <summary>
/// GattServiceProvider gives no control over the GAP Appearance field, and hosts such as
/// Android use it to decide whether to attach their HOGP host. This publishes a parallel
/// advertisement carrying Appearance = Keyboard to try to influence that classification.
/// </summary>
public sealed class AppearanceAdvertiser : IDisposable
{
    public const ushort KeyboardAppearance = 0x03C1;
    public const ushort MouseAppearance    = 0x03C2;

    private BluetoothLEAdvertisementPublisher? _publisher;
    private BluetoothLEAdvertisementPublisher? _mousePublisher;

    public event Action<string>? Log;

    public BluetoothLEAdvertisementPublisherStatus Status =>
        _publisher?.Status ?? BluetoothLEAdvertisementPublisherStatus.Created;

    public bool Start(ushort appearance = KeyboardAppearance)
    {
        try
        {
            _publisher = CreatePublisher(appearance);
            _publisher.StatusChanged += (_, args) =>
                Log?.Invoke($"[appr] keyboard publisher -> {args.Status} (error: {args.Error})");

            _publisher.Start();
            Log?.Invoke($"[appr] keyboard appearance started, status = {_publisher.Status}");
            return true;
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[appr] keyboard appearance FAILED: {ex.Message}");
            _publisher = null;
            return false;
        }
    }

    /// <summary>
    /// Some iPhone builds only attach a pointer when the device also advertises as a mouse.
    /// </summary>
    public void StartMousePointerAdvertisement()
    {
        if (_mousePublisher is not null) return;

        try
        {
            _mousePublisher = CreatePublisher(MouseAppearance);
            _mousePublisher.StatusChanged += (_, args) =>
                Log?.Invoke($"[appr] mouse publisher -> {args.Status} (error: {args.Error})");

            _mousePublisher.Start();
            Log?.Invoke($"[appr] mouse appearance started, status = {_mousePublisher.Status}");
        }
        catch (Exception ex)
        {
            Log?.Invoke($"[appr] mouse appearance skipped: {ex.Message}");
            _mousePublisher = null;
        }
    }

    private static BluetoothLEAdvertisementPublisher CreatePublisher(ushort appearance)
    {
        var writer = new DataWriter { ByteOrder = ByteOrder.LittleEndian };
        writer.WriteUInt16(appearance);

        var advertisement = new BluetoothLEAdvertisement();
        advertisement.DataSections.Add(new BluetoothLEAdvertisementDataSection
        {
            DataType = 0x19, // GAP Appearance
            Data = writer.DetachBuffer()
        });

        var uuidWriter = new DataWriter { ByteOrder = ByteOrder.LittleEndian };
        uuidWriter.WriteUInt16(0x1812);
        advertisement.DataSections.Add(new BluetoothLEAdvertisementDataSection
        {
            DataType = 0x03, // Complete list of 16-bit service UUIDs
            Data = uuidWriter.DetachBuffer()
        });

        return new BluetoothLEAdvertisementPublisher(advertisement);
    }

    public void Dispose()
    {
        if (_publisher?.Status == BluetoothLEAdvertisementPublisherStatus.Started)
            _publisher.Stop();
        if (_mousePublisher?.Status == BluetoothLEAdvertisementPublisherStatus.Started)
            _mousePublisher.Stop();
        _publisher = null;
        _mousePublisher = null;
    }
}
