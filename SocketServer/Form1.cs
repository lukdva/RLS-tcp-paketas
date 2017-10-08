using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Packets;

namespace MySocketServer
{
    public partial class Form1 : Form
    {
        private ushort packet_cnt;
        TcpListener server = null;
        Int32 port = 7777;
        Boolean srvRunning = false;

        private delegate void UpdateTextBox1Callback(string strMessage);
        private delegate void UpdateTextBox2Callback(string strMessage);
        private Thread listenThread;

        enum RX_STATE { WAIT_FOR_SYNC, RX_LENGTH, RX_DATA, RX_CRC, RX_END };

        public Form1()
        {
            InitializeComponent();
            packet_cnt = 1;
        }

        private void updateTextBox1(string data)
        {
            this.textBox1.Text += data + "\r\n";
        }

        private void updateTextBox2(string data)
        {
            this.textBox2.Text += data + "\r\n";
        }

        private void button1_Click(object sender, EventArgs e)
        {

            try
            {
                server = new TcpListener(IPAddress.Parse(this.textBoxIP.Text), port);

                server.Start();

                srvRunning = true;
                this.listenThread = new Thread(accept_n_process);
                this.listenThread.Start();

                this.button1.Enabled = false;
                this.button2.Enabled = true;
            }
            catch (SocketException ex)
            {
                this.textBox1.Text = "SocketException: " + ex;
            }
            catch (Exception exx)
            {
                this.textBox1.Text = "Exception: " + exx;
            }
        }

