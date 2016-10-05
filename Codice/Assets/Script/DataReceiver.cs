﻿using System;
using UnityEditor;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace SerialLibrary
{
    public class DataReceiver
    {
        public bool go;
        private SerialPort serial;
        private string _com;
        public event Action<int, float> PedalataTrovata;

		//========================COSTRUTTORE========================//
        public DataReceiver(string ComName)
        {
            go = true;
            _com = ComName;
        }
		
		//=========FUNZIONI PRIVATE per gestione porta seriale=======//
        private bool openPort()
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
        private bool closePort()
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
        private string getData()
        {
            String tmp = "";
            if (serial.IsOpen && go)
                tmp = serial.ReadLine();
            return tmp;
        }

		//============FUNZIONI PUBBLICHE per gestione dati ==========//
        public void stop()
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
					//ricezione dati dalla porta seriale
                    data = getData();
                    
                    if (data != "")
                    {
                        //gestione sterzo
                        tmp = "";
                        if (data[0] != '0')
                            tmp += data[0];
                        tmp += data[1];
                        sterzo = int.Parse(tmp);

                        //gestione velocità
                        tmp = "";
                        if (data[3] != '0')
                            tmp += data[3];
                        tmp += data.Substring(4);
                        speed = float.Parse(tmp);
                        if (speed > 0 && data[2] == '-')
                            speed *= -1;

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
            MotoController.msgToDebug("Thread: processo terminato.");
        }
    }
}
