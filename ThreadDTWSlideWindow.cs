using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace GloveLibrary
{
    public class ThreadDTWSlideWindow
    {
        private bool go;
        private DTWWindowSlide dtw;
        private int num;
        private bool inPedalata;
        private int soglia;
        private BluetoothDeviceGloveReceiver receiver;

        public event Action PedalataTrovata;

        public ThreadDTWSlideWindow(int Soglia = 20, int FinestraDtw = 20)
        {
            go = true;
            soglia = Soglia;
            receiver = new BluetoothDeviceGloveReceiver();
            dtw = new DTWWindowSlide(PedalataTemplate.getPedalata(), FinestraDtw);//implementazione pedalata perfetta
            num = 0;
            inPedalata = false;
        }

        public void stopThread()
        {
            go = false;
        }

        public void start()
        {
            receiver.openPort();

            receiver.enableGloveFunction();

            float[] data;

            while (go)
            {
                data = receiver.getData();

                if (dtw.add(data))
                {
                    if (dtw.Costo == float.NaN)
                        continue;

                    if (dtw.Costo < soglia)
                    {
                        if (!inPedalata)
                        {
                            num++;

                            if(PedalataTrovata != null)
                                PedalataTrovata();

                            inPedalata = true;
                        }

                    }
                    else
                    {
                        if (inPedalata)
                            inPedalata = false;
                    }
                }

            }

            receiver.disableGloveFunction();

            receiver.closePort();
        }
    }
}
