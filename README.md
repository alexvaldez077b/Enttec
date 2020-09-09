Enttec


example

            //dest is an array hexademinal destination address [0x36, 0x38, 0x12 , 0x34, 0x56 , 0x78]
            //
            
            byte[] get_info = devices.packet_builder(dest, devices.GET, devices.GETINFO, null);
                                                     parameter 
                                                     destiantion   :[] array 
                                                     Command class :hexadecimal BYTE
                                                     PID           :hex 2Bytes
                                                     data          :[] array payload
            msg serialstatus;
            
       
            serialstatus = (msg)devices.write(devices.buildToSend(7, set_serial)); Console.WriteLine(serialstatus._ToString() + " || " + serialstatus.message);
            
