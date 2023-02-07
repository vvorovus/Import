using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Globalization;
using NExcel;

namespace Import
{
    class Buildings
    {
        public const string Format = "Жилые дома";
        public const string C_time = "Время";

        public static int GlobalIndex = 0;
        public static int GlobalPercent = 0;
        public static int GlobalCount = 0;

        public static NumberFormatInfo NumberProvider;
        static Buildings()
        {
            NumberProvider = new NumberFormatInfo();
            NumberProvider.NumberDecimalSeparator = ",";
        }

        public static DateTime StringToDateTime(string text)
        {
            // "01.01.2012 01:00"
            int yyyy = int.Parse(text.Substring(6, 4)); // год
            int mm = int.Parse(text.Substring(3, 2)); // месяц
            int dd = int.Parse(text.Substring(0, 2)); // день
            int hh, nn, ss;
            if (text.Length > 18)
            {
                hh = int.Parse(text.Substring(11, 2)); // час
                nn = int.Parse(text.Substring(14, 2)); // минута
                ss = 0; // секунда
            }
            else
            {
                hh = int.Parse(text.Substring(11, 1)); // час
                nn = int.Parse(text.Substring(13, 2)); // минута
                ss = 0; // секунда
            }
            int diff00 = 0 - nn;
            int diff30 = 30 - nn;
            int diff60 = 60 - nn;

            if (Math.Abs(diff00) < Math.Abs(diff30) && Math.Abs(diff00) < Math.Abs(diff60))
                nn += diff00;

            if (Math.Abs(diff30) < Math.Abs(diff00) && Math.Abs(diff30) < Math.Abs(diff60))
                nn += diff30;

            if (Math.Abs(diff60) < Math.Abs(diff00) && Math.Abs(diff60) < Math.Abs(diff30))
            {
                nn = 0;
                hh += 1;
            }

            int GTM = +8;
            DateTime d = new DateTime(yyyy, mm, dd, hh, nn, ss);
            d = d.AddHours(-GTM);

            return d;
        }
        public static int getRowsIndex(Sheet sheet, string channelName)
        {
            for (int i = 0; i < sheet.Rows; i++)
            {
                string value = C.Str(sheet.getCell(0,i).Value);
                
                if (value.IndexOf(channelName) > -1)
                    return i;
            }
            throw new EErrorFormat();
        }
        public static float StringToFloat(string text)
        {
            try
            {
                if (text == "-")
                    return -1;
                else
                    return float.Parse(text, NumberProvider);
            }
            catch
            {
                return -1;
            }
        }

        public static void Parse(string FileName)
        {
            int sourcetypeID = (int)App.ExecuteInt32(
                                "select id from sourcetype \n" +
                                "where code = '{0}'\n",
                                "Sum");

            Workbook workbook = Workbook.getWorkbook(FileName);
            Sheet sheet = workbook.Sheets[0];
            GlobalCount = sheet.Rows;

            int format = getRowsIndex(sheet, Format);
            int TimeNumber = getRowsIndex(sheet, C_time);

            for (int j = 1; j <= 1; j++)
            {
                string pointname = C.Str(sheet.getCell(j,TimeNumber).Value);
                int point = (int)App.ExecuteInt32("select id from point where name = '{0}'",
                                pointname);
                int datasource = (int)App.ExecuteInt32("select id from datasource where point = {0} and sourcetype = 7 and channel = 1",
                                point);

                 DateTime periodBegin, periodEnd;
                 float value;

                 for (int irow = TimeNumber+1; irow < sheet.Rows; irow++)
                 {
                     try
                     {
                         periodEnd = StringToDateTime(sheet.getCell(0, irow).Value.ToString());
                         value = StringToFloat(C.Str(sheet.getCell(j, irow).Value));
                         periodBegin = periodEnd.AddMinutes(-30);
                         App.InsertPeriod(datasource, periodBegin, periodEnd, value / 2);
                         
                         periodBegin = periodEnd.AddHours(-1);
                         periodEnd = periodBegin.AddMinutes(30);                         
                         App.InsertPeriod(datasource, periodBegin, periodEnd, value / 2);

                     }
                     catch { }

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
