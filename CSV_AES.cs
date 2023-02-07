using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Import
{
    class CSV_AES
    {
        public static int GlobalIndex = 0;
        public static int GlobalPercent = 0;
        public static int GlobalCount = 0;
        public static int IndexRow = 0;

        public static void Parse(String FileName)
        {
            string buf, s1, s2;
            string delimStr = ";";
            char[] delimiter = delimStr.ToCharArray();
            string[] split = null;
            DateTime date, date3, datebuf;
            
            double del;
            datebuf = new DateTime(2000, 01, 01);
            int i;

            s1 = FileName.Remove(0, FileName.Length-12);
            s2 = s1.Substring(0, 8);
           

            date = new DateTime();
            using (StreamReader sr = File.OpenText(FileName))
            {
                GlobalCount = File.ReadAllLines(FileName).Length;

                while ((buf = sr.ReadLine()) != null)
                {

                    if (buf.Length > 13)
                    {

                        split = buf.Split(delimiter);

                        App.InsertLKYUR(2, C.Int(split[0]), C.Int(split[1]), C.Str(split[2]), C.Str(split[3]), C.Str(split[4]), C.Str(split[5]), C.Float(split[6], 0));


                    }

                    GlobalIndex++;
                    int posY = Console.CursorTop;
                    decimal percent = Math.Round((decimal)(GlobalIndex * 100 / GlobalCount));
                    if (GlobalPercent != Decimal.ToInt32(percent))
                    {
                        GlobalPercent = Decimal.ToInt32(percent);
                        Console.Write(GlobalPercent.ToString());
                        Console.CursorTop = posY;
                        Console.CursorLeft = 0;
                    }
                }
            }            

        }
    }
}
