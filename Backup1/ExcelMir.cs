using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Globalization;
using NExcel;

namespace Import
{
    class ExcelMir
    {       
    
        public const string A_plus = "�������� ������ (���)";
        public const string A_minus = "�������� �������� (���)";
        public const string R_plus = "���������� ������ (����)";
        public const string R_minus = "���������� �������� (����)";
        public const string Metka = "����� �������";
        public const string PointNumber = "�������� �����";


        public static int GlobalIndex = 0;
        public static int GlobalPercent = 0;
        public static int GlobalCount = 0;
        public static int IndexRow = 0;
        public static int Season = 0;

        public static NumberFormatInfo NumberProvider;
        static ExcelMir()
        {
            NumberProvider = new NumberFormatInfo();
            NumberProvider.NumberDecimalSeparator = ",";
        }

        public static DateTime StringToDateTime(string text)
        {
            // "25.09.2007 00:30"
            
            int yyyy = int.Parse(text.Substring(6, 4)); // ���
            int mm = int.Parse(text.Substring(3, 2)); // �����
            int dd = int.Parse(text.Substring(0, 2)); // ����
            int hh, nn, ss;
            if (text.Length >18)
            {
                hh = int.Parse(text.Substring(11, 2)); // ���
                nn = int.Parse(text.Substring(14, 2)); // ������
                ss = 0; // �������
            }
            else
            {
                hh = int.Parse(text.Substring(11, 1)); // ���
                nn = int.Parse(text.Substring(13, 2)); // ������
                ss = 0; // �������
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

            int GTM = +7;
            DateTime d = new DateTime(yyyy, mm, dd, hh, nn, ss);
            d = d.AddHours(-GTM + Season);
            
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

        public static int getColumnIndex(Sheet sheet, string channelName)
        {
            int i;
            for (i = 0; i < sheet.Rows; i++)
            {
                string value = C.Str(sheet.getCell(0, i).Value);
                if (value.IndexOf(Metka) > -1)
                {
                    IndexRow = i;
                    break;
                }
            }
            for (i = 0; i < sheet.Columns; i++)
            {
                string value = C.Str(sheet.getCell(i, IndexRow).Value);
                if (value.IndexOf(channelName) > -1)
                    return i;
            }
            throw new EErrorFormat();
        }
        public static int getPointNumber(Sheet sheet)
        {
            for (int i = 0; i < sheet.Rows; i++)
            {
                string value = C.Str(sheet.getCell(0, i).Value);
                if (value.IndexOf(PointNumber) > -1)
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
                                "ExcelMir");
            
            Workbook workbook = Workbook.getWorkbook(FileName);
            Sheet sheet = workbook.Sheets[0];
            GlobalCount = sheet.Rows * 4;

            int a_plusNumber = getColumnIndex(sheet, A_plus);
            int a_minusNumber = getColumnIndex(sheet, A_minus);
            int r_plusNumber = getColumnIndex(sheet, R_plus);
            int r_minusNumber = getColumnIndex(sheet, R_minus);

            int pointNumber = getPointNumber(sheet);

            int pcID = App.ExecuteInt32("select id from pointcode where factory = {0}", pointNumber);

            int pointID = App.ExecuteInt32("select point from pointcode where id = {0}", pcID);

            if (pointID == 0) throw new Exception("����� \"" + pointNumber.ToString() + "\" �� �������");
            
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
            for (int irow = IndexRow+1; irow < sheet.Rows; irow++)
            {
                try
                {
                    if (sheet.getCell(10, irow).Value.ToString() == "����")
                    {
                        Season = -1;
                    }
                    else
                    {
                        Season = 0;
                    }
                    periodBegin = StringToDateTime(sheet.getCell(0, irow).Value.ToString());
                    a_plus = StringToFloat(C.Str(sheet.getCell(a_plusNumber, irow).Value))*koeftrans/2;
                    r_plus = StringToFloat(C.Str(sheet.getCell(r_plusNumber, irow).Value)) * koeftrans/2;
                    a_minus = StringToFloat(C.Str(sheet.getCell(a_minusNumber, irow).Value)) * koeftrans/2;
                    r_minus = StringToFloat(C.Str(sheet.getCell(r_minusNumber, irow).Value)) * koeftrans/2;

                    periodEnd = periodBegin.AddMinutes(30);

                    App.BeginUpdate(a_plus_datasourceID, periodBegin, periodEnd);
                    App.InsertPeriod(a_plus_datasourceID, periodBegin, periodEnd, a_plus);
                    App.BeginUpdate(a_minus_datasourceID, periodBegin, periodEnd);
                    App.InsertPeriod(a_minus_datasourceID, periodBegin, periodEnd, a_minus);
                    App.BeginUpdate(r_plus_datasourceID, periodBegin, periodEnd);
                    App.InsertPeriod(r_plus_datasourceID, periodBegin, periodEnd, r_plus);
                    App.BeginUpdate(r_minus_datasourceID, periodBegin, periodEnd);
                    App.InsertPeriod(r_minus_datasourceID, periodBegin, periodEnd, r_minus);
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
    