using System;
using System.IO;
using System.IO.Ports;
using System.Linq;

namespace GloveLibrary
{
    class BluetoothDeviceGloveReceiver
    {
        private SerialPort serial;
        private string _com;

        public BluetoothDeviceGloveReceiver(string ComName)
        {
            _com = ComName;
            //serial = new SerialPort();
            //MotoController.msgToDebug("porta creata: " + (serial != null));
        }

        public bool openPort()
        {
            string name = null;
            name = "";
            /*
            int check;

            try
            {
                check = int.Parse(_com.Substring(3));
            }
            catch (Exception)
            {
                return false;
            }
            
            if(check > 9)
            {
                name = "\\\\.\\";
            }
            */
            if(_com.Length > 4)
            {
                name = "\\\\.\\";
            }

            name += _com;
            /*
            string[] porte = SerialPort.GetPortNames();
            foreach(string tmp in porte)
            {
                MotoController.msgToDebug(tmp);
            }
            */

            //name = "\\\\.\\" + SerialPort.GetPortNames()[1]; //"COM2"; // "\\\\.\\COM16";
            
            MotoController.msgToDebug("Nome com: "+name);
            try
            {
                //MotoController.msgToDebug("provo ad aprire");
                serial = new SerialPort(name, 9600, Parity.None, 8, StopBits.One);
                serial.ReadBufferSize = 4096;
                serial.DtrEnable = true;
                //serial.ReadTimeout = 200;
                
                //MotoController.msgToDebug("Prima dell'apertura");
                serial.Open();
            }
            catch (IOException e)
            {
                MotoController.msgToDebug(e.ToString());
                return false;
            }
            MotoController.msgToDebug("Is open: "+serial.IsOpen);
            
            return serial.IsOpen;
        }

        public string getData()
        {
            //MotoController.msgToDebug("Mi fermo a leggere la porta...");
            String tmp = serial.ReadLine();
            //MotoController.msgToDebug(tmp);
            return tmp;
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
