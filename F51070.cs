using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Import
{
    public class F51070
    {
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

        public static void Parse(System.Xml.XmlDocument doc)
        {
            XmlNodeList group = doc.SelectNodes("//flow");
            int GlobalCount = group.Count;
            int GlobalIndex = 0;
            int GlobalPercent = 0;

            XmlNode groupnode = doc.SelectSingleNode("//group");

            if (groupnode != null)
            {
                string groupcode = C.Str(groupnode.Attributes["code"].Value);
                if (groupcode == "PABAKSB1" || groupcode == "PABAKSB2")
                    throw new EErrorFormat();

                foreach (XmlElement flow in group)
                {
                    DateTime begin = StringToDateTime(flow.Attributes["begin"].Value);
                    DateTime end = StringToDateTime(flow.Attributes["end"].Value);
                    int value = int.Parse(flow.Attributes["power"].Value);

                    App.ExecuteNonQuery(
                        "delete from temperature where [begin] >= '{0}' and [end] <= '{1}'\n" +
                        "insert into temperature ([begin], [end], [value]) values('{0}', '{1}', {2})",
                        begin.ToString("yyyyMMdd HH:mm:ss"),
                        end.ToString("yyyyMMdd HH:mm:ss"),
                        value);

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
            else
            {
                throw new EErrorFormat();
            }
        }
    }
}
