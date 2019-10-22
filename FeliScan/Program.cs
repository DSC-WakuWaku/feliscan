using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FelicaLib;
using NfcStarterKitWrap;

namespace FeliScan
{
    class Program
    {
        static void Main(string[] args)
        {
            #region FelicaLib
            try
            {
                Console.WriteLine("FelicaLib");
                using (FelicaLib.Felica f = new FelicaLib.Felica())
                {
                    PrintSuicaNo(f);
                }
                Console.WriteLine("\n\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            #endregion

            #region NfcStarterKitWrap
            try
            {
                Console.WriteLine("NfcStarterKitWrap");
                nfc mFNS = new nfc();
                FelicaLite mLite = null;
                byte[] mWriteValue = new byte[nfc.BLOCK_SIZE];
                if (!mFNS.init())
                {
                    Console.WriteLine("SDK for NFC Starter Kit fail");
                    Environment.Exit(0);
                    return;
                }

                mLite = new FelicaLite(mFNS);

                bool ret;
                ret = mFNS.pollingF();
                if (!ret)
                {
                    Console.WriteLine("Polling fail");
                    return;
                }
                Console.WriteLine("IDm=" + BitConverter.ToString(mFNS.NfcId));

                // 読み込み
                ReadSuica(mFNS);

                mFNS.unpoll();
                Console.WriteLine("\n\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            #endregion

            Console.Read();
        }

        //[FelicaLib]Suica読み込み
        private static void PrintSuicaNo(FelicaLib.Felica f)
        {
            // システムコード: 0003 (Suicaなどの領域)
            f.Polling((int)SystemCode.Suica);
            //f.Polling((int)SystemCode.Any);

            Console.WriteLine("IDm=" + BitConverter.ToString(f.IDm()));
            Console.WriteLine("PMm=" + BitConverter.ToString(f.PMm()));

            // Suica 各サービスコード
            for (int i = 0; ; i++)
            {
                // サービスコード　乗降履歴情報
                byte[] data = f.ReadWithoutEncryption(0x090f, i);
                if (data == null) break;
                Console.WriteLine("Suica 乗降履歴情報 [" + i + "]  " + BitConverter.ToString(data));
            }
            for (int i = 0; ; i++)
            {
                Console.WriteLine("Suica カード種別およびカード残額情報 " + i);
                // サービスコード　カード種別およびカード残額情報
                byte[] data = f.ReadWithoutEncryption(0x008B, i);
                if (data == null) break;
            }
            for (int i = 0; ; i++)
            {
                Console.WriteLine("Suica 改札入出場履歴情報 " + i);
                // サービスコード　改札入出場履歴情報
                byte[] data = f.ReadWithoutEncryption(0x108F, i);
                if (data == null) break;
            }
            for (int i = 0; ; i++)
            {
                Console.WriteLine("Suica SF入場情報 " + i);
                // サービスコード　SF入場情報
                byte[] data = f.ReadWithoutEncryption(0x10CB, i);
                if (data == null) break;
            }
            for (int i = 0; ; i++)
            {
                Console.WriteLine("Suica 料金券情報 " + i);
                // サービスコード　料金券情報
                byte[] data = f.ReadWithoutEncryption(0x184B, i);
                if (data == null) break;
            }

            for (int i = 0; ; i++)
            {
                byte[] data = f.ReadWithoutEncryption(0x008B, i);
                if (data == null) break;
            }

        }

        //[NfcStarterKitWrap]サービスコード読み込み
        private static bool Read(nfc mFNS, ref byte[] buf, byte block_num, byte[] svc)
        {
            UInt16[] blocks = new UInt16[1];
            blocks[0] = block_num;
            bool b = mFNS.NfcF_Read(ref buf, blocks, 1, svc);
            return b;
        }

        //[NfcStarterKitWrap]Suica読み込み
        private static void ReadSuica(nfc mFNS)
        {
            int num = 1;    //最大20ブロック[0~19]
            string _str = string.Empty;
            for (int i = 0; i < num; i++)
            {
                byte[] buf = new byte[nfc.BLOCK_SIZE];
                var b = Read(mFNS, ref buf, (byte)i, new byte[2] { 0x0F, 0x09 });
                if (!b) break;
                Console.WriteLine("Suica 乗降履歴情報 [" + i + "]  " + BitConverter.ToString(buf));
                _str += "[" + i + "]  <090F>" + BitConverter.ToString(buf) + "\n";

                //利用種別


                //年月日
                byte[] v2 = new byte[] { buf[5], buf[4] };
                var val2 = BitConverter.ToInt16(v2, 0);

                //情報
                byte[] v3_1 = new byte[] { buf[6], buf[7] };
                var val3_1 = BitConverter.ToInt16(v3_1, 0);
                byte[] v3_2 = new byte[] { buf[8], buf[9] };
                var val3_2 = BitConverter.ToInt16(v3_2, 0);

                //残高
                byte[] v4 = new byte[] { buf[10], buf[11] };
                var val4 = BitConverter.ToInt16(v4, 0);

                //履歴連番
                byte[] v1 = new byte[] { buf[13], buf[14] };
                var val1 = BitConverter.ToInt16(v4, 0);

                Console.WriteLine("機器種別  ：" + buf[0].ToString("x2"));
                Console.WriteLine("利用種別  ：" + (buf[1] >> 7) + "  " + (buf[1] & 0x7F));
                Console.WriteLine("決済種別  ：" + buf[2].ToString("x2"));
                Console.WriteLine("入出場種別：" + buf[3].ToString("x2"));
                Console.WriteLine("年月日 　 ：20" + (val2 >> 9) + "年" + ((val2 & 0x01FF) >> 5) + "月" + (val2 & 0x001F) + "日");
                Console.WriteLine("情報  　　：" + val3_1 + "  " + val3_2);
                Console.WriteLine("残額  　　：" + val4);
                Console.WriteLine("？  　　　：" + buf[12].ToString("x2"));
                Console.WriteLine("履歴連番  ：" + val1);
                Console.WriteLine("地域コード：" + buf[15].ToString("x2"));
                _str += "機器種別  ：" + buf[0].ToString("x2") + "\n";
                _str += "利用種別  ：" + (buf[1] >> 7) + "  " + (buf[1] & 0x7F) + "\n";
                _str += "決済種別  ：" + buf[2].ToString("x2") + "\n";
                _str += "入出場種別：" + buf[3].ToString("x2") + "\n";
                _str += "年月日 　 ：20" + (val2 >> 9) + "年" + ((val2 & 0x01FF) >> 5) + "月" + (val2 & 0x001F) + "日\n";
                _str += "情報  　　：" + val3_1 + "  " + val3_2 + "\n";
                _str += "残額  　　：" + val4 + "\n";
                _str += "？  　　　：" + buf[12].ToString("x2") + "\n";
                _str += "履歴連番  ：" + val1 + "\n";
                _str += "地域コード：" + buf[15].ToString("x2") + "\n";
            }
            Console.WriteLine(_str);
        }
    }
}

/*
namespace FeliScan
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}
*/