        private void accept_n_process()
        {
            UpdateTextBox1Callback tb1 = new UpdateTextBox1Callback(updateTextBox1);
            UpdateTextBox2Callback tb2 = new UpdateTextBox2Callback(updateTextBox2);

            this.Invoke(tb1, new object[] { "accept_n_process started" });
            this.Invoke(tb2, new object[] { "accept_n_process started" });

            TcpClient client = null;

            try
            {
                while (srvRunning == true)
                {

                    client = server.AcceptTcpClient();
                    
                    Socket soc = client.Client;
                    soc.SendTimeout = 10000;
                    soc.ReceiveTimeout = 10000;
                    soc.NoDelay = true;

                    byte b = (byte)0x00;
                    byte[] data = new byte[1];
                    int bytes = 0;
                    int counter = 0;
                    ushort sizeOfReceivingFrame = (ushort)0x0000;
                    ushort rxCRC = (ushort)0x0000;
                    ushort crc = (ushort)0x0000;
                    byte crc_hb = (byte)0x00;
                    byte crc_lb = (byte)0x00;
                    bool pck_error = false;
                    bool rxOK = false;
                    bool crc_error = false;
                    bool end_error = false;
                    bool rx_len_error = false;
                    byte[] rxPacket = new byte[512];//cia tik atvaizdavimui reikalinga!
                    int idx = 0;

                    RX_STATE rxState = RX_STATE.WAIT_FOR_SYNC;

                    while ((pck_error == false && rxOK == false && (bytes = soc.Receive(data, 1, 0)) > 0))
                    {
                        //this.Invoke(tb1, new object[] { "byte received, bytes_size=" + bytes });
                        int a = 0;
                        b = (byte)data[0];
                        switch (rxState)
                        {
                            case RX_STATE.WAIT_FOR_SYNC:
                                if (b != (byte)0x21)
                                {
                                    counter = 0;
                                    break;
                                }

                                counter++;
                                rxPacket[idx++] = b;

                                if (counter == 2)
                                {
                                    counter = 0;
                                    rxState = RX_STATE.RX_LENGTH;
                                }
                                break;
                            case RX_STATE.RX_LENGTH:
                                counter++;

                                if (counter == 1)
                                {
                                    sizeOfReceivingFrame = b;
                                    rxPacket[idx++] = b;
                                    break;
                                }

                                if (counter == 2)
                                {
                                    counter = 0;
                                    sizeOfReceivingFrame = (ushort)((sizeOfReceivingFrame << 8) | b);
                                    sizeOfReceivingFrame -= 3;// atmetam CRC ir END
                                    if (sizeOfReceivingFrame <= 0)
                                    {
                                        pck_error = true;
                                        rx_len_error = true;
                                        this.Invoke(tb1, new object[] { "RX_STATE.RX_LENGTH: pck_error" });
                                        break;
                                    }
                                    rxPacket[idx++] = b;
                                    rxState = RX_STATE.RX_DATA;
                                    break;
                                }
                                break;
                            case RX_STATE.RX_DATA:
                                rxPacket[idx++] = b;
                                counter++;
                                if (counter == sizeOfReceivingFrame)
                                {
                                    counter = 0;
                                    rxState = RX_STATE.RX_CRC;
                                    break;
                                }
                                break;
                            case RX_STATE.RX_CRC:
                                counter++;
                                if (counter == 1)
                                {
                                    crc_hb = b;
                                    rxPacket[idx++] = b;
                                    break;
                                }

                                if (counter == 2)
                                {
                                    crc_lb = b;
                                    rxPacket[idx++] = b;
                                    counter = 0;

                                    crc = (ushort)((crc_hb << 8) | crc_lb);

                                    rxCRC = calcCRC(rxPacket, idx);

                                    if (crc != rxCRC)
                                    {
                                        this.Invoke(tb1, new object[] { "BAD CRC: calcCRC=" + word2hexstr(rxCRC) + " | crc=" + word2hexstr(rxCRC) });
                                        this.Invoke(tb1, new object[] { "packet: " + toHexString(rxPacket) });
                                        pck_error = true;
                                        crc_error = true;
                                    }
                                    else
                                    {
                                        rxState = RX_STATE.RX_END;
                                    }

                                    break;
                                }
                                break;
                            case RX_STATE.RX_END:
                                counter = 0;
                                if (b != (byte)0x2E)   // discard
                                {
                                    this.Invoke(tb1, new object[] { "MISSING END! ..." });
                                    this.Invoke(tb1, new object[] { "packet discarded: " + toHexString(rxPacket) });
                                    pck_error = true;
                                    end_error = true;
                                }
                                else
                                {
                                    //this.Invoke(tb1, new object[] { "rxOK! ..." });
                                    rxPacket[idx++] = b;
                                    rxOK = true;
                                }

                                // 

                                break;
                        }
                    }

                    if (rxOK == true) // response
                    {
                        byte[] p = new byte[idx];
                        Array.Copy(rxPacket, p, idx);
                        Packet client_packet = new Packet();
                        client_packet.setPacket(p);
                        this.Invoke(tb1, new object[] { "OK: packet=" + client_packet.toHexString() });

                        Packet server_packet = processAnswer(client_packet);

                        if (server_packet != null)
                        {
                            soc.Send(server_packet.getRawPacket(), server_packet.getRawPacket().Length, 0);
                            this.Invoke(tb2, new object[] { "packet=" + server_packet.toHexString() });
                        }
                        else
                            this.Invoke(tb2, new object[] { "failed at processAnswer(): server_packet = null" });
                    }
                    else
                    {
                        this.Invoke(tb1, new object[] { "RX failed" });
                    }

                    client.Close();
                    this.Invoke(tb1, new object[] { "Connection closed" });
                }
            }
            catch (SocketException ex)
            {
                this.Invoke(tb1, new object[] { ex.Message });
                this.Invoke(tb2, new object[] { ex.Message });
                client.Close();
                this.Invoke(tb1, new object[] { "Connection closed after SocketException" });
            }
            catch (Exception exx)
            {
                this.Invoke(tb1, new object[] { exx.Message });
                this.Invoke(tb2, new object[] { exx.Message });
                client.Close();
                this.Invoke(tb1, new object[] { "Connection closed after Exception" });
            }

        }
        private void accept_n_process2()
        {
            UpdateTextBox1Callback tb1 = new UpdateTextBox1Callback(updateTextBox1);
            UpdateTextBox2Callback tb2 = new UpdateTextBox2Callback(updateTextBox2);

            this.Invoke(tb1, new object[] { "accept_n_process started" });
            this.Invoke(tb2, new object[] { "accept_n_process started" });

            TcpClient client = null;

            try
            {
                while (srvRunning == true)
                {

                    client = server.AcceptTcpClient();

                    Socket soc = client.Client;
                    soc.SendTimeout = 10000;
                    soc.ReceiveTimeout = 10000;
                    soc.NoDelay = true;

                    byte b = (byte)0x00;
                    byte previousByte = (byte)0x00;
                    byte[] data = new byte[1];
                    int bytes = 0;
                    int counter = 0;
                    uint sizeOfReceivingFrame = (ushort)0x00000000;
                    ushort rxCRC = (ushort)0x0000;
                    ushort crc = (ushort)0x0000;
                    byte crc_hb = (byte)0x00;
                    byte crc_lb = (byte)0x00;
                    bool pck_error = false;
                    bool rxOK = false;
                    bool crc_error = false;
                    bool end_error = false;
                    bool rx_len_error = false;
                    byte[] rxPacket = new byte[512];//cia tik atvaizdavimui reikalinga!
                    int idx = 0;

                    RX_STATE rxState = RX_STATE.WAIT_FOR_SYNC;

                    while ((pck_error == false && rxOK == false && (bytes = soc.Receive(data, 1, 0)) > 0))
                    {
                        //this.Invoke(tb1, new object[] { "byte received, bytes_size=" + bytes });

                        b = (byte)data[0];
                        switch (rxState)
                        {
                            case RX_STATE.WAIT_FOR_SYNC:
                                if (b == (byte)0x4c && counter == 0)
                                {
                                    counter++;
                                    rxPacket[idx++] = b;
                                }
                                if (b == (byte)0x55 && counter == 1)
                                {
                                    counter++;
                                    rxPacket[idx++] = b;
                                }

                                if (counter == 2)
                                {
                                    counter = 0;
                                    rxState = RX_STATE.RX_LENGTH;
                                }
                                break;
                            case RX_STATE.RX_LENGTH:
                                counter++;

                                if (counter != 4)
                                {
                                    sizeOfReceivingFrame = (uint)((sizeOfReceivingFrame << 8*(counter-1)) | b);
                                    rxPacket[idx++] = b; 
                                    break;
                                }

                                sizeOfReceivingFrame = (ushort)((sizeOfReceivingFrame << 24) | b);
                                sizeOfReceivingFrame -= 5;// atmetam CRC ir END
                                if (sizeOfReceivingFrame <= 0)
                                {
                                    pck_error = true;
                                    rx_len_error = true;
                                    this.Invoke(tb1, new object[] { "RX_STATE.RX_LENGTH: pck_error" });
                                    break;
                                }
                                rxPacket[idx++] = b;
                                rxState = RX_STATE.RX_DATA;
                                break;
                            case RX_STATE.RX_DATA:
                                rxPacket[idx++] = b;
                                counter++;
                                if (counter == sizeOfReceivingFrame)
                                {
                                    counter = 0;
                                    rxState = RX_STATE.RX_CRC;
                                    break;
                                }
                                break;
                            case RX_STATE.RX_CRC:
                                counter++;
                                if (counter == 1)
                                {
                                    crc_hb = b;
                                    rxPacket[idx++] = b;
                                    break;
                                }

                                if (counter == 2)
                                {
                                    crc_lb = b;
                                    rxPacket[idx++] = b;
                                    counter = 0;

                                    crc = (ushort)((crc_hb << 8) | crc_lb);

                                    rxCRC = calcCRC(rxPacket, idx);

                                    if (crc != rxCRC)
                                    {
                                        this.Invoke(tb1, new object[] { "BAD CRC: calcCRC=" + word2hexstr(rxCRC) + " | crc=" + word2hexstr(rxCRC) });
                                        this.Invoke(tb1, new object[] { "packet: " + toHexString(rxPacket) });
                                        pck_error = true;
                                        crc_error = true;
                                    }
                                    else
                                    {
                                        counter = 0;
                                        rxState = RX_STATE.RX_END;
                                    }

                                    break;
                                }
                                break;
                            case RX_STATE.RX_END:
                                counter++;
                                bool isEndingValid = true;
                                switch (counter)
                                {
                                    case (1):
                                        if (b != (byte)0x44)
                                            isEndingValid = false;
                                    break;
                                    case (2):
                                        if (b != (byte)0x56)
                                            isEndingValid = false;
                                        break;
                                    case (3):
                                        if (b != (byte)0x41)
                                            isEndingValid = false;
                                        break;
                                }
                                
                                if (isEndingValid == false)   // discard
                                {
                                    this.Invoke(tb1, new object[] { "MISSING END! ..." });
                                    this.Invoke(tb1, new object[] { "packet discarded: " + toHexString(rxPacket) });
                                    pck_error = true;
                                    end_error = true;
                                }
                                else
                                {
                                    //this.Invoke(tb1, new object[] { "rxOK! ..." });
                                    rxPacket[idx++] = b;
                                    if(counter == 3)
                                        rxOK = true;
                                }

                                // 

                                break;
                        }
                    }

                    if (rxOK == true) // response
                    {
                        byte[] p = new byte[idx];
                        Array.Copy(rxPacket, p, idx);
                        Packet client_packet = new Packet();
                        client_packet.setPacket(p);
                        this.Invoke(tb1, new object[] { "OK: packet=" + client_packet.toHexString() });

                        Packet server_packet = processAnswer(client_packet);

                        if (server_packet != null)
                        {
                            soc.Send(server_packet.getRawPacket(), server_packet.getRawPacket().Length, 0);
                            this.Invoke(tb2, new object[] { "packet=" + server_packet.toHexString() });
                        }
                        else
                            this.Invoke(tb2, new object[] { "failed at processAnswer(): server_packet = null" });
                    }
                    else
                    {
                        this.Invoke(tb1, new object[] { "RX failed" });
                    }

                    client.Close();
                    this.Invoke(tb1, new object[] { "Connection closed" });
                }
            }
            catch (SocketException ex)
            {
                this.Invoke(tb1, new object[] { ex.Message });
                this.Invoke(tb2, new object[] { ex.Message });
                client.Close();
                this.Invoke(tb1, new object[] { "Connection closed after SocketException" });
            }
            catch (Exception exx)
            {
                this.Invoke(tb1, new object[] { exx.Message });
                this.Invoke(tb2, new object[] { exx.Message });
                client.Close();
                this.Invoke(tb1, new object[] { "Connection closed after Exception" });
            }

        }

