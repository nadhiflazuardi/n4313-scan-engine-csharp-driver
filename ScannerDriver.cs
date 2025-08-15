using System.IO.Ports;
using ScannerDriver.Enums;
using System.Text;

namespace ScannerDriver;

public class ScannerDriver
{
  private const string COMMAND_PREFIX = "\x16" + "M" + "\x0D";
  private const string ACTIVATE_ENGINE_COMMAND = "\x16" + "T" + "\x0D";
  private const string DEACTIVATE_ENGINE_COMMAND = "\x16" + "U" + "\x0D";
  public event EventHandler<ECommandResponse>? OnResponseReceived;
  public event EventHandler<string>? OnDataReceived;

  private EScannerMode _currentMode;

  private CancellationTokenSource? _listenerCts;
  private Task? _listenerTask;

  private readonly StringBuilder _barcodeBuffer = new();

  private SerialPort _serialPort = new SerialPort()
  {
    PortName = "/dev/ttyV0",
    BaudRate = 9600,
    DataBits = 8,
    Parity = Parity.None,
    StopBits = StopBits.One,
    Handshake = Handshake.None,
  };

  public void Connect()
  {
    if (!_serialPort.IsOpen)
    {
      _serialPort.Open();
    }

    _listenerCts = new CancellationTokenSource();
    _listenerTask = Task.Run(() => ListenLoop(_listenerCts.Token));
  }

  public void Disconnect()
  {
    _listenerCts?.Cancel();

    if (_serialPort.IsOpen)
    {
      _serialPort.Close();
    }
  }

  public async Task ActivateEngineAsync(CancellationToken cancellationToken)
  {
    string message = ACTIVATE_ENGINE_COMMAND;
    try
    {
      EnsureConnected();

      await SendCommandAsync(message, cancellationToken);
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException("Failed to activate engine.", ex);
    }
  }

  public async Task DeactivateEngineAsync(CancellationToken cancellationToken)
  {
    string message = DEACTIVATE_ENGINE_COMMAND;
    try
    {
      EnsureConnected();

      await SendCommandAsync(message, cancellationToken);
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException("Failed to deactivate engine.", ex);
    }
  }

  public async Task SetModeAsync(EScannerMode mode, CancellationToken cancellationToken)
  {
    if (_currentMode == mode)
    {
      return;
    }

    try
    {
      EnsureConnected();

      string message = mode switch
      {
        EScannerMode.Continuous => COMMAND_PREFIX + "ppam3!",
        EScannerMode.Default => COMMAND_PREFIX + "aosdft",
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
      };

      await SendCommandAsync(message, cancellationToken);
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException("Failed to set scanner mode.", ex);
    }
  }


  public async Task FactoryReset(CancellationToken cancellationToken)
  {
    try
    {
      EnsureConnected();

      string removeCustomSettingsCommand = COMMAND_PREFIX + "defovr!";
      await SendCommandAsync(removeCustomSettingsCommand, cancellationToken);
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException("Failed to reset scanner to factory settings.", ex);
    }
  }

  private void EnsureConnected()
  {
    if (!_serialPort.IsOpen)
    {
      throw new InvalidOperationException("ScannerDriver is not connected. Call Connect() before using this method.");
    }
  }

  private async Task ListenLoop(CancellationToken cancellationToken)
  {
    var buffer = new byte[1];
    while (!cancellationToken.IsCancellationRequested)
    {
      int bytesRead = await _serialPort.BaseStream.ReadAsync(buffer, 0, 1, cancellationToken);
      if (bytesRead == 0)
      {
        continue; // No data read, continue listening
      }

      char ch = (char)buffer[0];

      if (ch == '\x06')
      {
        OnResponseReceived?.Invoke(this, ECommandResponse.ACK);
      }
      else if (ch == '\x15')
      {
        OnResponseReceived?.Invoke(this, ECommandResponse.NAK);
      }
      else if (ch == '\x05')
      {
        OnResponseReceived?.Invoke(this, ECommandResponse.ENQ);
      }
      else if (ch == '\r')
      {
        if (_barcodeBuffer.Length > 0)
        {
          string barcode = _barcodeBuffer.ToString();
          _barcodeBuffer.Clear();
          OnDataReceived?.Invoke(this, barcode);
        }
      }
      else
      {
        _barcodeBuffer.Append(ch);
      }
    }
  }

  private async Task SendCommandAsync(string command, CancellationToken cancellationToken)
  {
    try
    {
      EnsureConnected();

      string fullCommand = command + "\n";
      byte[] commandBytes = Encoding.ASCII.GetBytes(fullCommand);
      await _serialPort.BaseStream.WriteAsync(commandBytes, 0, commandBytes.Length, cancellationToken);
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException("Failed to send command to scanner.", ex);
    }
  }
}

