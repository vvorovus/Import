using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using NExcel;

namespace Import
{
    class Arena
    {
        public const string PointIndex = "Код ГТП";
        public const string MounthIndex = "Месяц";
        public const string DateIndex = "Дата";

        public static int GlobalIndex = 0;
        public static int GlobalPercent = 0;
        public static int GlobalCount = 0;
        public static int IndexRow = 0;

        public static NumberFormatInfo NumberProvider;
        static Arena()
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
            
            int GTM = +4;
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
                
                    if (number == "PABAKSB1")
                    {
                        return 1;
                        
                    }
                    else if (number == "PABAKSB2")
                    {
                        return 2;
                    }
                    else
                    {
                        return 0;
                    }
                    
                }
            }
            throw new EErrorFormat();
         
        }

        public static void Parse(string FileName)
        {
            
            int sourcetypeID = (int)App.ExecuteInt32(
                                "select id from sourcetype \n" +
                                "where code = '{0}'\n",
                                "Биржа");

            Workbook workbook = Workbook.getWorkbook(FileName);
            Sheet sheet = workbook.Sheets[0];
            GlobalCount = sheet.Rows;

            int DateNumber = getColumnIndex(sheet, DateIndex);
            int MonthNumber = getColumnIndex(sheet, MounthIndex);
            int pointID = getPointNumber(sheet);
            
            if (pointID == 0) throw new Exception("Точка \"" + pointID.ToString() + "\" не найдена");
            int datasourceID = App.getDataSource(pointID, 1, sourcetypeID);

            int month = C.Int(sheet.getCell(1, MonthNumber).Value.ToString());
            App.ExecuteNonQuery("delete from data where id in (" +
                "select dt.id from data dt " +
                "join datasource ds On ds.id = dt.datasource and ds.sourcetype = 60 and ds.point = {0} " +
                "join period per On per.id = dt.period and Month(per.[date+3DL]) = {1})", pointID, month);
            

            DateTime periodBegin, periodEnd, DayDate;
            float value;

            // for each row
            for (int irow = DateNumber + 1; irow < sheet.Rows; irow++)
            {
                                
                try
                {
                    DayDate = StringToDateTime(sheet.getCell(0, irow).Value.ToString());

                    string hours;
                    hours = C.Str(sheet.getCell(1, irow).Value);
                    while (hours.Length > 0)
                    {
                        string hour;
                        DateTime begin;
                        int index = hours.IndexOf(',');

                        if (index == -1)
                        {
                            hour = hours.Trim();
                            index = hours.Length - 1;
                        }
                        else
                        {
                            hour = hours.Remove(index);
                            hour = hour.Trim();                            
                        }
                        hours = hours.Remove(0, index + 1);
                        
                        begin = DayDate.AddHours(C.Int(hour));
                        value = StringToFloat(C.Str(sheet.getCell(2, irow).Value));
                        value *= 500;
                        if (begin.IsDaylightSavingTime())
                        {
                            begin = begin.AddHours(-1);

                        }
                        
                        int period = App.GetPeriod(begin,begin.AddMinutes(30));

                        if (App.GetData(datasourceID, period) == 0)
                        {
                            App.ExecuteNonQuery("Insert into data([datasource],[period],[value]) Values({0},{1},{2})", datasourceID.ToString(), period.ToString(), value.ToString());
                        }
                        else
                        {
                            App.ExecuteNonQuery("Update data Set [value] = [value] + {0} Where [datasource] = {1} and [period] = {2}", value.ToString(), datasourceID.ToString(), period.ToString());
                        }
                        period = App.GetPeriod(begin.AddMinutes(30),begin.AddHours(1));
                        if (App.GetData(datasourceID, period) == 0)
                        {
                            App.ExecuteNonQuery("Insert into data([datasource],[period],[value]) Values({0},{1},{2})", datasourceID.ToString(), period.ToString(), value.ToString());
                        }
                        else
                        {
                            App.ExecuteNonQuery("Update data Set [value] = [value] + {0} Where [datasource] = {1} and [period] = {2}", value.ToString(), datasourceID.ToString(), period.ToString());
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
         
        }
    }
}
