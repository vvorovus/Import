using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Globalization;

namespace Import
{
    public class EPQS
    {
        public static DateTime StringToDateTime(string text)
        {
            int yyyy = int.Parse(text.Substring(0, 4)); // год
            int mm = int.Parse(text.Substring(4, 2)); // мес€ц
            int dd = int.Parse(text.Substring(6, 2)); // день
            int hh = 0; // час
            int nn = 0; // минута
            int ss = 0; // секунда
            if (text.Length == 12)
            {
                hh = int.Parse(text.Substring(8, 2)); // час
                nn = int.Parse(text.Substring(10, 2)); // минута
            }
            int GTM = int.Parse(text.Substring(17,2));
            DateTime d = new DateTime(yyyy, mm, dd, hh, nn, ss);
            d = d.AddHours(-7);

            return d;
        }
               
        public static void Parse(System.Xml.XmlDocument doc)
        {
            XmlNodeList values = doc.SelectNodes("//value");
            int GlobalCount = values.Count;
            int GlobalIndex = 0;
            int GlobalPercent = 0;
         
            
            int sourcetypeID = (int)App.ExecuteInt32(
                                "select id from sourcetype \n" +
                                "where code = '{0}'\n",                                
                                "EPQS");


            XmlNodeList messageList = doc.SelectNodes("//message");

            
            foreach(XmlElement message in messageList)
            {
                

                XmlNodeList pointList = message.SelectNodes("./point");

                foreach (XmlElement point in pointList)
                {

                    int pointcodeID = (int)App.ExecuteInt32(
                    "select id from pointcode \n" +
                    "where factory = '{0}' and sourcetype = {1}\n",
                    point.GetAttribute("number"),sourcetypeID);

                    if (pointcodeID == 0)
                        throw new Exception("счетчик EPQS [" + point.GetAttribute("number") + "] не найден");

                    int pointID = (int)App.ExecuteInt32(
                    "select point from pointcode \n" +
                    "where id = {0}\n",
                    pointcodeID);

                    
                    //catch(Exception e)
                    {
 //                       App.LogMessage(e);
                    }


                    int Invert = (int)App.ExecuteInt32("select invert from pointcode where id = {0}", pointcodeID);//getInvert(pointID, sourcetype);

                    int koeftrans = (int)App.ExecuteInt32("select koeftrans from pointcode where id = {0}", pointcodeID);
                    
                    XmlNodeList periodList = point.SelectNodes("./period");

                    foreach (XmlElement period in periodList)
                    {
                        String dateBegin = period.GetAttribute("begin");
                        DateTime periodDate = StringToDateTime(period.GetAttribute("begin"));

                        XmlNodeList channelList = period.SelectNodes("./channel");

                        foreach (XmlElement channel in channelList)
                        {
                            int channelID = App.getChannel(channel.GetAttribute("code"), Invert);

                            XmlNodeList valueList = channel.SelectNodes("./value");

                            int datasourceID = App.getDataSource(pointID, channelID, sourcetypeID);
                            App.BeginUpdate(datasourceID, periodDate, periodDate.AddHours(24));

                            foreach (XmlElement value in valueList)
                            {
                                string strBegin = value.GetAttribute("begin");
                                string strEnd = value.GetAttribute("end");

                                DateTime begin = periodDate.AddHours(Int32.Parse(strBegin.Substring(0, 2)));
                                begin = begin.AddMinutes(Int32.Parse(strBegin.Substring(2, 2)));
                                
                                DateTime end = periodDate.AddHours(Int32.Parse(strEnd.Substring(0, 2)));
                                end = end.AddMinutes(Int32.Parse(strEnd.Substring(2, 2)));

                                if (begin > end)
                                    end = end.AddDays(1);
                                
                                NumberFormatInfo provider = new NumberFormatInfo();
                                provider.NumberDecimalSeparator = ".";
                                
                                float power = float.Parse(value.InnerText,provider)*koeftrans;
                                //power = Math.Round(power);

                                App.InsertPeriod(datasourceID, begin, end, power);
                                
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
                            } // foreach (XmlElement value in valueList)
                        } // foreach (XmlElement channel in channelList)
                        App.EndUpdate();
                    } // foreach (XmlElement period in periodList)
                }
            }
        }
    }
}
