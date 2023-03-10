using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Globalization;
using NExcel;

namespace Import
{
    class Correct
    {
        public const string PointIndex = "Point";
        public const string DateIndex = "Время и дата";


        public static int GlobalIndex = 0;
        public static int GlobalPercent = 0;
        public static int GlobalCount = 0;
        public static int IndexRow = 0;

        public static NumberFormatInfo NumberProvider;
        static Correct()
        {
            NumberProvider = new NumberFormatInfo();
            NumberProvider.NumberDecimalSeparator = ",";
        }

        public static DateTime StringToDateTime(string text)
        {
            // "25.09.2007 00:30"

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
            
            int GTM = +3;
            DateTime d = new DateTime(yyyy, mm, dd, hh, nn, ss);
            if (d.IsDaylightSavingTime())
            {
                d = d.AddHours(-1);
            }

            d = d.AddHours(-GTM);

            return d;
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

        public static void insertPeriod(int datasourceID, DateTime periodBegin, DateTime periodEnd, float Value)
        {
            App.InsertPeriod(datasourceID, periodBegin, periodEnd, Value);

        }

        public static int getColumnIndex(Sheet sheet, string Name)
        {
            int i;
            for (i = 0; i < sheet.Rows; i++)
            {
                string value = C.Str(sheet.getCell(0, i).Value);
                if (value.IndexOf(Name) > -1)
                {
                    return i;
                }
            }
            throw new EErrorFormat();
        }
        public static int getPointNumber(Sheet sheet)
        {
            for (int i = 0; i < sheet.Rows; i++)
            {
                string value = C.Str(sheet.getCell(0, i).Value);
                if (value.IndexOf(PointIndex) > -1)
                {
                    string number = C.Str(sheet.getCell(1, i).Value);
                    int num = C.Int(number);
                    return num;
                }
            }
            throw new EErrorFormat();

        }

        public static void Parse(string FileName)
        {
            int sourcetypeID = (int)App.ExecuteInt32(
                                "select id from sourcetype \n" +
                                "where code = '{0}'\n",
                                "Корректировка");

            Workbook workbook = Workbook.getWorkbook(FileName);
            Sheet sheet = workbook.Sheets[0];
            GlobalCount = sheet.Rows;


            int DateNumber = getColumnIndex(sheet, DateIndex);
            int pointID = getPointNumber(sheet);


            if (pointID == 0) throw new Exception("Точка \"" + pointID.ToString() + "\" не найдена");

            int datasourceID = App.getDataSource(pointID, 1, sourcetypeID);
            //int datasourceReact = App.getDataSource(pointID, 3, sourcetypeID);

            DateTime periodBegin, periodEnd, DayDate;
            float value;


            for (int irow = DateNumber + 1; irow < sheet.Rows; irow++)
            {
                try
                {
                    periodEnd = StringToDateTime(sheet.getCell(0, irow).Value.ToString());
                    periodBegin = periodEnd.AddMinutes(-60);

                    /*if (periodBegin.IsDaylightSavingTime())
                    {
                        periodBegin = periodBegin.AddHours(-1);
                    }*/

                    periodEnd = periodBegin.AddMinutes(30);
                    value = StringToFloat(C.Str(sheet.getCell(1, irow).Value));
                    value = value / 2;

                    App.BeginUpdate(datasourceID, periodBegin, periodEnd);
                    App.InsertPeriod(datasourceID, periodBegin, periodEnd, value);

                    periodBegin = periodEnd;
                    
                    /*
                    if (periodBegin.IsDaylightSavingTime())
                    {
                        periodBegin = periodBegin.AddHours(-1);
                    }*/

                    periodEnd = periodBegin.AddMinutes(30);
                    App.BeginUpdate(datasourceID, periodBegin, periodEnd);
                    App.InsertPeriod(datasourceID, periodBegin, periodEnd, value);
                    
                    //                    App.BeginUpdate(datasourceReact, periodBegin, periodEnd);
                    //                      App.InsertPeriod(datasourceReact, periodBegin, periodEnd, value);

                }
                catch
                {

                }
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
            App.EndUpdate();
        }
    }
}