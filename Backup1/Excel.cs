using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Globalization;
using NExcel;

namespace Import
{
    public class Excel
    {
        public const string A_plus = ". Активная энергия, приём\nЗначения на интервале, кВтч";
        public const string A_minus = ". Активная энергия, отдача\nЗначения на интервале, кВтч";
        public const string R_plus = ". Реактивная энергия, приём\nЗначения на интервале, кврч";
        public const string R_minus = ". Реактивная энергия, отдача\nЗначения на интервале, кврч";

        public static int GlobalIndex = 0;
        public static int GlobalPercent = 0;
        public static int GlobalCount = 0;

        public static NumberFormatInfo NumberProvider;
        static Excel()
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

            int hh = int.Parse(text.Substring(11, 2)); // час
            int nn = int.Parse(text.Substring(14, 2)); // минута
            int ss = 0; // секунда
            int GTM = +7;
            DateTime d = new DateTime(yyyy, mm, dd, hh, nn, ss);
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
                    return C.Float(text.Replace("[$0001]", ""),0); //float.Parse(text.Replace("[$0001]", ""), NumberProvider);
            }
            catch
            {
                return -1;
            }
        }

        public static void insertPeriod(int datasourceID, DateTime periodBegin, DateTime periodEnd, float Value)
        {
            App.InsertPeriod(datasourceID, periodBegin, periodEnd, Value);

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
                                "ЖД");
            
            Workbook workbook = Workbook.getWorkbook(FileName);
            Sheet sheet = workbook.Sheets[0];
            GlobalCount = sheet.Rows * 4;

            int a_plusNumber = getColumnIndex(sheet, A_plus);
            int a_minusNumber = getColumnIndex(sheet, A_minus);
            int r_plusNumber = getColumnIndex(sheet, R_plus);
            int r_minusNumber = getColumnIndex(sheet, R_minus);

            string pointName = C.Str(sheet.getCell(a_plusNumber, 0).Value).Replace(A_plus, "");

            int pcID = App.ExecuteInt32("select id from pointcode where name = '{0}'", pointName);

            int pointID = App.ExecuteInt32("select point from pointcode where id = {0}", pcID);

            if (pointID == 0) throw new Exception("Точка \"" + pointName + "\" не найдена");
            
            int koeftrans = App.ExecuteInt32("select koeftrans from pointcode where id = {0}", pcID);
            int invert = App.getInvert(pointID, sourcetypeID);
            int a_plusID = App.getChannel("+A", invert);
            int a_minusID = App.getChannel("-A", invert);
            int r_plusID = App.getChannel("+R", invert);
            int r_minusID = App.getChannel("-R", invert);

            int a_plus_datasourceID = App.getDataSource(pointID, a_plusID, sourcetypeID);
            int a_minus_datasourceID = App.getDataSource(pointID, a_minusID, sourcetypeID);
            int r_plus_datasourceID = App.getDataSource(pointID, r_plusID, sourcetypeID);
            int r_minus_datasourceID = App.getDataSource(pointID, r_minusID, sourcetypeID);

            DateTime periodBegin, periodEnd;
            float a_plus, a_minus, r_plus, r_minus;

            // for each row
            for (int irow = 1; irow < sheet.Rows; irow++)
            {
                try
                {
                    periodEnd = StringToDateTime(C.Str(sheet.getCell(1, irow).Value));
                    a_plus = StringToFloat(C.Str(sheet.getCell(a_plusNumber, irow).Value));
                    r_plus = StringToFloat(C.Str(sheet.getCell(r_plusNumber, irow).Value));
                    a_minus = StringToFloat(C.Str(sheet.getCell(a_minusNumber, irow).Value));
                    r_minus = StringToFloat(C.Str(sheet.getCell(r_minusNumber, irow).Value));
                    if (a_minus <0 || a_plus <0 || r_minus<0 || r_plus<0)
                    {
                        a_plus *= (-1); 
                    }

                    periodBegin = periodEnd.AddMinutes(-30);

                    App.BeginUpdate(a_plus_datasourceID, periodBegin, periodEnd);
                    insertPeriod(a_plus_datasourceID, periodBegin, periodEnd, a_plus);
                    App.BeginUpdate(a_minus_datasourceID, periodBegin, periodEnd);
                    insertPeriod(a_minus_datasourceID, periodBegin, periodEnd, a_minus);
                    App.BeginUpdate(r_plus_datasourceID, periodBegin, periodEnd);
                    insertPeriod(r_plus_datasourceID, periodBegin, periodEnd, r_plus);
                    App.BeginUpdate(r_minus_datasourceID, periodBegin, periodEnd);
                    insertPeriod(r_minus_datasourceID, periodBegin, periodEnd, r_minus);
                }
                catch
                {

                }
            }
            App.EndUpdate();
        }
    }
}