using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Globalization;

namespace Import
{
    public class C_01
    {
        public static void Parse(System.Xml.XmlDocument doc)
        {
            XmlNodeList values = doc.SelectNodes("//channel");
            int GlobalCount = values.Count;
            int GlobalIndex = 0;
            int GlobalPercent = 0;

            XmlNodeList messageList = doc.SelectNodes("//message");


            foreach (XmlElement message in messageList)
            {
                int sourcetypeID = (int)App.ExecuteInt32(
                                    "select id from sourcetype \n" +
                                    "where code = '{0}'\n",
                                    message.GetAttribute("class"));

                XmlNodeList pointList = message.SelectNodes("./point");

                foreach (XmlElement point in pointList)
                {

                    /*int pointID = (int)App.ExecuteInt32(
                        "declare @id int\n" +
                        "set @id = 0\n" +
                        "select @id = id from point \n" +
                        "where factory = '{0}'\n" +
                        "if @id is null begin\n" +
                        "    insert into point (name, code)\n" +
                        "    values('{0}', '{0}')\n" +
                        "    set @id = scope_identity()\n" +
                        "end\n" +
                        "select @id", point.GetAttribute("number"));

                    int Invert = EPQS.getInvert(pointID, sourcetype);

                    int koeftrans = (int)App.ExecuteInt32("select koeftrans from point where id = {0}", pointID);*/

                    int pointcode = (int)App.ExecuteInt32(
                            "select id from pointcode \n" +
                            "where factory = {0} and sourcetype = {1}\n",
                            point.GetAttribute("number"), sourcetypeID);

                    int pointID = (int)App.ExecuteInt32(
                    "select point from pointcode \n" +
                    "where id = {0}\n",
                    pointcode);
             


                    int Invert = (int)App.ExecuteInt32("select invert from pointcode where id = {0}", pointcode);

                    int koeftrans = (int)App.ExecuteInt32("select koeftrans from pointcode where id = {0}", pointcode);


                    XmlNodeList periodList = point.SelectNodes("./period");

                    foreach (XmlElement period in periodList)
                    {
                        String dateBegin = period.GetAttribute("begin");
                        DateTime periodDate = EPQS.StringToDateTime(period.GetAttribute("begin"));

                        XmlNodeList valuesList = period.SelectNodes("./values");
                       
                        foreach (XmlElement value in valuesList)
                        {
                            string strBegin = value.GetAttribute("begin");
                            string strEnd = value.GetAttribute("end");

                            DateTime begin = periodDate.AddHours(Int32.Parse(strBegin.Substring(0, 2)));
                            begin = begin.AddMinutes(Int32.Parse(strBegin.Substring(2, 2)));

                            DateTime end = periodDate.AddHours(Int32.Parse(strEnd.Substring(0, 2)));
                            end = end.AddMinutes(Int32.Parse(strEnd.Substring(2, 2)));

                            if (begin > end)
                                end = end.AddDays(1);

                            XmlNodeList channelList = value.SelectNodes("./channel");

                            foreach (XmlElement channel in channelList)
                            {
                                int channelID = App.getChannel(channel.GetAttribute("code"), Invert);

                                NumberFormatInfo provider = new NumberFormatInfo();
                                provider.NumberDecimalSeparator = ".";

                                float power = float.Parse(channel.GetAttribute("power"), provider) * koeftrans;

                                int datasourceID = App.getDataSource(pointID, channelID, sourcetypeID);
                                App.BeginUpdate(datasourceID, begin, end);

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

                                App.EndUpdate();

                            }                                                     

                        }
                    }
                }
            }


        }
    }
    
}
