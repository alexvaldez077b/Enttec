using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FTD2XX_NET;
namespace RDM
{
    class msg
    {
        public bool status = false;
        public int TransactionNumber = 0;
        public byte[] _pid = { 0, 0 };
        public byte[] data = null; 
        public string message = "";

        public string dataTostring = " ";

        public void info()
        {
            Console.WriteLine(string.Format("STATUS: {0} \nTN: {1} PID:{2}{3}\nMESSAGE: {4} ", status,TransactionNumber, _pid[0].ToString("X2"), _pid[1].ToString("X2"), message ));
            if(data != null)
            {
                Console.WriteLine("DATA:");
                foreach (var item in data)
                {
                    Console.Write(item.ToString("X2") + " ");
                }
            }
        }

        public string _ToString()
        {

            string buffer = "";

            if (this.data != null)
            {
                foreach (var item in this.data)
                {
                    buffer += (item.ToString("X2") + " - ");

                }

            }

            return buffer;

        }
        
    }



    class Enttec
    {

        FTDI dmx512Pro = new FTDI();

        byte [] source = { 0x36,0x38,0xEE,0xEE,0xEE,0xEE };
        

        int TN = 0;

       public Enttec()
        {

            dmx512Pro.OpenByDescription("DMX USB PRO");

            this.dmx_get_serial();


        }

        public bool isOpen()
        {
            return dmx512Pro.IsOpen;
        }

        public void dmx_get_serial()
        {

            byte[] payload = { 0x7e, 10 , 0 , 0,  0xe7 };

            List<byte> serial = new List<byte>();
            byte[] getserial = { 0x7e, 10, 0, 0, 0xe7 };



            if( !dmx512Pro.IsOpen )
                dmx512Pro.OpenByDescription("DMX USB PRO");

            uint tx = 0;

            dmx512Pro.Write(getserial, getserial.Length, ref tx);

            byte[] buffer = new byte[10];

            dmx512Pro.Read(buffer, 9, ref tx);


            if (buffer[0] == 0x7e && buffer[1] == 0x0A)
            {
                serial.Add(buffer[4]);
                serial.Add(buffer[5]);
                serial.Add(buffer[6]);
                serial.Add(buffer[7]);

            }

            setSource(serial.ToArray().Reverse().ToArray());

            Console.WriteLine("DMX SERIAL NUMBER: ");

            foreach (var item in serial.ToArray().Reverse())
            {
                Console.Write(item.ToString("X2") + " ");
            }

            Console.WriteLine(" ");


        }


        public byte [] buildToSend(byte flag, byte [] payload)
        {

            List<Byte> load = new List<byte>();

            load.Add(0x7E); //start package
            load.Add(flag);

            load.AddRange( BitConverter.GetBytes((UInt16)payload.Length) );

            load.AddRange(payload);

            load.Add(0xE7); //end package

            return load.ToArray();



        }

        public void setSource( byte[] src)
        {
            source[0] = 0x45;
            source[1] = 0x4E;

            source[2] = src[0];
            source[3] = src[1];
            source[4] = src[2];
            source[5] = src[3];

            

        }


        public byte [] packet_builder( byte[] destination, byte CC ,UInt16 PID, byte [] data )
        {

            List<Byte> packet = new List<byte>();
            UInt16 checksum = 0;
            

            packet.Add(0xCC);               //start code
            packet.Add(0x01);               //sub start code
            packet.Add(0);                  //message length
            packet.AddRange(destination);   //destination UID
            packet.AddRange(source);        //source UID

            packet.Add( (byte) TN++);                  //Transaction number
            packet.Add(1);                  //Port ID
            packet.Add(0);                  //message count

            packet.Add(0);                  //sub device
            packet.Add(0);                  //sub device
            //message data block

            packet.Add(CC);

            packet.AddRange(BitConverter.GetBytes(PID).Reverse());

            if (data != null)
            {
                packet.Add((byte) data.Length  );
                packet.AddRange(data);
            }
            else
            {
                packet.Add(0);
            }

            packet[2] = (byte)packet.Count();


            foreach (var item in packet.ToArray())
            {
                checksum += item;
            }

            packet.AddRange(BitConverter.GetBytes(checksum).Reverse());

            //this.write(packet.ToArray());
            
            return packet.ToArray();

        }


        public object write(byte[] payload)
        {


            byte[] Start = { 0x00 };
            byte[] MAB = new byte[256];