        private Packet processAnswer(Packet client_packet)
        {
            UpdateTextBox2Callback tb2 = new UpdateTextBox2Callback(updateTextBox2);
            byte pck_id = client_packet.getPCK_ID();

            switch (pck_id)
            {
                case 0x01:
                    Packet sp1 = new Packet();
                    sp1.init_SERVER_P1(packet_cnt++, client_packet.getPCK_ID(), client_packet.getPCK_CNT());
                    return sp1;
                case 0x03:
                    Packet sp2 = new Packet();
                    sp2.init_SERVER_P2(packet_cnt++, client_packet.getPCK_ID(), client_packet.getPCK_CNT(), (byte)0xCD);
                    return sp2;
                case 0x05:
                    Packet sp3 = new Packet();
                    byte[] more_data = new byte[3] { 0x04, 0x05, 0x06 };
                    sp3.init_SERVER_P3(packet_cnt++, client_packet.getPCK_ID(), client_packet.getPCK_CNT(), more_data);
                    return sp3;
                case 0x07:
                    Packet sp4 = new Packet(); //TODO create new packet 07
                    return sp4;
                default:
                    this.Invoke(tb2, new object[] { "UNKNOWN CLIENT PACKET ID" });
                    break;
            }

            return null;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.srvRunning = false;
            server.Stop();
            this.button1.Enabled = true;
            this.button2.Enabled = false;
        }

