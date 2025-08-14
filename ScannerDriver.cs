using System.IO.Ports;
using ScannerDriver.Enums;
using System.Text;

namespace ScannerDriver;

public class ScannerDriver
{
  private const string COMMAND_PREFIX = "\x16" + "M" + "\x0D";
  private const string ACTIVATE_ENGINE_COMMAND = "\x16" + "T" + "\x0D";
  private const string DEACTIVATE_ENGINE_COMMAND = "\x16" + "U" + "\x0D";

  private SerialPort _serialPort = new SerialPort()
  {
    PortName = "/dev/serial0",
    BaudRate = 9600,
    DataBits = 8,
    Parity = Parity.None,
    StopBits = StopBits.One,
    Handshake = Handshake.None,
  };

  public ScannerDriver(SerialPort serialPort)
  {
    _serialPort = serialPort;
  }

  public bool SetMode(EScannerMode mode)
  {
    try
    {
      string message = string.Empty;
      if (!_serialPort.IsOpen)
      {
        _serialPort.Open();
      }
      if (mode == EScannerMode.Continuous)
      {
        message = COMMAND_PREFIX + "ppam3!";
      }
      else if (mode == EScannerMode.Default)
      {
        message = COMMAND_PREFIX + "aosdft";
      }

      _serialPort.WriteLine(message);
      return true;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error setting mode: {ex.Message}");
    }
    finally
    {
      if (_serialPort.IsOpen)
      {
        _serialPort.Close();
      }
    }

    return false;
  }

  public async Task ActivateEngineAsync(CancellationToken cancellationToken)
  {
    string message = ACTIVATE_ENGINE_COMMAND;
    
  }

  public bool Deactivate()
  {
    string message = DEACTIVATE_ENGINE_COMMAND;

    try
    {
      if (!_serialPort.IsOpen)
      {
        _serialPort.Open();
      }

      _serialPort.WriteLine(message);
      return true;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error deactivating scanner: {ex.Message}");
    }
    finally
    {
      if (_serialPort.IsOpen)
      {
        _serialPort.Close();
      }
    }
  }

  private async Task<ECommandResponse> ListenForResponseAsync(CancellationToken cancellationToken)
  {
    var buffer = new byte[1];

    while (!cancellationToken.IsCancellationRequested)
    {
      int bytesRead = await _serialPort.BaseStream.ReadAsync(buffer, 0, 1, cancellationToken);

      char ch = (char)buffer[0];

      if (ch == '\x06')
      {
        return ECommandResponse.ACK;
      }
      else if (ch == '\x15')
      {
        return ECommandResponse.NAK;
      }
      else if (ch == '\x05')
      {
        return ECommandResponse.ENQ;
      }
    }

    return ECommandResponse.NAK; // Default response
  }

  private async Task SendCommandAsync(string command, CancellationToken cancellationToken)
  {
    try
    {
      if (!_serialPort.IsOpen)
      {
        _serialPort.Open();
      }

      string fullCommand = command + "\n";
      byte[] commandBytes = Encoding.ASCII.GetBytes(fullCommand);
      await _serialPort.BaseStream.WriteAsync(commandBytes, 0, commandBytes.Length, cancellationToken);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error sending command: {ex.Message}");
    }
    finally
    {
      if (_serialPort.IsOpen)
      {
        _serialPort.Close();
      }
    }
  }
}

