using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Import
{
    public class F80021
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

        public static void Parse(System.Xml.XmlDocument doc)
        {
            XmlNodeList lst = doc.SelectNodes("//area");
            int GlobalCount = lst.Count;
            int GlobalIndex = 0;
            int GlobalPercent = 0;
          
            
            string buf;
            string delimStr = "_";
            char[] delimiter = delimStr.ToCharArray();
            string[] split = null;

            XmlNodeList messageList = doc.SelectNodes("//message");
            foreach (XmlElement message in messageList)
            {
                XmlElement file = (XmlElement)message.SelectSingleNode("./file");
                string s = file["day"].InnerText;
                DateTime baseDay = StringToDateTime(s);

                XmlNodeList currentstateList = message.SelectNodes("./currentstate");
                foreach (XmlElement currentstate in currentstateList)
                {
                    XmlNodeList areaList = currentstate.SelectNodes("./area");
                    foreach (XmlElement area in areaList)
                    {
                        int point = 0;
                        string code = area.GetAttribute("code");
                        if(code == "1900000904")
                            point = 1;
                        if(code == "1900000902")
                            point = 2;

                        int status = C.Int(area.GetAttribute("status"));
                        if (point !=0)
                        {
                            buf = area.GetAttribute("fromfile");
                            split = buf.Split(delimiter);
                            int number80021 = C.Int(split[3]);
                            App.Insert80021(point, status, number80021,buf,baseDay);
                        }

                        
                    }
                }
            }
            
        }
    }
}