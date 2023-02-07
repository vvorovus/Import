using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;
using NExcel;

namespace Import
{
    public class RD
    {
        public const string PointIndex = "Код ГТП";
        public const string MounthIndex = "Месяц";
        public const string DateIndex = "Дата/Час суток";

        public static int GlobalIndex = 0;
        public static int GlobalPercent = 0;
        public static int GlobalCount = 0;
        public static int IndexRow = 0;

        public static NumberFormatInfo NumberProvider;
        
        static RD()
        {
            NumberProvider = new NumberFormatInfo();
            NumberProvider.NumberDecimalSeparator = ".";
        }
        public static DateTime StringToDateTime(string text)
        {
            // "01.01.09"

            text = text.Trim();

            int yyyy = int.Parse(text.Substring(6, 4)); // год
            int mm = int.Parse(text.Substring(3, 2)); // месяц
            int dd = int.Parse(text.Substring(0, 2)); // день

            int GTM = 0;
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
        public static int getPointNumber(Sheet sheet)
        {
            for (int i = 0; i < sheet.Columns; i++)
            {
                string value = C.Str(sheet.getCell(i,17).Value);
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

        public static void Parse(string Path)
        {
            string[] fileEntries = Directory.GetFiles(Path);
            int year = 2009;
            GlobalCount = fileEntries.Length * 365;
            
            /*App.ExecuteNonQuery("delete from data where id in (" +
                "select dt.id from data dt " +
                "join datasource ds On ds.id = dt.datasource and ds.sourcetype = 50 and ds.point in (1,2) " +
                "join period per On per.id = dt.period and YEAR(per.[date+3DL]) = {0})", year);
            */
            foreach (string fileName in fileEntries)
            {
                ProcessFile(fileName);
            }
        }

        public static void ProcessFile(string fileName)
        {
            int sourcetypeID = (int)App.ExecuteInt32(
                               "select id from sourcetype \n" +
                               "where code = '{0}'\n",
                               "РД");

            Workbook workbook = Workbook.getWorkbook(fileName);
            Sheet sheet = workbook.Sheets[0];
            Cell cell;
            int pointID;
            pointID = 0;
            
            cell = sheet.findLabelCell("PABAKSB1");
            if (cell != null)
                pointID = 1;
            else
            {
                cell = sheet.findLabelCell("PABAKSB2");
                if (cell != null)
                    pointID = 2;
                else
                {
                    throw new Exception("Код ГТП не найден");
                    
                }
            }
              

                       
            int datasourceID = App.getDataSource(pointID, 1, sourcetypeID);
            int DateNumber = sheet.findLabelCell(DateIndex).Row;
            int HourNumber = sheet.findLabelCell("0-1").Column;
            
            DateTime periodBegin, periodEnd, DayDate;
            float value;

            for (int irow = DateNumber + 2; irow < sheet.Rows; irow+=2)
            {

                try
                {
                    DayDate = StringToDateTime(sheet.getCell(1, irow).Value.ToString());
                    //DayDate = DateTime.Parse("29.03.2009");

                    for (int j = 0; j < 24; j++)
                    {
                        DateTime begin;

                        if (DayDate.AddHours(j).IsDaylightSavingTime())
                        {
                            begin = DayDate.AddHours(j - 4);
                        }
                        else
                        {
                            begin = DayDate.AddHours(j - 3);
                        }

                        value = float.Parse(sheet.getCell(HourNumber + j * 2, irow).Contents); //StringToFloat(C.Str(sheet.getCell(HourNumber + j * 2, irow).Value));
                        value = value + float.Parse(sheet.getCell(HourNumber + j * 2, irow + 1).Contents); //StringToFloat(C.Str(sheet.getCell(HourNumber + j * 2, irow + 1).Value));
                        value = value * 500; 
                        
                        int period = App.GetPeriod(begin, begin.AddMinutes(30));

                        if (App.GetData(datasourceID, period) == 0)
                        {
                            App.ExecuteNonQuery("Insert into data([datasource],[period],[value]) Values({0},{1},{2})", datasourceID.ToString(), period.ToString(), value.ToString(NumberProvider));
                        }
                        else
                        {
                            App.ExecuteNonQuery("Update data Set [value] = [value] + {0} Where [datasource] = {1} and [period] = {2}", value.ToString(NumberProvider), datasourceID.ToString(), period.ToString());
                        }
                        period = App.GetPeriod(begin.AddMinutes(30), begin.AddHours(1));
                        if (App.GetData(datasourceID, period) == 0)
                        {
                            App.ExecuteNonQuery("Insert into data([datasource],[period],[value]) Values({0},{1},{2})", datasourceID.ToString(), period.ToString(), value.ToString(NumberProvider));
                        }
                        else
                        {
                            App.ExecuteNonQuery("Update data Set [value] = [value] + {0} Where [datasource] = {1} and [period] = {2}", value.ToString(NumberProvider), datasourceID.ToString(), period.ToString());
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
