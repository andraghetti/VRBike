using System;
using UnityEditor;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace GloveLibrary
{
    public class ThreadDTWSlideWindow
    {
        public bool go;
        private SerialPort serial;
        private string _com;
        public event Action<int, float> PedalataTrovata;

        public ThreadDTWSlideWindow(string ComName)
        {
            go = true;
            _com = ComName;
        }

        public void stopThread()
        {
            go = false;
        }

        public void start()
        {
            go = openPort();

            string data;
            string tmp = "";
            int sterzo;
            float speed;

            while (go)
            {
                try
                {
                    data = getData();
                    
                    if (data != "")
                    {

                        //gestione sterzo
                        tmp = "";
                        if (data[0] != '0')
                            tmp += data[0];
                        tmp += data[1];
                        sterzo = int.Parse(tmp);

                        //MotoController.msgToDebug("Spacchetto Velocità");
                        //gestione velocità
                        tmp = "";
                        if (data[3] != '0')
                            tmp += data[3];
                        tmp += data.Substring(4);
                        speed = float.Parse(tmp);
                        if (speed > 0 && data[2] == '-')
                            speed *= -1;

                        //debug
                        MotoController.msgToDebug(sterzo + " --- " + speed);

                        //Invio dati a unity
                        PedalataTrovata(sterzo, speed);
                    }
                }
                catch (TimeoutException)
                {
                    if (!go)
                        closePort();
                }
            }
            closePort();
            MotoController.msgToDebug("close glove");
        }

        public bool openPort()
        {

            MotoController.msgToDebug("Nome com: " + _com);
            try
            {
                serial = new SerialPort(_com, 9600, Parity.None, 8, StopBits.One);
                serial.ReadBufferSize = 4096;
                serial.DtrEnable = true;
                serial.ReadTimeout = 2000; //ferma la lettura se non c'è più nessun dato in ingresso

                serial.Open();
            }
            catch (IOException e)
            {
                MotoController.msgToDebug(e.ToString());
                return false;
            }
            MotoController.msgToDebug("Is open: " + serial.IsOpen);

            return serial.IsOpen;
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

        public string getData()
        {
            String tmp = "";
            if (serial.IsOpen && go)
                tmp = serial.ReadLine();
            return tmp;
        }

    }
}