            if(!dmx512Pro.IsOpen)
                dmx512Pro.OpenByDescription("DMX USB PRO");


            //DMX_PRO.SetDataCharacteristics(FTDI.FT_DATA_BITS.FT_BITS_8, FTDI.FT_STOP_BITS.FT_STOP_BITS_2, FTDI.FT_PARITY.FT_PARITY_NONE);

            dmx512Pro.Purge(FTDI.FT_PURGE.FT_PURGE_RX);
            dmx512Pro.Purge(FTDI.FT_PURGE.FT_PURGE_TX);


            //DMX_PRO.SetBreak(true);
            dmx512Pro.SetTimeouts(1000, 500);
            
            uint tx = 0;

            dmx512Pro.SetBaudRate(250000);
            

            dmx512Pro.SetBreak(true);

            System.Threading.Thread.Sleep(10);
            dmx512Pro.SetBreak(false);

            dmx512Pro.Write(MAB, 0, ref tx);
            dmx512Pro.Write(Start, Start.Length, ref tx);


            //DMX_PRO.Purge(FTDI.FT_PURGE.FT_PURGE_TX);
            uint rx = 0;
            int _try = 0;
            msg _out = (msg)parse(payload , 0 , 0 , null );

            _out.info();


            do
            {
                Console.WriteLine(string.Format("Try: {0} ", ++_try));
                dmx512Pro.Write(payload, payload.Length, ref tx);
                



                do
                {
                    dmx512Pro.GetTxBytesWaiting(ref tx);
                    //Console.WriteLine("Bytes Waiting: " + tx);
                    System.Threading.Thread.Sleep(100);

                } while (tx != 0);


                
                dmx512Pro.GetRxBytesAvailable(ref rx);


            } while (rx == 0  );

            
            byte line_status = 0;
            byte[] buffer = new byte[rx];
            dmx512Pro.Read(buffer, rx, ref rx);

           
            msg _in = (msg)parse(buffer,1,_out.TransactionNumber,_out._pid);
            ///////////////////////////////////////////////////////////////////
            _in.info();
            Console.WriteLine("\n");
            ///////////////////////////////////////////////////////////////////
            return _in;

        }

        public object parse(byte[] package, int type, int tn, byte [] _pid )
        {
            msg _msg = new msg();
            List<byte> rdm = new List<byte>();

            if (package.Length > 0 && package != null && package[0] == 0x7E)
            {
                UInt16 size = package[2];

                if (package[1] == 5)
                    size--;

                for (int i = 0; i < size; i++)
                {
                    int index = 0;
                    if (package[1] == 5)
                        index = 5;
                    else
                        index = 4;
                    rdm.Add(package[index + i]);

                }
                Console.WriteLine("=========================================================================");
                foreach (var item in rdm.ToArray())
                {
                    Console.Write(item.ToString("X2") + " ");
                }
                Console.WriteLine("\n=========================================================================");

                if (rdm.Count > 16)
                {
                    _msg.TransactionNumber = rdm[15];

                    _msg._pid[0] = rdm[21];
                    _msg._pid[1] = rdm[22];
                    switch (type)
                    {
                        case 0:
                            break;
                        case 1:
                                /*
                                   RESPONSE_TYPE_ACK            0x00
                                   RESPONSE_TYPE_ACK_TIMER      0x01
                                   RESPONSE_TYPE_NACK_REASON    0x02 
                                   RESPONSE_TYPE_ACK_OVERFLOW   0x03 
                                 */

                                if ( (rdm[16] == 0 || rdm[16] == 0x01) && tn == _msg.TransactionNumber && (_pid[0] == _msg._pid[0] && _pid[1] == _msg._pid[1]))
                                {
                                    _msg.status = true;
                                }

                                switch (rdm[16])
                                {
                                    case 0x00:
                                        _msg.message = "RESPONSE_TYPE_ACK";

                                        break;
                                    case 0x01:
                                        _msg.message = "RESPONSE_TYPE_ACK_TIMER";

                                        break;
                                    case 0x02:
                                        _msg.message = "RESPONSE_TYPE_ACK_REASON";

                                        break;
                                    case 0x03:
                                        _msg.message = "RESPONSE_TYPE_ACK_OVERFLOW";

                                        break;
                                    default:
                                        break;
                                }
                            _msg.data = new byte[rdm[23]];

                            for (int i = 0; i < _msg.data.Length; i++)
                            {

                                _msg.data[i] = rdm[24 + i];

                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            return _msg;

            
            

        }

    }
}