        private ushort calcCRC(byte[] packet, int length)
        {
            ushort crc = (ushort)0xffff;
            ushort index;
            byte b;

            for (index = 4; index < length-2; index++)//-2 : atmetam CRC kuri gavom ir idejom i packet
            {
                crc ^= ((ushort)((packet[index] << 8) & 0x0000ffff));
                for (b = 0; b < 8; b++)
                {
                    if ((crc & (ushort)0x8000) == (ushort)0x8000)
                        crc = (ushort)((ushort)((crc << 1) & 0x0000ffff) ^ (ushort)0x1021);
                    else
                        crc = (ushort)((crc << 1) & 0x0000ffff);
                }
            }

            return crc;
        }

        public string toHexString(byte[] packet)
        {
            return BitConverter.ToString(packet);
        }

        private string word2hexstr(ushort data)
        {
            StringBuilder sb = new StringBuilder(6);
            byte hb = (byte)((data >> 8) & 0x000000FF);
            byte lb = (byte)(data & 0x00FF);
            sb.Append("0x");
            sb.AppendFormat("{0:X2}", hb);
            sb.AppendFormat("{0:X2}", lb);
            return sb.ToString();
        }

        private ushort bytes2word(byte hb, byte lb)
        {
            ushort data = (ushort)(hb << 8 | lb);
            return data;
        }
    }
}
