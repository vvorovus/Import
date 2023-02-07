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
            return d;
        }

        /*
         * собственный код получения каналов нужен, потому что данные получаются по полю code а не abbr
         */
        public static int getChannel(string channelCode, int invert)
        {
            int codeLenth = channelCode.Length;
            if (C.Int(channelCode) > 10 )
                channelCode = "0" + channelCode[1];
            
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
            int parentID = 0;
            int IsNegative = 0;
            
            int sourcetypeID = (int)App.ExecuteInt32(
                                "select id from sourcetype \n" +
                                "where code = '{0}'\n",
                                "ХЭ");

            XmlNodeList messageList = doc.SelectNodes("//message");
            foreach (XmlElement message in messageList)
            {

                int num = C.Int(message.GetAttribute("number"));
                int current_num = App.ExecuteInt32("select [value] from Count_ID where id = 2");
                if (num > current_num)
                    App.ExecuteNonQuery("UPDATE Count_ID SET [value]={0} WHERE id = 2", num.ToString());

                XmlElement datetime = (XmlElement)message.SelectSingleNode("./datetime");
                string s = datetime["day"].InnerText;
                DateTime baseDay = StringToDateTime(s);
                
                int isSummer = 0;
                s = datetime["daylightsavingtime"].InnerText;
                if (s == "1")
                    isSummer = 1; // если летнее время
                else if (s == "2")
                {
                    // время перевода стрелок
                    if (baseDay.Month == 3) // переход осуществляется в марте месяце
                        isSummer = 1; // переход с зимнего времени на летнее - сутки практически по летнему времени
                }
                // новые форматы 80020 имеют аттрибут summer для тех периодов, что рассчитываются по летнему времени
                bool hasSummerAttribute = message.SelectSingleNode("//@summer") != null; // это признак что данный файл использует этот аттрибут

                if (s == "0")
                {
                    isSummer = 0;
                    hasSummerAttribute = false;

                }

                /* 
                 * ХакасЭнерго посылает данные по зимнему времени и не использует переход на летнее время
                 * блин, а СаЭС использует переход на зимнее время
                 */
                
                //Определиние, кто является отправителем файла ФСК или наша АИИС КУЭ
                
                bool isFSK = false;
                bool isMAREM = false;
                bool isCustomer = false;
                bool isHumans = false;
                bool isKhakas = false;
                bool isMes = false;
                int CustomerID;
                
                string parentName;

                XmlNode sender = message.SelectSingleNode("./sender");
                string inn = sender["inn"].InnerText;
                //if (inn == "2600002610")
                  //  isMes = true;
                if (inn == "4716016979" || inn == "2600002610")
                    isFSK = true;
                else if (inn == "7704181109")
                    isMAREM = true;
                else if (inn == "0000000000" || inn == "1901067718" || inn == "7705454461" || inn == "7812014560" || inn == "7713076301" || inn == "9876543210" 
                    || inn == "000000000000000" || inn == "7740000076" || inn == "7717127211")
                    isCustomer = true;
                else if (inn == "8888888888")
                    isHumans = true;
                else if (inn == "0123456789")
                    isKhakas = true;



                XmlNodeList areaList = message.SelectNodes("./area");
                foreach (XmlElement area in areaList)
                {

                    XmlElement inn_gtp = (XmlElement)area.SelectSingleNode("./inn");
                    XmlElement name_gtp = (XmlElement)area.SelectSingleNode("./name");
                    if (inn_gtp.InnerText == "1900000907")
                        App.ExecuteNonQuery("UPDATE checksend SET [check_80020]={0} WHERE [date]='{1}' and [point]=1", num.ToString(), baseDay.ToString("yyyyMMdd"));
                    if (inn_gtp.InnerText == "1900003700")
                        App.ExecuteNonQuery("UPDATE checksend SET [check_80020]={0} WHERE [date]='{1}' and [point]=2", num.ToString(), baseDay.ToString("yyyyMMdd"));
                    
                    if (isCustomer)
                    {
                        if (inn == "1901067718")
                            CustomerID = 50;
                        else if (inn == "7705454461")
                            CustomerID = 3144;
                        else if (inn == "7812014560")
                        {
                            CustomerID = 1855;
                            IsNegative = 1;
                        }
                        else if (inn == "7713076301")
                            CustomerID = 2028;
                        else if (inn == "9876543210")
                            CustomerID = 3444;
                        else if (inn == "7740000076")
                            CustomerID = 722;
                        else if (inn == "7717127211")
                            CustomerID = 327;

                        else
                            CustomerID = C.Int(inn_gtp.InnerText);


                       
                        parentName = C.Str(name_gtp.InnerText);
                        parentID = App.ExecuteInt32("select point from pointCustomer where CustomerID = {0}", CustomerID);
                        if (parentID == 0)
                        { 
                            parentID = App.InsertParent(CustomerID,parentName); 
                        }
                    }
                    if (isHumans)
                    {
                        parentID = 8;
                    }
                    
                    
                    XmlNodeList mpList = area.SelectNodes("./measuringpoint");
                    foreach (XmlElement mp in mpList)
                    {
                        
                        /*int pointcodeID = App.ExecuteInt32(
                            "select id from pointcode \n" +
                            "where code = '{0}' and rtrim('{0}') <> ''",
                            mp.GetAttribute("code"));*/

                        int pointcodeID;
                        if (inn_gtp.InnerText == "2600002610")
                        {
                            pointcodeID = App.ExecuteInt32(
                            "select id from pointcode \n" +
                            "where code = '{0}' and rtrim('{0}') <> '' and sourcetype = 15",
                            mp.GetAttribute("code"));

                        }
                        else if (isFSK)
                        {
                            pointcodeID = App.ExecuteInt32(
                            "select id from pointcode \n" +
                            "where code = '{0}' and rtrim('{0}') <> ''",
                            inn);
                        }
                        else if (isMAREM)
                        {
                            pointcodeID = App.ExecuteInt32(
                            "select id from pointcode \n" +
                            "where code = '{0}' and rtrim('{0}') <> '' and sourcetype = 16",
                            mp.GetAttribute("code"));
                        }
                        else if (isKhakas)
                        {
                            pointcodeID = App.ExecuteInt32(
                           "select id from pointcode \n" +
                           "where code = '{0}' and rtrim('{0}') <> '' and sourcetype = 1",
                           mp.GetAttribute("code"));
                                
                        }
                        else
                        {

                            pointcodeID = App.ExecuteInt32(
                                "select id from pointcode \n" +
                                "where code = '{0}' and rtrim('{0}') <> ''",
                                mp.GetAttribute("code"));
                        }

                        int pointID = (int)App.ExecuteInt32(
                            "select point from pointcode \n" +
                            "where id = {0}",
                            pointcodeID);

                        if (pointID == 0)
                        {
                            Console.Write("точка " + mp.GetAttribute("name") + " [" + mp.GetAttribute("code") + "] не найдена");
                            if (isCustomer && mp.GetAttribute("name") != "ПНС 3 КТПН")
                            {
                                pointID = App.InsertPoint(mp.GetAttribute("code"), mp.GetAttribute("name"), parentID);
                                    pointcodeID = App.ExecuteInt32(
                                    "select id from pointcode \n" +
                                    "where code = '{0}' and rtrim('{0}') <> ''",
                                    mp.GetAttribute("code"));
                            }
                            else if (isHumans)
                            {
                                pointID = App.InsertPointH(mp.GetAttribute("code"), mp.GetAttribute("name"), parentID);
                                pointcodeID = App.ExecuteInt32(
                                "select id from pointcode \n" +
                                "where code = '{0}' and rtrim('{0}') <> ''",
                                mp.GetAttribute("code"));
                            }
                            else
                                break;
                            //throw new Exception("точка " + mp.GetAttribute("name") + " [" + mp.GetAttribute("code") + "] не найдена");
                        }

                        int timezone = App.ExecuteInt32("select timezone from pointcode where id = {0}", pointcodeID);
                        //timezone = 3;
                                                
                        int daylightsavings = App.ExecuteInt32("select daylightsavings from pointcode where id = {0}", pointcodeID);
                        DateTime day = baseDay.AddHours(-timezone);

                        sourcetypeID = (int)App.ExecuteInt32(
                            "select sourcetype from pointcode \n" +
                            "where id = {0}", pointcodeID);
                        //sourcetypeID = 12;

                        int invert = App.getInvert(pointID, sourcetypeID);
                        
                        XmlNodeList mcList = mp.SelectNodes("./measuringchannel");
                        foreach (XmlElement mc in mcList)
                        {
                            
                            int channelID = getChannel(mc.GetAttribute("code"), invert);

                            int datasourceID = App.getDataSource(pointID, channelID, sourcetypeID);

                            App.BeginUpdate(datasourceID, day, day.AddHours(24));

                            XmlNodeList periodList = mc.SelectNodes("./period");
                            foreach (XmlElement period in periodList)
                            {
                                XmlElement xmlvalue = (XmlElement)period.SelectSingleNode("./value");
                                string strBegin = period.GetAttribute("start");
                                string strEnd = period.GetAttribute("end");
                                float value = 0;

                                value = C.Float(xmlvalue.InnerText, 0);
                                if (value < 0)
                                    if (IsNegative == 1)
                                        value = 1;
                                if(value > 3000)
                                    if (IsNegative == 1)
                                        value /= 4000;

                                DateTime begin = day.AddHours(Int32.Parse(strBegin.Substring(0, 2)));
                                begin = begin.AddMinutes(Int32.Parse(strBegin.Substring(2, 2)));

                                DateTime end = day.AddHours(Int32.Parse(strEnd.Substring(0, 2)));
                                end = end.AddMinutes(Int32.Parse(strEnd.Substring(2, 2)));

                                if (begin > end)
                                    end = end.AddDays(1);

                               

                                if (daylightsavings == 1 && hasSummerAttribute)
                                {
                                    int summer = C.Int(period.GetAttribute("summer"), 0);
                                    if (summer == 1)
                                    {
                                        begin = begin.AddHours(-1);
                                        end = end.AddHours(-1);
                                    }
                                }
                                else
                                {
                                    // обработка старых форматов формата 80020
                                    if (daylightsavings == 1 && isSummer == 1)
                                    {
                                        begin = begin.AddHours(-1);
                                        end = end.AddHours(-1);
                                    }

                                }

                                /*if (inn == "0123456789")
                                {
                                    begin = begin.AddHours(5);
                                    end = end.AddHours(5);
                                }*/
                                
                                App.InsertPeriod(datasourceID, begin, end, value);

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