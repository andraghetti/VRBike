using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace GloveLibrary
{
    class DTWWindowSlide
    {
        private ArrayList _campione;
        private ArrayList _finestra;
        private ArrayList _normalizzati;
        private int _windowSize;
        private float[,] _distanzaEuclidea;
        private float[,] _dtw;
        private ArrayList _percorso;
        private float _costo;

        private int _dataUsage;//1 - accelerometri, 2 - giroscopi, 3 - entrambi

        public DTWWindowSlide(ArrayList campione, int windowSize = 30, int confronto = 3)
        {
            _campione = campione;
            _finestra = new ArrayList();
            _windowSize = windowSize;
            if (confronto > 3 || confronto < 1)
            {
                confronto = 3;
            }
            _dataUsage = confronto;
            _distanzaEuclidea = new float[_campione.Count, _windowSize];
            _dtw = new float[_campione.Count, _windowSize];
            _percorso = new ArrayList();
            _normalizzati = new ArrayList();
        }

        public bool ready()
        {
            return _finestra.Count == _windowSize;
        }

        public bool add(float[] quanto)
        {
            _finestra.Add(quanto);

            while (_finestra.Count > _windowSize)
            {
                _finestra.RemoveAt(0);
            }

            if (_finestra.Count == _windowSize)
            {
                _normalizzati.Clear();
                foreach (float[] tmp in _finestra)
                {
                    _normalizzati.Add(new float[] { tmp[0], tmp[1], tmp[2], tmp[3], tmp[4], tmp[5] });
                }
                normDeviazioneStandard(ref _normalizzati);
                genDati();
                return true;
            }
            else
            {
                return false;
            }
        }

        private void genDati()
        {
            float[] a, b;
            for (int x = 0; x < _campione.Count; x++)
            {
                a = (float[])_campione[x];
                for (int y = 0; y < _windowSize; y++)
                {
                    b = (float[])_normalizzati[y];
                    _distanzaEuclidea[x, y] = 0;
                    for (int index = (_dataUsage != 2 ? 0 : 3); index < (_dataUsage != 1 ? 6 : 3); index++)
                    {
                        _distanzaEuclidea[x, y] += (float)Math.Pow(a[index] - b[index], 2);
                    }
                    _distanzaEuclidea[x, y] = (float)Math.Sqrt(_distanzaEuclidea[x, y]);
                }
            }

            _dtw[0, 0] = _distanzaEuclidea[0, 0];

            for (int x = 1; x < _campione.Count; x++)
            {
                _dtw[x, 0] = _distanzaEuclidea[x, 0] + _dtw[x - 1, 0];
            }

            for (int y = 1; y < _windowSize; y++)
            {
                _dtw[0, y] = _distanzaEuclidea[0, y] + _dtw[0, y - 1];
            }

            for (int x = 1; x < _campione.Count; x++)
            {
                for (int y = 1; y < _windowSize; y++)
                {
                    _dtw[x, y] = _distanzaEuclidea[x, y] + Math.Min(_dtw[x - 1, y], Math.Min(_dtw[x - 1, y - 1], _dtw[x, y - 1]));
                }
            }

            _percorso.Clear();
            _costo = 0;

            int i = _campione.Count - 1;
            int j = _windowSize - 1;

            _percorso.Add(new float[] { i, j, _dtw[i, j] });
            _costo += _dtw[i, j];

            while (i > 0 || j > 0)
            {
                if (i == 0)
                {
                    j--;
                }
                else if (j == 0)
                {
                    i--;
                }
                else
                {
                    if (_dtw[i - 1, j - 1] == Math.Min(_dtw[i - 1, j - 1], Math.Min(_dtw[i - 1, j], _dtw[i, j - 1])))
                    {
                        i -= 1;
                        j -= 1;
                    }
                    else if (_dtw[i, j - 1] == Math.Min(_dtw[i - 1, j - 1], Math.Min(_dtw[i - 1, j], _dtw[i, j - 1])))
                    {
                        j -= 1;
                    }
                    else
                    {
                        i -= 1;
                    }
                }
                _percorso.Add(new float[] { i, j, _dtw[i, j] });
                _costo += _dtw[i, j];
            }
            _costo /= _percorso.Count;
        }

        public float Costo
        {
            get
            {
                if (!ready())
                    return float.NaN;

                return _costo;
            }
        }

        //funzioni per normalizzazione onda
        private void normDeviazioneStandard(ref ArrayList list)
        {
            float x, y, z, a, b, c, m0, m1, m2, m3, m4, m5;

            x = varianza(ref list, 0);
            y = varianza(ref list, 1);
            z = varianza(ref list, 2);
            a = varianza(ref list, 3);
            b = varianza(ref list, 4);
            c = varianza(ref list, 5);

            m0 = media(list, 0);
            m1 = media(list, 1);
            m2 = media(list, 2);
            m3 = media(list, 3);
            m4 = media(list, 4);
            m5 = media(list, 5);


            for (int i = 0; i < list.Count; i++)
            {
                ((float[])list[i])[0] -= m0;
                ((float[])list[i])[0] /= x;

                ((float[])list[i])[1] -= m1;
                ((float[])list[i])[1] /= y;

                ((float[])list[i])[2] -= m2;
                ((float[])list[i])[2] /= z;

                //giroscopio
                ((float[])list[i])[3] -= m3;
                ((float[])list[i])[3] /= a;

                ((float[])list[i])[4] -= m4;
                ((float[])list[i])[4] /= b;

                ((float[])list[i])[5] -= m5;
                ((float[])list[i])[5] /= c;
            }
        }

        private float varianza(ref ArrayList list, int val)
        {
            float varianza = 0;

            float m = media(list, val);

            foreach (float[] tmp in list)
            {
                varianza += (float)Math.Pow(tmp[val] - m, 2);
            }

            varianza /= (list.Count);//utilizzo la formula n perchè so di quanti membri è composta la popolazione altrimenti dovevo usare N-1

            return (float)Math.Sqrt(varianza);
        }

        private float media(ArrayList list, int val)
        {
            float media = 0;

            foreach (float[] tmp in list)
            {
                media += tmp[val];
            }
            media /= list.Count;

            return media;
        }
    }
}
