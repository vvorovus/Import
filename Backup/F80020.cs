using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Import
{
    public class F80020
    {
        public static DateTime StringToDateTime(string text)
        {
            int yyyy = int.Parse(text.Substring(0, 4)); // год
            int mm = int.Parse(text.Substring(4, 2)); // месяц
            int dd = int.Parse(text.Substring(6, 2)); // день
            int hh = 0; // час
            int nn = 0; // минута
            int ss = 0; // секунда
            if (text.Length == 12)
            {
                hh = int.Parse(text.Substring(8, 2)); // час
                nn = int.Parse(text.Substring(10, 2)); // минута
            }
            DateTime d = new DateTime(yyyy, mm, dd, hh, nn, ss);
            d = d.AddHours(-7);

            return d;
        }

        public static int getChannel(string channelCode, int invert)
        {
            int channelID = 0;
            if (invert == 1)
            {
                channelID = (int)App.ExecuteInt32(
                    "declare @id int\n" +
                    "set @id = 0\n" +
                    "select @id = invert_id from channel \n" +
                    "where code = '{0}'\n" +
                    "select @id \n",
                    channelCode);
            }
            else
            {
                channelID = (int)App.ExecuteInt32(
                    "declare @id int\n" +
                    "set @id = 0\n" +
                    "select @id = id from channel \n" +
                    "where code = '{0}'\n" +
                    "select @id \n",
                    channelCode);
            }
            if (channelID == 0) throw new Exception("Канал не найден \"" + channelCode + "\"");
            return channelID;
        }

        public static void Parse(System.Xml.XmlDocument doc)
        {
            XmlNodeList lst = doc.SelectNodes("//period");
            int GlobalCount = lst.Count;
            int GlobalIndex = 0;
            int GlobalPercent = 0;

            int sourcetypeID;

            

            XmlNodeList messageList = doc.SelectNodes("//message");
            foreach (XmlElement message in messageList)
            {
                XmlElement datetime = (XmlElement)message.SelectSingleNode("./datetime");
                string s = datetime["day"].InnerText;
                DateTime day = StringToDateTime(s);
                /* ХакасЭнерго посылает данные по зимнему времени
                 * 
                s = datetime["daylightsavingtime"].InnerText;
                if (s == "1")
                    day = day.AddHours(-1); // если летнее время
                else if (s == "2")
                {
                    //if (day.Month < 6)
                    //    day.AddHours(-1); // переход с зимнего времени на летнее - сутки практически по летнему времени
                }*/
                XmlElement sender = (XmlElement)message.SelectSingleNode("./sender");
                if (sender["name"].InnerText == "ООО \"Абаканэнергосбыт\"")
                {
                    sourcetypeID = (int)App.ExecuteInt32(
                                "select id from sourcetype \n" +
                                "where code = '{0}'\n",
                                "АСКУЭ");
                }
                else
                {
                    sourcetypeID = (int)App.ExecuteInt32(
                                "select id from sourcetype \n" +
                                "where code = '{0}'\n",
                                "ХЭ");
                }
               

                XmlNodeList areaList = message.SelectNodes("./area");
                foreach (XmlElement area in areaList)
                {
                    XmlNodeList mpList = area.SelectNodes("./measuringpoint");
                    foreach (XmlElement mp in mpList)
                    {
                        int pointcode = App.ExecuteInt32(
                            "select id from pointcode \n" +
                            "where code = '{1}' and name = '{0}' and rtrim('{1}') <> ''",
                            mp.GetAttribute("name"),
                            mp.GetAttribute("code"));

                        int mpID = (int)App.ExecuteInt32(
                            "select point from pointcode \n" +
                            "where id = {0}",
                            pointcode);

                        if (mpID == 0)
                            throw new Exception("точка " + mp.GetAttribute("name") + " [" + mp.GetAttribute("code") + "] не найдена");

                        sourcetypeID = (int)App.ExecuteInt32(
                            "select sourcetype from pointcode \n" +
                            "where id = {0}", pointcode);

                        int invert = App.getInvert(mpID, sourcetypeID);

                        XmlNodeList mcList = mp.SelectNodes("./measuringchannel");
                        foreach (XmlElement mc in mcList)
                        {
                            int mcID = getChannel(mc.GetAttribute("code"), invert);
                            int datasourceID = App.getDataSource(mpID, mcID, sourcetypeID);

                            App.BeginUpdate(datasourceID, day, day.AddHours(24));

                            XmlNodeList periodList = mc.SelectNodes("./period");
                            foreach (XmlElement period in periodList)
                            {
                                XmlElement value = (XmlElement)period.SelectSingleNode("./value");
                                string strBegin = period.GetAttribute("start");
                                string strEnd = period.GetAttribute("end");

                                DateTime begin = day.AddHours(Int32.Parse(strBegin.Substring(0, 2)));
                                begin = begin.AddMinutes(Int32.Parse(strBegin.Substring(2, 2)));

                                DateTime end = day.AddHours(Int32.Parse(strEnd.Substring(0, 2)));
                                end = end.AddMinutes(Int32.Parse(strEnd.Substring(2, 2)));

                                if (begin > end)
                                    end = end.AddDays(1);

                                App.InsertPeriod(datasourceID, begin, end, Int32.Parse(value.InnerText));

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
            App.EndUpdate();
        }
    }
}