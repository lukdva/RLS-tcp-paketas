using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Packets
{
    
    public class Packet
    {
        protected const   byte    PCK_START = (byte)0x21;
        protected const byte PCK_START1 = (byte)0x4C;
        protected const byte PCK_START2 = (byte)0x55;
        protected ushort PCK_SIZE;
        protected ushort PCK_CNT;
        protected byte PCK_ID;
        protected ushort PCK_CRC;
        protected const byte PCK_END = (byte)0x2E;
        protected const byte PCK_END1 = (byte)0x44;
        protected const byte PCK_END2 = (byte)0x56;
        protected const byte PCK_END3 = (byte)0x41;
        protected bool isNewPacket;

        protected byte PCK_SID;
        protected ushort PCK_SCNT;

        private byte[] packet;
        private int idx;

        public Packet(bool isNewPacket=false)
        {
            this.PCK_SIZE = (ushort)0x0000;
            this.PCK_CNT = (ushort)0x0000;
            this.PCK_ID = (byte)0x00;
            this.PCK_CRC = (ushort)0x0000;
            this.PCK_SID = (byte)0x00;
            this.PCK_SCNT = (ushort)0x0000;
            this.idx = 0;
            this.isNewPacket = isNewPacket;
        }

        #region PACKETU GENERATORIAI

        #region TCP CLIENT packetai
        
        public void init_CLIENT_P1(ushort cnt)
        {
            const ushort pck_size = (ushort)0x0006;
            const byte pck_id = (byte)0x01;

            initPacket(pck_size + 4);
            addByte((byte)PCK_START);
            addByte((byte)PCK_START);
            addWord((ushort)pck_size);
            addWord((ushort)cnt);
            addByte((byte)pck_id);
            addWord((ushort)0x0000);//crc
            addByte((byte)PCK_END);
            updateCRC();
        }

        public void init_CLIENT_P2(ushort cnt, byte data)
        {
            const ushort pck_size = (ushort)0x0007; //6 + 1
            const byte pck_id = (byte)0x03;

            initPacket(pck_size + 4);
            addByte((byte)PCK_START);
            addByte((byte)PCK_START);
            addWord((ushort)pck_size);
            addWord((ushort)cnt);
            addByte((byte)pck_id);
            addByte((byte)data);
            addWord((ushort)0x0000);//crc
            addByte((byte)PCK_END);
            updateCRC();
        }

        public void init_CLIENT_P3(ushort cnt, byte[] more_data)
        {
            ushort pck_size = (ushort)(6 + more_data.Length);
            const byte pck_id = (byte)0x05;

            initPacket(pck_size + 4);
            addByte((byte)PCK_START);
            addByte((byte)PCK_START);
            addWord((ushort)pck_size);
            addWord((ushort)cnt);
            addByte((byte)pck_id);
            for (int i = 0; i < more_data.Length; i++)
            {
                addByte((byte)more_data[i]);
            }
            addWord((ushort)0x0000);//crc
            addByte((byte)PCK_END);
            updateCRC();
        }

        public void init_CLIENT_P4(ushort cnt, byte[] more_data)
        {
            isNewPacket = true;

            ushort pck_size = (ushort)(9 + more_data.Length); //14 = e total
            const byte pck_id = (byte)0x07;

            initPacket(pck_size + 6);  // 6 for start symbols + length field
            addByte((byte)PCK_START1);
            addByte((byte)PCK_START2);

            addWord((ushort)0x0000);
            addWord((ushort)pck_size); // TODO smart way

            addByte((byte)0x00);
            addWord((ushort)cnt); // TODO smart way


            addByte((byte)pck_id);
            for (int i = 0; i < more_data.Length; i++)
            {
                addByte((byte)more_data[i]);
            }
            addWord((ushort)0x0000);//crc

            addByte((byte)PCK_END1);
            addByte((byte)PCK_END2);
            addByte((byte)PCK_END3);
            updateCRC();
        }
        #endregion

        #region TCP SERVER packetai

        public void init_SERVER_P1(ushort cnt, byte pck_sid, ushort pck_scnt)
        {
            const ushort pck_size = (ushort)0x0009;
            const byte pck_id = (byte)0x02;

            initPacket(pck_size + 4);
            addByte((byte)PCK_START);
            addByte((byte)PCK_START);
            addWord((ushort)pck_size);
            addWord((ushort)cnt);
            addByte((byte)pck_id);
            addByte((byte)pck_sid);
            addWord((ushort)pck_scnt);
            addWord((ushort)0x0000);//crc
            addByte((byte)PCK_END);
            updateCRC();
        }

        public void init_SERVER_P2(ushort cnt, byte pck_sid, ushort pck_scnt, byte data)
        {
            const ushort pck_size = (ushort)0x000A; //9 + 1
            const byte pck_id = (byte)0x04;

            initPacket(pck_size + 4);
            addByte((byte)PCK_START);
            addByte((byte)PCK_START);
            addWord((ushort)pck_size);
            addWord((ushort)cnt);
            addByte((byte)pck_id);
            addByte((byte)pck_sid);
            addWord((ushort)pck_scnt);
            addByte((byte)data);
            addWord((ushort)0x0000);//crc
            addByte((byte)PCK_END);
            updateCRC();
        }

        public void init_SERVER_P3(ushort cnt, byte pck_sid, ushort pck_scnt, byte[] more_data)
        {
            ushort pck_size = (ushort)(9 + more_data.Length);
            const byte pck_id = (byte)0x06;

            initPacket(pck_size + 4);
            addByte((byte)PCK_START);
            addByte((byte)PCK_START);
            addWord((ushort)pck_size);
            addWord((ushort)cnt);
            addByte((byte)pck_id);
            addByte((byte)pck_sid);
            addWord((ushort)pck_scnt);
            for (int i = 0; i < more_data.Length; i++)
            {
                addByte((byte)more_data[i]);
            }
            addWord((ushort)0x0000);//crc
            addByte((byte)PCK_END);
            updateCRC();
        }

        public void init_SERVER_P4(ushort cnt, byte pck_cid, ushort pck_ccnt, byte[] more_data)
        {
            isNewPacket = true;

            ushort pck_size = (ushort)(13 + more_data.Length); //14 = e total
            const byte pck_id = (byte)0x08;

            initPacket(pck_size + 6);  // 6 for start symbols + length field
            addByte((byte)PCK_START1);
            addByte((byte)PCK_START2);

            addWord((ushort)0x0000);
            addWord((ushort)pck_size); // TODO smart way

            addByte((byte)0x00);
            addWord((ushort)cnt); // TODO smart way


            addByte((byte)pck_id);
            addByte((byte)pck_cid);

            addByte((byte)0x00);
            addWord((ushort)pck_ccnt); //Clients count

            for (int i = 0; i < more_data.Length; i++)
            {
                addByte((byte)more_data[i]);
            }
            addWord((ushort)0x0000);//crc

            addByte((byte)PCK_END1);
            addByte((byte)PCK_END2);
            addByte((byte)PCK_END3);
            updateCRC();


            //ushort pck_size = (ushort)(8 + more_data.Length); //13 = d total
            //const byte pck_id = (byte)0x07;

            //initPacket(pck_size + 6);  // 6 for start symbols + length field
            //addByte((byte)PCK_START1);
            //addByte((byte)PCK_START2);

            //addWord((ushort)0x0000);
            //addWord((ushort)pck_size); // TODO smart way

            //addWord((ushort)cnt);
            //addByte((byte)pck_id);
            //for (int i = 0; i < more_data.Length; i++)
            //{
            //    addByte((byte)more_data[i]);
            //}
            //addWord((ushort)0x0000);//crc

            //addByte((byte)PCK_END1);
            //addByte((byte)PCK_END2);
            //addByte((byte)PCK_END3);
            //updateCRC();
        }

        #endregion

        #endregion

        #region PACKET FIELD ACCESSORS

        #region PCK_SIZE

        public void setPCK_SIZE(ushort pck_size)
        {
            addWordAt(pck_size, 2);
            updateCRC();
        }

        public ushort getPCK_SIZE()
        {
            ushort data = bytes2word(this.packet[2], this.packet[3]);
            return data;
        }

        public string getPCK_SIZE_HexString()
        {
            ushort data = bytes2word(this.packet[2], this.packet[3]);
            return word2hexstr(data);
        }

        #endregion

        #region PCK_CNT

        public void setPCK_CNT(ushort pck_cnt)
        {
            addWordAt(pck_cnt, 4);
            updateCRC();
        }

        public ushort getPCK_CNT()
        {
            ushort data = bytes2word(this.packet[isNewPacket? 7:4], this.packet[isNewPacket? 8:5]);
            return data;
        }

        public string getPCK_CNT_HexString()
        {
            ushort data = bytes2word(this.packet[4], this.packet[5]);
            return word2hexstr(data);
        }

        #endregion

        #region PCK_ID

        public byte getPCK_ID()
        {
            byte data = 0x00;
            if (isNewPacket)
                data = this.packet[9];
            else
                data = this.packet[6];
            return data;
        }

        public byte getPCK_ID2()
        {
            byte data = this.packet[9];
            return data;
        }

        public string getPCK_ID_HexString()
        {
            byte data = this.packet[6];
            return byte2hexstr(data);

        }

        #endregion

        #region PCK_CRC

        public ushort getPCK_CRC()
        {
            int crc_index = this.packet.Length - 3;
            ushort data = bytes2word(this.packet[crc_index], this.packet[crc_index+1]);
            return data;
        }

        public string getPCK_CRC_HexString()
        {
            int crc_index = this.packet.Length - 3;
            ushort data = bytes2word(this.packet[crc_index], this.packet[crc_index + 1]);
            return word2hexstr(data);
        }

        #endregion

        /*unique to SERVER packets*/

        #region PCK_SID

        public void setPCK_SID(byte pck_sid)
        {
            addByteAt(pck_sid, 7);
            updateCRC();
        }
        
        public byte getPCK_SID()
        {
            byte data = this.packet[7];
            return data;
        }

        public string getPCK_SID_HexString()
        {
            byte data = this.packet[7];
            return byte2hexstr(data);

        }

        #endregion

        #region PCK_SCNT

        public void setPCK_SCNT(ushort pck_scnt)
        {
            addWordAt(pck_scnt, 8);
            updateCRC();
        }

        public ushort getPCK_SCNT()
        {
            ushort data = bytes2word(this.packet[8], this.packet[9]);
            return data;
        }

        public string getPCK_SCNT_HexString()
        {
            ushort data = bytes2word(this.packet[8], this.packet[9]);
            return word2hexstr(data);
        }

        #endregion


        #endregion

        #region PACKET TOOLS

        protected void initPacket(int size)
        {
            this.packet = new byte[size];
        }

        protected void addByte(byte data)
        {
            this.packet[idx++] = data;
        }

        protected void addWord(ushort data)
        {
            byte hbyte = (byte)0x00;
            byte lbyte = (byte)0x00;
            word2bytes(data, ref hbyte, ref lbyte);
            this.packet[idx++] = hbyte;
            this.packet[idx++] = lbyte;
        }

        protected void addByteAt(byte data, int index)
        {
            this.packet[index] = data;
        }

        protected void addWordAt(ushort data, int index)
        {
            byte hbyte = (byte)0x00;
            byte lbyte = (byte)0x00;
            word2bytes(data, ref hbyte, ref lbyte);
            this.packet[index] = hbyte;
            this.packet[index + 1] = lbyte;
        }

        private ushort calcCRC()
        {
            ushort crc = (ushort)0xffff;
            ushort index;
            byte b;

            for (index = 4; index < this.packet.Length - (isNewPacket ? 5:3); index++)
            {
                crc ^= ((ushort)((this.packet[index] << 8) & 0x0000ffff));
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

        protected void updateCRC()
        {
            ushort crc = calcCRC();
            addWordAt((ushort)crc, this.packet.Length - (isNewPacket?5:3));
        }

        protected void updateCRCNew()
        {
            ushort crc = calcCRC();
            addWordAt((ushort)crc, this.packet.Length - 5);
        }

        public string toHexString()
        {
            return BitConverter.ToString(this.packet);
        }

        public void setPacket(byte[] p)
        {
            this.packet = p;
        }

        public byte[] getRawPacket()
        {
            return this.packet;
        }

        #endregion

        #region BYTE TOOLS

        private ushort bytes2word(byte hb, byte lb)
        {
            ushort data = (ushort)(hb << 8 | lb);
            return data;
        }

        private void word2bytes(ushort data, ref byte hb, ref byte lb)
        {
            hb = (byte)((data >> 8) & 0x000000FF);
            lb = (byte)(data & (ushort)0x00FF);
        }

        private string byte2hexstr(byte data)
        {
            StringBuilder sb = new StringBuilder(4);
            sb.Append("0x");
            sb.AppendFormat("{0:x2}", data);
            return sb.ToString();
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

        #endregion

    }
}
