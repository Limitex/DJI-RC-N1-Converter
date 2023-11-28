using Nefarius.ViGEm.Client.Targets.Xbox360;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DJI_RC_N1_Converter
{
    public class BackgroundProgram : IDisposable
    {
        private static readonly byte SOF = 0x55;

        public event EventHandler<string?>? ComPortConnected;
        public event EventHandler<string?>? ComPortDisconnected;

        static readonly short CONTROLLER_AXIS_MIN_VALUE = -0x7FFF;
        static readonly short CONTROLLER_AXIS_MAX_VALUE = 0x7FFF;
        static readonly short CONTROLLER_SLIDER_MIN_VALUE = -0xFF;
        static readonly short CONTROLLER_SLIDER_MAX_VALUE = 0xFF;
        static readonly short CONTROLLER_SLIDER_DEADZONE = 0x50;

        private ComPortManager comPortManager;

        private ViGEmClient? client;
        private IXbox360Controller? controller;

        public BackgroundProgram()
        {
            comPortManager = new ComPortManager();
            comPortManager.ComPortAdded += ComPortAdded_Event;
            comPortManager.ComPortRemoved += ComPortRemoved_Event;
            comPortManager.CheckExistingComPorts();
        }

        public void Dispose()
        {
            comPortManager.Dispose();
        }

        private void ComPortAdded_Event(object? sender, string? portName)
        {
            Debug.WriteLine($"Port connected: {portName}");
            Task.Run(() =>
            {
                if (portName != null && isConnect(portName))
                {
                    SerialPort serialPort = new SerialPort(portName, 115200);
                    serialPort.Open();
                    Debug.WriteLine($"Successfully connected to {serialPort.PortName} port.");

                    client = new ViGEmClient();
                    controller = client.CreateXbox360Controller();
                    controller.Connect();

                    ComPortConnected?.Invoke(this, serialPort.PortName);

                    try
                    {
                        Run(serialPort);
                    }
                    catch (Exception ex) when (ex is OperationCanceledException || ex is IOException || ex is InvalidOperationException)
                    {
                        Debug.WriteLine(ex);
                    }
                    Debug.WriteLine($"Connection with {serialPort.PortName} port was lost.");

                    serialPort.Close();
                    serialPort.Dispose();

                    controller?.Disconnect();
                    client?.Dispose();

                    ComPortDisconnected?.Invoke(this, portName);
                }
                else
                {
                    Debug.WriteLine($"Failed to connect to {portName} port.");
                }
            });
        }

        private void ComPortRemoved_Event(object? sender, string? portName)
        {
            Debug.WriteLine($"Port disconnected: {portName}");
        }

        private bool isConnect(string portName, int timeout = 10000)
        {
            byte[] buffer = new byte[1];
            SerialPort serialPort = new(portName, 115200) { ReadTimeout = timeout };
            try
            {
                serialPort.Open();
                Debug.WriteLine($"Checking {serialPort.PortName} port.");
                serialPort.Write(new byte[0], 0, 0);
                serialPort.Read(buffer, 0, 1);
            }
            catch (Exception ex) when (
                ex is TimeoutException || 
                ex is OperationCanceledException || 
                ex is IOException || 
                ex is InvalidOperationException ||
                ex is UnauthorizedAccessException)
            {
                Debug.WriteLine($"Timeout on {serialPort.PortName} port.");
                Debug.WriteLine(ex);
            }
            finally
            {
                serialPort.Close();
                serialPort.Dispose();
            }
            return buffer[0] == SOF;
        }

        private void Run(SerialPort serialPort)
        {
            if (controller == null)
            {
                Debug.WriteLine("Failed to create controller.");
                return;
            }

            byte[] enableSimulatorModePacket = DumlProtocolPacketBuilder.Build(0x0a, 0x06, 0x40, 0x06, 0x24, new byte[1] { 0x01 });
            byte[] readRequestPacket = DumlProtocolPacketBuilder.Build(0x0a, 0x06, 0x40, 0x06, 0x01, new byte[0]);

            serialPort.Write(enableSimulatorModePacket, 0, enableSimulatorModePacket.Length);

            while (true)
            {
                serialPort.Write(readRequestPacket, 0, readRequestPacket.Length);

                List<byte> data = new();
                var b = serialPort.ReadByte();
                if (b == SOF)
                {
                    data.Add((byte)b);

                    byte[] ph = new byte[2];
                    serialPort.Read(ph, 0, 2);
                    data.AddRange(ph);
                    ushort phValue = BitConverter.ToUInt16(ph, 0);
                    if (!BitConverter.IsLittleEndian)
                    {
                        phValue = (ushort)((phValue >> 8) | (phValue << 8));
                    }
                    ushort pl = (ushort)(0b0000001111111111 & phValue);
                    ushort pv = (ushort)(0b1111110000000000 & phValue);
                    pv >>= 10;
                    byte pc = (byte)serialPort.ReadByte();
                    data.Add(pc);

                    int payloadLength = pl - 4;
                    if (payloadLength > 0)
                    {
                        byte[] pd = new byte[payloadLength];
                        serialPort.Read(pd, 0, payloadLength);
                        data.AddRange(pd);
                    }
                }

                if (data.Count() == 38)
                {
                    byte[] d = data.ToArray();
                    var rh = ParseInputAxis(ParseInput(SubArray(d, 13, 2)));
                    var rv = ParseInputAxis(ParseInput(SubArray(d, 16, 2)));
                    var lh = ParseInputAxis(ParseInput(SubArray(d, 19, 2)));
                    var lv = ParseInputAxis(ParseInput(SubArray(d, 22, 2)));
                    var camera = ParseInputSlider(ParseInput(SubArray(d, 25, 2)));

                    var c1 = (byte)(camera > 0 ? camera : 0);
                    var c2 = (byte)(camera < 0 ? -camera : 0);

                    c1 = (byte)(c1 > CONTROLLER_SLIDER_DEADZONE ? c1 : 0);
                    c2 = (byte)(c2 > CONTROLLER_SLIDER_DEADZONE ? c2 : 0);

                    controller.SetAxisValue(Xbox360Axis.RightThumbX, rh);
                    controller.SetAxisValue(Xbox360Axis.RightThumbY, rv);
                    controller.SetAxisValue(Xbox360Axis.LeftThumbY, lh);
                    controller.SetAxisValue(Xbox360Axis.LeftThumbX, lv);

                    controller.SetSliderValue(Xbox360Slider.RightTrigger, c1);
                    controller.SetSliderValue(Xbox360Slider.LeftTrigger, c2);

                    Debug.WriteLine($"RH: {rh}, RV: {rv}, LH: {lh}, LV:{lv}, CAMERA: {camera}, C1: {c1}, C2: {c2}");
                }
            }

            static byte[] SubArray(byte[] data, int index, int length)
            {
                byte[] result = new byte[length];
                Array.Copy(data, index, result, 0, length);
                return result;
            }

            static double ParseInput(byte[] input)
            {
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(input);
                return (BitConverter.ToInt16(input, 0) - 364.0) / 1320.0;
            }

            static short ParseInputAxis(double input)
            {
                int axisSum = CONTROLLER_AXIS_MAX_VALUE - CONTROLLER_AXIS_MIN_VALUE;
                return (short)(input * axisSum + CONTROLLER_AXIS_MIN_VALUE);
            }

            static short ParseInputSlider(double input)
            {
                int axisSum = CONTROLLER_SLIDER_MAX_VALUE - CONTROLLER_SLIDER_MIN_VALUE;
                return (short)(input * axisSum + CONTROLLER_SLIDER_MIN_VALUE);
            }
        }
    }
}
