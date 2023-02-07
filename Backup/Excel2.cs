using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Globalization;
using NExcel;

namespace Import
{
    public class Excel2
    {
        public const string A_plus = "[Неконтролируемое:Потр без САЗ ( МЭС и ГПП5)]";
        public const string C_date = "Дата";
        public const string C_time = "Время";

        public static NumberFormatInfo NumberProvider;
        static Excel2()
        {
            NumberProvider = new NumberFormatInfo();
            NumberProvider.NumberDecimalSeparator = ".";
        }

        public static void StringToDateTime(string date, string time, out DateTime begin, out DateTime end)
        {
            // "01.06.2007" Московское время :)
            int yyyy = int.Parse(date.Substring(6, 4)); // год
            int mm = int.Parse(date.Substring(3, 2)); // месяц
            int dd = int.Parse(date.Substring(0, 2)); // день

            int hh1 = int.Parse(time.Substring(0, 2)); // час
            int nn1 = int.Parse(time.Substring(3, 2)); // минута
            int ss = 0;

            int hh2 = int.Parse(time.Substring(6, 2)); // час
            int nn2 = int.Parse(time.Substring(9, 2)); // минута

            int GTM = +4;

            begin = new DateTime(yyyy, mm, dd, hh1, nn1, ss);
            begin = begin.AddHours(-GTM);

            if (hh2 == 24)
            {
                hh2 = 00;
                dd += 1;
            }
            end = new DateTime(yyyy, mm, dd, hh2, nn2, ss);
            end = end.AddHours(-GTM);
        }

        public static float StringToFloat(string text)
        {
            try
            {
                if (text == "-")
                    return -1;
                else
                    return float.Parse(text.Replace("[$0001]", ""), NumberProvider);
            }
            catch
            {
                return -1;
            }
        }

        public static int getColumnIndex(Sheet sheet, string channelName)
        {
            for (int i = 0; i < sheet.Columns; i++)
            {
                string value = C.Str(sheet.getCell(i, 0).Value);
                if (value.IndexOf(channelName) > -1)
                    return i;
            }
            throw new EErrorFormat();
        }

        public static void Parse(string FileName)
        {
            int sourcetypeID = (int)App.ExecuteInt32(
                                "select id from sourcetype \n" +
                                "where code = '{0}'\n",
                                "TMP2007");
            
            Workbook workbook = Workbook.getWorkbook(FileName);
            Sheet sheet = workbook.Sheets[0];
            Excel.GlobalCount = sheet.Rows * 2;

            int a_plusNumber = getColumnIndex(sheet, A_plus);
            int c_dateNumber = getColumnIndex(sheet, C_date);
            int c_timeNumber = getColumnIndex(sheet, C_time);

            int pointID = 1; // ГТП-1

            int koeftrans = (int)App.ExecuteInt32(
                "select koeftrans from pointcode " +
                "where point = {0} and sourcetype = {1}", pointID, sourcetypeID);

            int invert = App.getInvert(pointID, sourcetypeID);
            int a_plusID = App.getChannel("+A", invert);
            int datasourceID = App.getDataSource(pointID, a_plusID, sourcetypeID);

            DateTime periodBegin, periodEnd;
            float a_plus;
            string date = "";

            // for each row
            for (int irow = 1; irow < sheet.Rows; irow++)
            {
                try
                {
                    string _date = C.Str(sheet.getCell(c_dateNumber, irow).Value);
                    if (_date.Length > 0)
                        date = _date;

                    string time = C.Str(sheet.getCell(c_timeNumber, irow).Value);
                    StringToDateTime(
                        date, 
                        time,
                        out periodBegin, out periodEnd);

                    a_plus = StringToFloat(C.Str(sheet.getCell(a_plusNumber, irow).Value));
                    a_plus = a_plus / 2; // данные будем добавлять в виде получасовок
                    DateTime time1 = periodBegin.AddHours(4);
                    DateTime time2 = periodEnd.AddHours(4);
                    App.BeginUpdate(datasourceID, periodBegin, periodEnd.AddHours(1));
                    Excel.insertPeriod(datasourceID, periodBegin, periodBegin.AddMinutes(30), a_plus);
                    Excel.insertPeriod(datasourceID, periodEnd.AddMinutes(-30), periodEnd, a_plus);
                }
                catch
                {

                }
            }
            App.EndUpdate();
        }
    }
}