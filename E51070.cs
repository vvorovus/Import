using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Import
{
    public class E51070
    {
        public static bool invert = true;

        public static DateTime StringToDateTime(string text)
        {
            int yyyy = int.Parse(text.Substring(0, 4)); //год
            int mm = int.Parse(text.Substring(4, 2)); //месяц
            int dd = int.Parse(text.Substring(6, 2)); //день
            int hh = int.Parse(text.Substring(8, 2)); //час
            int nn = int.Parse(text.Substring(10, 2)); //минута
            int ss = 0; // секунда
            int pos = text.IndexOf("GMT"); // если время без секунд - do not localize
            if (pos == 14)
            {
                mm = int.Parse(text.Substring(12, 2));
            }
            DateTime d = new DateTime(yyyy, mm, dd, hh, nn, ss);

            int zone = int.Parse(text.Substring(pos + 3, 2)); // приведение GMT: +7
            d = d.AddHours(-zone);
            if (text.Length == (pos + 7))
            {
                string dl = text.Substring(pos + 5, 2);
                if (dl == "DL")
                    d = d.AddHours(-1);
            }

            return d;
        }
        public static int getGtp(string code)
        {
            if (code == "PABAKSB1")
            {
                return 1;
            }
            else if (code == "PABAKSB2")
            {
                return 2;
            }
            else
            {
                return -1;
            }
            
        }
        
        public static int getAdj(string codeto, string codefrom)
        {
            if (codeto == "PABAKSB1" && codefrom == "PHAKASEN")
            {
                return 11;
            }
            else if (codeto == "PABAKSB2" && codefrom == "PMAREM34")
            {
                return 21;
            }
            else if (codeto == "PABAKSB2" && codefrom == "PHAKASEN")
            {
                return 21;
            }
            else if (codeto == "PABAKSB1" && codefrom == "PRUSGDS2")
            {
                return 12;
            }
            else if (codeto == "POBOR372" && codefrom == "PABAKSB1")
            {
                invert = false;
                return 13;
            }
            else if (codeto == "FHAKASEN" && codefrom == "PABAKSB1")
            {
                invert = false;
                return 14;
            }
            else if (codeto == "PSIBUE40" && codefrom == "PABAKSB1")
            {
                invert = false;
                return 15;
            }
            else if (codeto == "PSIBUE41" && codefrom == "PABAKSB1")
            {
                invert = false;
                return 16;
            }
            else
            {
                return -1;
            }
            
        }
        public static void Parse(System.Xml.XmlDocument doc)
        {
            XmlNodeList flows = doc.SelectNodes("//flow");
            int GlobalCount = flows.Count;
            int GlobalIndex = 0;
            int GlobalPercent = 0;

            int sourcetypeID = (int)App.ExecuteInt32(
                                "select id from sourcetype \n" +
                                "where code = '{0}'\n",
                                "51070");


            XmlNodeList group = doc.SelectNodes("//group");
            XmlNodeList adjacent = doc.SelectNodes("//adjacent");

            if (group.Count != 0)
            {

                foreach (XmlElement grp in group)
                {
                    int gtp = getGtp(grp.Attributes["code"].Value);
                    if (gtp == -1)
                        return;

                    int pointcodeID = (int)App.ExecuteInt32(
                        "select id from pointcode \n" +
                        "where point = {0} and sourcetype = {1}\n",
                        gtp, sourcetypeID);

                    int pointID = gtp;
                    int channelID = 1;
                    int datasourceID = App.getDataSource(pointID, 1, sourcetypeID);

                    flows = grp.SelectNodes("./flow");

                    foreach (XmlElement flow in flows)
                    {

                        DateTime begin = StringToDateTime(flow.Attributes["begin"].Value);
                        DateTime end = begin.AddMinutes(30);

                        float value = float.Parse(flow.Attributes["power"].Value);
                        App.InsertPeriod(datasourceID, begin, end, value / 2);

                        begin = end;
                        end = begin.AddMinutes(30);
                        App.InsertPeriod(datasourceID, begin, end, value / 2);
                        
                        

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
            else if (adjacent != null)
            {
                foreach (XmlElement adjacents in adjacent)
                {
                    int adj = getAdj(adjacents.Attributes["code-to"].Value, adjacents.Attributes["code-from"].Value);
                    if (adj == -1)
                        return;

                    int pointcodeID = (int)App.ExecuteInt32(
                        "select id from pointcode \n" +
                        "where point = {0} and sourcetype = {1}\n",
                        adj, sourcetypeID);

                    int pointID = adj;
                    int channelID = 1;
                    int datasourceID = App.getDataSource(pointID, 1, sourcetypeID);

                    flows = adjacents.SelectNodes("./flow");

                    foreach (XmlElement flow in flows)
                    {

                        DateTime begin = StringToDateTime(flow.Attributes["begin"].Value);
                        DateTime end = begin.AddMinutes(30);

                        float value;

                        if (invert)
                        {
                            value = float.Parse(flow.Attributes["power"].Value);
                        }
                        else
                            value = float.Parse(flow.Attributes["power"].Value) * (-1);

                       
                        App.InsertPeriod(datasourceID, begin, end, value / 2);

                        begin = end;
                        end = begin.AddMinutes(30);
                        App.InsertPeriod(datasourceID, begin, end, value / 2);


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

            App.EndUpdate();
        }
    }
}
