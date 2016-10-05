using System;
using System.IO;
using System.IO.Ports;
using System.Linq;

namespace GloveLibrary
{
    class BluetoothDeviceGloveReceiver
    {
        private SerialPort serial;
        private byte[] START_CODE = new byte[] { 0x01, 0x02, 0x80, 0x03 };
        private byte[] STOP_CODE = new byte[] { 0x01, 0x02, 0x00, 0x03 };
        private string MacAddressID = "0080E1B22D90";

        public BluetoothDeviceGloveReceiver()
        {
            serial = new SerialPort();
        }

        public bool openPort()
        {
            string[] names = SerialPort.GetPortNames();
            string name;

            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.EnableRaisingEvents = false;
            proc.StartInfo.FileName = "ComID.exe";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            proc.Start();

            name = proc.StandardOutput.ReadLine();

            proc.WaitForExit();

            if (name.Contains("null") || string.IsNullOrEmpty(name))
                return false;

            serial.PortName = name;

            serial.ReadBufferSize = 4096;

            try
            {
                serial.Open();
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }

            serial.Parity = Parity.None;
            serial.BaudRate = 115200;
            serial.DataBits = 8;
            serial.StopBits = StopBits.One;

            return true;
        }

        public bool enableGloveFunction()
        {
            try
            {
                serial.Write(START_CODE, 0, START_CODE.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
            return true;
        }

        public float[] getData()
        {
            PacchettoGlove p = new PacchettoGlove();
            while (!p.isWellDone())
            {
                p.add(serial.ReadByte());
            }

            return p.getInfo();
        }

        public bool disableGloveFunction()
        {
            try
            {
                serial.Write(STOP_CODE, 0, STOP_CODE.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }

            return true;
        }

        public bool closePort()
        {
            try
            {
                serial.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }

            return true;
        }
    }
}
