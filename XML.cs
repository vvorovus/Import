using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Globalization;
namespace Import
{
    
    class XML
    {
        
        public static DateTime StringToDateTime(string text,int timezone)
        {
            //"20080901T00:30:00000"
            int yyyy = int.Parse(text.Substring(0, 4)); // год
            int mm = int.Parse(text.Substring(4, 2)); // месяц
            int dd = int.Parse(text.Substring(6, 2)); // день
            int hh = 0; // час
            int nn = 0; // минута
            int ss = 0; // секунда
            if (text.Length > 8)
            {
                hh = int.Parse(text.Substring(9, 2)); // час
                nn = int.Parse(text.Substring(12, 2)); // минута
                ss = int.Parse(text.Substring(15, 4)); // секунды
            }
            
            DateTime d = new DateTime(yyyy, mm, dd, hh, nn, ss);
            d = d.AddHours(-timezone);

            return d;
        }
        /*
         * собственный код получения каналов нужен, потому что данные получаются по полю id а не abbr
         */
        public static int getChannel(int channelCode, int invert)
        {
            int channelID = 0;
            if (invert == 1)
            {
                channelID = (int)App.ExecuteInt32(
                    "declare @id int\n" +
                    "set @id = 0\n" +
                    "select @id = invert_id from channel \n" +
                    "where id = {0}\n" +
                    "select @id \n",
                    channelCode);
            }
            else
            {
                channelID = (int)App.ExecuteInt32(
                    "declare @id int\n" +
                    "set @id = 0\n" +
                    "select @id = id from channel \n" +
                    "where id = {0}\n" +
                    "select @id \n",
                    channelCode);
            }
            if (channelID == 0) throw new Exception("Канал не найден \"" + channelCode + "\"");
            return channelID;
        }

        public static void Parse(System.Xml.XmlDocument doc)
        {
            XmlNodeList values = doc.SelectNodes("//ROW");
            int GlobalCount = values.Count;
            int GlobalIndex = 0;
            int GlobalPercent = 0;

            int sourcetypeID = (int)App.ExecuteInt32(
                                "select id from sourcetype \n" +
                                "where code = '{0}'\n",
                                "XML");

            XmlNodeList deviceList = doc.SelectNodes("//DEVICE");
            
            foreach (XmlElement device in deviceList)
            {
                string name = device.GetAttribute("NAME_DEVICE");
                string code = device.GetAttribute("UUID");
                int coef_i = C.Int(device.GetAttribute("COEF_I"));
                int coef_u = C.Int(device.GetAttribute("COEF_U"));
                int imp_unit = C.Int(device.GetAttribute("IMP_UNIT"));
                int coef = coef_i * coef_u;

                int pointcodeID = (int)App.ExecuteInt32(
                    "select id from pointcode \n" +
                    "where name = '{0}' and code = '{1}' and sourcetype = {2} \n",
                    name, code, sourcetypeID);

                if (pointcodeID == 0)
                    throw new Exception("точка [" + name + "], code=[" + code + "] не найдена");

                int pointID = (int)App.ExecuteInt32(
                "select point from pointcode \n" +
                "where id = {0}\n",
                pointcodeID);

                int timezone = App.ExecuteInt32("select timezone from pointcode where id = {0}", pointcodeID);

                int Invert = (int)App.ExecuteInt32("select invert from pointcode where id = {0}", pointcodeID);//getInvert(pointID, sourcetype);

                int koeftrans = (int)App.ExecuteInt32("select koeftrans from pointcode where id = {0}", pointcodeID);

                if (koeftrans != coef)
                {
                    App.ExecuteNonQuery("UPDATE [pointcode] SET [koeftrans] = {0} WHERE id = {1}", coef, pointcodeID);
                    
                }
                   // throw new Exception("Не совпадают коэффициэнты трансформации " + koeftrans + " " + coef);
                                
                
                XmlNodeList channelList = device.SelectNodes("./CHANNEL");

                foreach (XmlElement channel in channelList)
                {

                    int channelID = getChannel(C.Int(channel.GetAttribute("IDTYPE_CHANNEL")), Invert);

                    DateTime DateStart = DateTime.Parse(channel.GetAttribute("DATE_START"));
                    DateTime DateEnd = DateTime.Parse(channel.GetAttribute("DATE_END"));

                    DateEnd.AddHours(-timezone);
                    DateStart.AddHours(-timezone);
                                         
                    int datasourceID = App.getDataSource(pointID, channelID, sourcetypeID);
                    App.BeginUpdate(datasourceID, DateStart, DateEnd);
                    
                    XmlNodeList datapacketList = channel.SelectNodes("./DATAPACKET");
                    
                    foreach (XmlElement datapacket in datapacketList)
                    {
                        XmlNodeList rowdataList = datapacket.SelectNodes("./ROWDATA");
                        
                        foreach (XmlElement rowdata in rowdataList)
                        {
                            
                            XmlNodeList rowList = rowdata.SelectNodes("./ROW");
                            
                            foreach (XmlElement row in rowList)
                            {

                                DateTime end = StringToDateTime(row.GetAttribute("TIME"),timezone);
                                int period = C.Int(row.GetAttribute("PERIOD"));
                                
                                DateTime begin = end.AddSeconds(-period);

                                if (begin > end)
                                    end = end.AddDays(1);

                                NumberFormatInfo provider = new NumberFormatInfo();
                                provider.NumberDecimalSeparator = ".";

                                int summer = int.Parse(row.GetAttribute("WINTER_SUMMER"));
                                if (summer == 0)
                                {
                                    end = end.AddHours(-1);
                                    begin = end.AddSeconds(-period);
                                    
                                }

                                float power = float.Parse(row.GetAttribute("VALUE_UNIT"), provider);
                                float indication = float.Parse(row.GetAttribute("VALUE_INDICATION"), provider);
                                //float value;
                                //power = Math.Round(power);

                                App.InsertPeriod(datasourceID, begin, end, power,indication);

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
                        App.EndUpdate();                         

                    }

                }

            }


        }
    }
}
