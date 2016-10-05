using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace GloveLibrary
{
    class PacchettoGlove
    {
        private ArrayList lista;

        public PacchettoGlove()
        {
            lista = new ArrayList();
        }

        public void add(int dato)
        {
            lista.Add(dato);

            if (lista.Count > 59)
                lista.RemoveAt(0);
        }

        public bool isWellDone()
        {
            if (lista.Count != 59)
                return false;

            if ((int)lista[0] != 1)
                return false;

            if ((int)lista[1] != 2)
                return false;

            if ((int)lista[58] != 3)
                return false;

            return true;
        }

        public float[] getInfo()
        {
            if (!this.isWellDone())
                return null;

            float[] ret = new float[9];
            Byte[] b = new Byte[4];

            for (int i = 2, j = 0; j < 6; i += 4, j++)
            {
                b[0] = BitConverter.GetBytes((int)lista[i])[0];
                b[1] = BitConverter.GetBytes((int)lista[i + 1])[0];
                b[2] = BitConverter.GetBytes((int)lista[i + 2])[0];
                b[3] = BitConverter.GetBytes((int)lista[i + 3])[0];

                ret[j] = BitConverter.ToSingle(b, 0);
            }

            ret[6] = BitConverter.GetBytes((int)lista[0])[0];
            ret[7] = BitConverter.GetBytes((int)lista[1])[0];
            ret[8] = BitConverter.GetBytes((int)lista[58])[0];

            return ret;
        }
    }
}
