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

    return false;
  }

  // private ECommandResponse ReadResponse()
  // {
  //   try
  //   {
  //     if (!_serialPort.IsOpen)
  //     {
  //       _serialPort.Open();
  //     }

  //     string response = ReadLineWithACK(_serialPort);

  //     if (response.Contains("ACK"))
  //     {
  //       return ECommandResponse.ACK;
  //     }
  //     else if (response.Contains("NAK"))
  //     {
  //       return ECommandResponse.NAK;
  //     }
  //     else if (response.Contains("ENQ"))
  //     {
  //       return ECommandResponse.ENQ;
  //     }
  //   }
  //   catch (TimeoutException)
  //   {
  //     Console.WriteLine("Read operation timed out.");
  //   }
  //   catch (Exception ex)
  //   {
  //     Console.WriteLine($"Error reading response: {ex.Message}");
  //   }
  //   finally
  //   {
  //     if (_serialPort.IsOpen)
  //     {
  //       _serialPort.Close();
  //     }
  //   }

  //   return ECommandResponse.NAK; // Default to NAK if no valid response
  // }

  /// <summary>
  /// Reads a line from the serial port that can be terminated by either:
  /// - Carriage Return (\r)
  /// - Line Feed (\n)
  /// - ASCII ACK character (\x06)
  /// </summary>
  // static string ReadLineWithACK(SerialPort port)
  // {
  //   StringBuilder buffer = new StringBuilder();

  //   while (true)
  //   {
  //     int byteRead = port.ReadByte(); // This will throw TimeoutException if no data
  //     char ch = (char)byteRead;

  //     // Check for termination characters
  //     if (ch == '\r' || ch == '\n' || ch == '\x06' || ch == '\x15' || ch == '\x05') // CR, LF, ACK, NAK, or ENQ
  //     {
  //       string result = buffer.ToString();

  //       // Display special characters for debugging
  //       // if (ch == '\x06')
  //       // {
  //       //     Console.ForegroundColor = ConsoleColor.Yellow;
  //       //     Console.WriteLine($"[ACK received after: '{result}']");
  //       //     Console.ResetColor();
  //       // }
  //       if (ch == '\x15')
  //       {
  //         Console.ForegroundColor = ConsoleColor.Red;
  //         Console.WriteLine($"[NAK Received: Bad command, or out of range command parameters]");
  //         Console.ResetColor();
  //       }
  //       if (ch == '\x05')
  //       {
  //         Console.ForegroundColor = ConsoleColor.Red;
  //         Console.WriteLine($"[ENQ Received: Bad command]");
  //         Console.ResetColor();
  //       }

  //       return result;
  //     }
  //     else
  //     {
  //       buffer.Append(ch);
  //     }
  //   }
  // }

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
}

