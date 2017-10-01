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

namespace MySocketClient
{
    public partial class Form1 : Form
    {
        private ushort packet_cnt;
        public delegate void UpdateTextBox1Callback(string strMessage);
        public delegate void UpdateTextBox2Callback(string strMessage);

        public Form1()
        {
            InitializeComponent();
            packet_cnt = 1;
        }

        public void updateTextBox1(string data)
        {
            this.textBox1.AppendText(data + "\r\n");
        }

        public void updateTextBox2(string data)
        {
            this.textBox2.AppendText(data + "\r\n");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Packet cp = new Packet();
            cp.init_CLIENT_P1(packet_cnt++);

            this.textBox1.Text = "Packet: " + cp.toHexString() + "\r\n";
            this.textBox1.Text += "------------------------------------------" + "\r\n";
            this.textBox1.Text += "PCK_SIZE = " + cp.getPCK_SIZE() + " (" + cp.getPCK_SIZE_HexString() + ")" + "\r\n";
            this.textBox1.Text += "PCK_CNT  = " + cp.getPCK_CNT() + " (" + cp.getPCK_CNT_HexString() + ")" + "\r\n";
            this.textBox1.Text += "PCK_ID   = " + cp.getPCK_ID() + " (" + cp.getPCK_ID_HexString() + ")" + "\r\n";
            this.textBox1.Text += "PCK_CRC  = " + cp.getPCK_CRC_HexString() + "\r\n";
            this.textBox1.Text += "------------------------------------------" + "\r\n";


            SendThread st = new SendThread(cp.getRawPacket(), this, this.textBoxIP.Text);

            Thread t = new Thread(new ThreadStart(st.ThreadProc));
            t.Start();
        }


        private void button2_Click(object sender, EventArgs e)
        {
            Packet cp = new Packet();
            cp.init_CLIENT_P2(packet_cnt++, (byte)0xAB);

            this.textBox1.Text = "Packet: " + cp.toHexString() + "\r\n";
            this.textBox1.Text += "------------------------------------------" + "\r\n";
            this.textBox1.Text += "PCK_SIZE = " + cp.getPCK_SIZE() + " (" + cp.getPCK_SIZE_HexString() + ")" + "\r\n";
            this.textBox1.Text += "PCK_CNT  = " + cp.getPCK_CNT() + " (" + cp.getPCK_CNT_HexString() + ")" + "\r\n";
            this.textBox1.Text += "PCK_ID   = " + cp.getPCK_ID() + " (" + cp.getPCK_ID_HexString() + ")" + "\r\n";
            this.textBox1.Text += "PCK_CRC  = " + cp.getPCK_CRC_HexString() + "\r\n";
            this.textBox1.Text += "------------------------------------------" + "\r\n";


            SendThread st = new SendThread(cp.getRawPacket(), this, this.textBoxIP.Text);

            Thread t = new Thread(new ThreadStart(st.ThreadProc));
            t.Start();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Packet cp = new Packet();
            byte[] more_data = new byte[3] { 0x01, 0x02, 0x03 };
            cp.init_CLIENT_P3(packet_cnt++, more_data);

            this.textBox1.Text = "Packet: " + cp.toHexString() + "\r\n";
            this.textBox1.Text += "------------------------------------------" + "\r\n";
            this.textBox1.Text += "PCK_SIZE = " + cp.getPCK_SIZE() + " (" + cp.getPCK_SIZE_HexString() + ")" + "\r\n";
            this.textBox1.Text += "PCK_CNT  = " + cp.getPCK_CNT() + " (" + cp.getPCK_CNT_HexString() + ")" + "\r\n";
            this.textBox1.Text += "PCK_ID   = " + cp.getPCK_ID() + " (" + cp.getPCK_ID_HexString() + ")" + "\r\n";
            this.textBox1.Text += "PCK_CRC  = " + cp.getPCK_CRC_HexString() + "\r\n";
            this.textBox1.Text += "------------------------------------------" + "\r\n";


            SendThread st = new SendThread(cp.getRawPacket(), this, this.textBoxIP.Text);

            Thread t = new Thread(new ThreadStart(st.ThreadProc));
            t.Start();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Packet cp = new Packet();
            byte[] more_data = new byte[5] { 0x30, 0x66, 0x45, 0x22, 0x5d };
            cp.init_CLIENT_P4(packet_cnt++, more_data);

            this.textBox1.Text = "Packet: " + cp.toHexString() + "\r\n";
            this.textBox1.Text += "------------------------------------------" + "\r\n";
            this.textBox1.Text += "PCK_SIZE = " + cp.getPCK_SIZE() + " (" + cp.getPCK_SIZE_HexString() + ")" + "\r\n";
            this.textBox1.Text += "PCK_CNT  = " + cp.getPCK_CNT() + " (" + cp.getPCK_CNT_HexString() + ")" + "\r\n";
            this.textBox1.Text += "PCK_ID   = " + cp.getPCK_ID() + " (" + cp.getPCK_ID_HexString() + ")" + "\r\n";
            this.textBox1.Text += "PCK_CRC  = " + cp.getPCK_CRC_HexString() + "\r\n";
            this.textBox1.Text += "------------------------------------------" + "\r\n";


            SendThread st = new SendThread(cp.getRawPacket(), this, this.textBoxIP.Text);

            Thread t = new Thread(new ThreadStart(st.ThreadProc));
            t.Start();
        }


