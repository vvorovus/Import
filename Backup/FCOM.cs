using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Globalization;

namespace Import
{
    public class FCOM
    {
        public static DateTime StringToDateTime(string text)
        {
            int yyyy = int.Parse(text.Substring(0, 4)); // год
            int mm = int.Parse(text.Substring(4, 2)); // месяц
            int dd = int.Parse(text.Substring(6, 2)); // день
            DateTime d = new DateTime(yyyy, mm, dd, 0, 0, 0);
            return d.AddHours(-7);
        }

        public static void Parse(System.Xml.XmlDocument doc)
        {
            XmlNodeList periods = doc.SelectNodes("//period");
            int GlobalCount = periods.Count;
            int GlobalIndex = 0;
            int GlobalPercent = 0;

            XmlNode node = doc.SelectSingleNode("./data");
            string number = node.Attributes["number"].Value;

            int pointcodeID = App.ExecuteInt32("select id from pointcode where factory = '{0}'", number);
            if (pointcodeID == 0)
                throw new Exception("Счетчик не найден: '" + number + "'");

            int pointID = App.ExecuteInt32("select point from pointcode where id = {0}", pointcodeID);
            int koeftrans = (int)App.ExecuteInt32("select koeftrans from pointcode where id = {0}", pointcodeID);

            int sourcetypeID = App.ExecuteInt32(
                "select id from sourcetype \n" +
                "where code = '{0}'\n",
                "СТЭБ");

            int channelID = App.ExecuteInt32("select id from channel where abbr = '+A'");

            NumberFormatInfo provider = new NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";

            foreach (XmlElement day in doc.SelectNodes("//day"))
            {
                DateTime date = StringToDateTime(day.Attributes["date"].Value);
                int datasourceID = App.getDataSource(pointID, channelID, sourcetypeID);
                App.BeginUpdate(datasourceID, date, date.AddHours(24));

                foreach (XmlElement period in day.SelectNodes("./period"))
                {
                    int time = Int32.Parse(period.Attributes["time"].Value);
                    DateTime begin = date.AddMinutes(time * 30);
                    DateTime end = date.AddMinutes((time + 1) * 30);

                    Double value = Double.Parse(period.Attributes["value"].Value, provider) * koeftrans;

                    App.InsertPeriod(datasourceID, begin, end, (float)value);

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
