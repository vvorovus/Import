using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Globalization;
using NExcel;

namespace Import
{
    class calcfacthour
    {
        public const string IndexFormat = "Ежемесячный отчет по часам пиковой нагрузки для субъектов Российской Федерации";
        public const string DateIndex = "Дата";

        public static int GlobalIndex = 0;
        public static int GlobalPercent = 0;
        public static int GlobalCount = 0;
        public static int IndexRow = 0;

        public static NumberFormatInfo NumberProvider;
        static calcfacthour()
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
            
            DateTime d = new DateTime(yyyy, mm, dd, 0, 0, 0);
                       
            return d;
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
            Workbook workbook = Workbook.getWorkbook(FileName);
            Sheet sheet = workbook.Sheets[0];
            GlobalCount = sheet.Rows;

            int TopicIndex = getColumnIndex(sheet, IndexFormat);
            int DateNumber = getColumnIndex(sheet, DateIndex);

            DateTime DayDate;
            float value;

            // for each row
            for (int irow = DateNumber + 2; irow < sheet.Rows; irow++)
            {
                                
                try
                {
                    DayDate = StringToDateTime(sheet.getCell(0, irow).Value.ToString());
                    value = StringToFloat(C.Str(sheet.getCell(1, irow).Value));
                    App.ExecuteNonQuery("delete from maxhour where [date]='{0}'", DayDate.ToString("yyyyMMdd HH:mm:ss"));
                    
                    App.ExecuteNonQuery("insert into maxhour ([date], [gtp], [hour]) values('{0}', 1,{1})", DayDate.ToString("yyyyMMdd HH:mm:ss"), value.ToString());                                      
                    App.ExecuteNonQuery("insert into maxhour ([date], [gtp], [hour]) values('{0}', 2,{1})",DayDate.ToString("yyyyMMdd HH:mm:ss"),value.ToString());                                      
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