        public class SendThread
        {
            private byte[] pck_data;
            private Form1 form;
            enum RX_STATE { WAIT_FOR_SYNC, RX_LENGTH, RX_DATA, RX_CRC, RX_END }
            private string server_ip;


            public SendThread(byte[] d, Form1 f, string ip)
            {
                pck_data = d;
                form = f;
                server_ip = ip;
            }

            public void ThreadProc()
            {
                Form1.UpdateTextBox1Callback tb1 = new Form1.UpdateTextBox1Callback(form.updateTextBox1);
                Form1.UpdateTextBox2Callback tb2 = new Form1.UpdateTextBox2Callback(form.updateTextBox2);

                form.Invoke(tb1, new object[] { "sendPacketToServer() started" });
                TcpClient client = new TcpClient();

                try
                {

                    form.Invoke(tb1, new object[] { "Connecting..." });

                    client.Connect(server_ip, 7777);

                    Socket soc = client.Client;
                    soc.SendTimeout = 10000;
                    soc.ReceiveTimeout = 10000;
                    soc.NoDelay = true;

                    form.Invoke(tb1, new object[] { "Connected" });

                    soc.Send(pck_data, pck_data.Length, 0);

                    form.Invoke(tb1, new object[] { "Data sent, waiting for response..." });

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
                    bool crc_error = false;
                    bool end_error = false;
                    bool rx_len_error = false;
                    bool rxOK = false;
                    byte[] rxPacket = new byte[512];//cia tik atvaizdavimui reikalinga!
                    int idx = 0;

                    RX_STATE rxState = RX_STATE.WAIT_FOR_SYNC;

                    while (pck_error == false && rxOK == false && ((bytes = soc.Receive(data, 1, 0)) > 0))
                    {
                        //form.Invoke(tb1, new object[] { "byte received, bytes_size=" + bytes });
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
                                        form.Invoke(tb2, new object[] { "RX_STATE.RX_LENGTH: pck_error" });
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
                                        form.Invoke(tb2, new object[] { "BAD CRC: calcCRC=" + word2hexstr(rxCRC) + " | crc=" + word2hexstr(rxCRC) });
                                        form.Invoke(tb2, new object[] { "packet: " + toHexString(rxPacket) });
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
                                    form.Invoke(tb2, new object[] { "MISSING END! ..." });
                                    form.Invoke(tb2, new object[] { "packet discarded: " + toHexString(rxPacket) });
                                    pck_error = true;
                                    end_error = true;
                                }
                                else
                                {
                                    rxPacket[idx++] = b;
                                    rxOK = true;
                                }

                                break;
                        }
                    }

                    if (rxOK == true)
                    {
                        byte[] p = new byte[idx];
                        Array.Copy(rxPacket, p, idx);
                        Packet server_packet = new Packet();
                        server_packet.setPacket(p);
                        form.Invoke(tb2, new object[] { "OK: packet=" + server_packet.toHexString() });
                    }
                    else
                    {
                        form.Invoke(tb2, new object[] { "RX failed" });
                    }


                    client.Close();

                    form.Invoke(tb1, new object[] { "Closed" });

                }
                catch (Exception ex)
                {
                    form.Invoke(tb2, new object[] { ex.Message });
                    client.Close();
                }

            }

            private ushort calcCRC(byte[] packet, int length)
            {
                ushort crc = (ushort)0xffff;
                ushort index;
                byte b;

                for (index = 4; index < length - 2; index++)//-2 : atmetam CRC kuri gavom ir idejom i packet
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
        }
    }
}
