using System;
using UnityEditor;
using System.Threading;

namespace GloveLibrary
{
    public class ThreadDTWSlideWindow
    {
        private bool go;
        private BluetoothDeviceGloveReceiver receiver;

        public event Action<int, float> PedalataTrovata;

        public ThreadDTWSlideWindow(string ComName)
        {
            go = true;
            receiver = new BluetoothDeviceGloveReceiver(ComName);
            
            //MotoController.msgToDebug("create thread dtw");
        }

        public void stopThread()
        {
            go = false;
        }

        public void start()
        {
            //MotoController.msgToDebug("start thread dtw and go: "+go);
            go = receiver.openPort();
            //MotoController.msgToDebug("open port: " + go);

            string data;
            string tmp = "";
            int sterzo;
            float speed;

            while (go)
            {   
                data = receiver.getData();

                //MotoController.msgToDebug("Spacchetto sterzo; "+data);

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
            MotoController.msgToDebug("close glove");
            
            receiver.closePort();
        }
    }
}
