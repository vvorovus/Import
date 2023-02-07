using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Globalization;
using NExcel;

namespace Import
{
    class PlanExcel
    {
        public const string PointIndex = "Клиент";
        public const string DateIndex = "Дата";

        public static int GlobalIndex = 0;
        public static int GlobalPercent = 0;
        public static int GlobalCount = 0;
        public static int IndexRow = 0;

        public static NumberFormatInfo NumberProvider;
        static PlanExcel()
        {
            NumberProvider = new NumberFormatInfo();
            NumberProvider.NumberDecimalSeparator = ",";
        }

        public static DateTime StringToDateTime(string text)
        {
            // "01.01.09"
            
            int yyyy = int.Parse(text.Substring(6, 4)); // год
            int mm = int.Parse(text.Substring(3, 2)); // месяц
            int dd = int.Parse(text.Substring(0, 2)); // день
            
            int GTM = +8;
            DateTime d = new DateTime(yyyy, mm, dd, 0, 0, 0);
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
                                "ПРОГНОЗ");

            Workbook workbook = Workbook.getWorkbook(FileName);
            Sheet sheet = workbook.Sheets[0];
            GlobalCount = sheet.Rows;

            int DateNumber = getColumnIndex(sheet, DateIndex);
            int pointNumber = getPointNumber(sheet);
            int pointID = App.ExecuteInt32("select point from pointCustomer where CustomerID = {0}", pointNumber);

//            int pcID = App.ExecuteInt32("select id from pointcode where factory = {0}", pointNumber);
  //          int pointID = App.ExecuteInt32("select point from pointcode where id = {0}", pcID);

            if (pointID == 0) throw new Exception("Точка \"" + pointID.ToString() + "\" не найдена");

            int datasourceID = App.getDataSource(pointID, 1, sourcetypeID);

            DateTime periodBegin, periodEnd, DayDate;
            float value;

            // for each row
            for (int irow = DateNumber + 1; irow < sheet.Rows; irow++)
            {
                                
                try
                {
                    DayDate = StringToDateTime(sheet.getCell(0, irow).Value.ToString());

                    for (int i = 0; i < 24; i++)
                    {

                        periodBegin = DayDate.AddMinutes(i * 60);
                        /*if (periodBegin.IsDaylightSavingTime())
                        {
                            periodBegin = periodBegin.AddHours(-1);
                        }*/
                        periodEnd = periodBegin.AddMinutes(30);
                        
                        value = StringToFloat(C.Str(sheet.getCell(i+1, irow).Value));
                        if (value > 0)
                        {
                            //App.BeginUpdate(datasourceID, periodBegin, periodEnd);
                            App.InsertPeriod(datasourceID, periodBegin, periodEnd, value / 2);
                            periodBegin = periodEnd;
                            periodEnd = periodBegin.AddMinutes(30);
                            //App.BeginUpdate(datasourceID, periodBegin, periodEnd);
                            App.InsertPeriod(datasourceID, periodBegin, periodEnd, value / 2);
                        }
                                                
                    }
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
