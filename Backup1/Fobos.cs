using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Import
{
    class Fobos
    {
        public static int GlobalIndex = 0;
        public static int GlobalPercent = 0;
        public static int GlobalCount = 11;
        public static int IndexRow = 0;

        public static void Parse(String FileName)
        {
            string buf;
            string delimStr = "=,";
        	char [] delimiter = delimStr.ToCharArray();
            string [] split = null;
            DateTime date,date3,datebuf;
            int prewhour=-1;
            double del;
            datebuf = new DateTime(2000, 01, 01);
            
            using (StreamReader sr = File.OpenText(FileName))
            {
                                
                while ((buf = sr.ReadLine()) != null)
                {
                    

                    if (buf.Length > 20)
                    {
                        if (datebuf.Year == 2000)
                        {
                            split = buf.Split(delimiter);
                            date = new DateTime(C.Int(split[5]), C.Int(split[3]), C.Int(split[1]), C.Int(split[7]),0,0);
                            date3 = date.AddHours(-4);
                            App.InsertFobos(date,
                                C.Int(split[7]), C.Int(split[9]), C.Int(split[11]),
                                C.Int(split[13]), C.Int(split[15]), C.Int(split[17]),
                                C.Int(split[19]), C.Int(split[21]), C.Int(split[23]),
                                C.Int(split[25]), C.Int(split[27]), C.Int(split[29]),
                                C.Int(split[31]), C.Int(split[33]),date3,date3.Hour);
                            datebuf = date;                            
                                                        
                        }
                        else
                        {
                            
                            split = buf.Split(delimiter);
                            date = new DateTime(C.Int(split[5]), C.Int(split[3]), C.Int(split[1]), C.Int(split[7]), 0, 0);
                            del = date.Subtract(datebuf).Hours;
                            datebuf = datebuf.AddHours(Math.Floor(del/2));
                            date3 = datebuf.AddHours(- 4);
                            for (int i = 0; i < del; i++)
                            {
                                App.InsertFobos(datebuf,
                                    datebuf.Hour, C.Int(split[9]), C.Int(split[11]),
                                    C.Int(split[13]), C.Int(split[15]), C.Int(split[17]),
                                    C.Int(split[19]), C.Int(split[21]), C.Int(split[23]),
                                    C.Int(split[25]), C.Int(split[27]), C.Int(split[29]),
                                    C.Int(split[31]), C.Int(split[33]),date3,date3.Hour);
                                datebuf = datebuf.AddHours(1);
                                date3 = datebuf.AddHours(-4);
                             }
                            datebuf = date;
                            
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
}
