using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Globalization;
using NExcel;

namespace Import
{
    class ExcelHour
    {
        public const string FileTypeLabel = "Интервальный акт учета электрической энергии";
        public const string CustomerIDLabel = "Договор №";
        public const string ObjectIDLabel = "Объект №";
        public const string MeterLabel = "№ счетчика";
        public const string DateLabel = "Дата";


        public const string ObjectNameLabel = "Наименование объекта";
        public const string CustomerNameLabel = "Наименование";      
       

        public static int GlobalIndex = 0;
        public static int GlobalPercent = 0;
        public static int GlobalCount = 0;

        public static NumberFormatInfo NumberProvider;
        static ExcelHour()
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

            int GTM = +3;
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

        public static int getColumnIndex(Sheet sheet, string CellName, int row)
        {
            for (int i = 0; i < sheet.Columns; i++)
            {
                string value = C.Str(sheet.getCell(i, row).Value);
                if (value.IndexOf(CellName) > -1)
                    return i;
            }
            throw new EErrorFormat();
        }

        public static int getRowIndex(Sheet sheet, string CellName, int Column)
        {
            for (int i = 0; i < sheet.Rows; i++)
            {
                string value = C.Str(sheet.getCell(Column,i).Value);
                if (value.IndexOf(CellName) > -1)
                    return i;
            }
            throw new EErrorFormat();
        }


        public static void Parse(string FileName)
        {
            Workbook workbook = Workbook.getWorkbook(FileName);
            Sheet sheet = workbook.Sheets[0];
            GlobalCount = sheet.Rows;

            int sourcetypeID = (int)App.ExecuteInt32(
                                "select id from sourcetype \n" +
                                "where code = '{0}'\n",
                                "ExcelPoint");

            int FileTypeRow = getRowIndex(sheet, FileTypeLabel, 0);

            int CustomerIDRow = getRowIndex(sheet, CustomerIDLabel, 0);
            int ObjectIDRow = getRowIndex(sheet, ObjectIDLabel, 0);
            int MeterRow = getRowIndex(sheet, MeterLabel, 0);
            int DateRow = getRowIndex(sheet, DateLabel, 0);


            int CustomerNameRow = getRowIndex(sheet, CustomerNameLabel,6);
            int ObjectNameRow = getRowIndex(sheet, ObjectNameLabel, 6);

            int CustomerID, ObjectID;
            int point, parent;
                    
            string ObjectName, CustomerName, Factory;

            ObjectName = C.Str(sheet.getCell(9,ObjectNameRow).Value);
            CustomerName = C.Str(sheet.getCell(9,CustomerNameRow).Value);

            CustomerID = C.Int(C.Str(sheet.getCell(2,CustomerIDRow).Value));
            ObjectID = C.Int(C.Str(sheet.getCell(2, ObjectIDRow).Value));
            Factory = C.Str(sheet.getCell(2, MeterRow).Value);

            point = App.ExecuteInt32("select point from pointcode where code = '{0}' and sourcetype = {1}", Factory, sourcetypeID);


            if (point == 0)
            {
                int tariff;

                point = App.ExecuteInt32("Select Max(id) from point");
                point++;

                tariff = App.ExecuteInt32("select OP.TariffTypeID from [UNIEL].[dbo].R_Objects O join [UNIEL].[dbo].R_ObjectPoints OP ON OP.ObjectID = O.ID  where O.ID ={0}",ObjectID);
                parent = App.ExecuteInt32("select point from pointCustomer where CustomerID = {0} and TariffTypeID = {1}", CustomerID, tariff);

                if (parent == 0)
                {
                    parent = App.InsertParent(CustomerID, CustomerName,tariff);                    
                }
                
                App.ExecuteScalar("Insert into point (id, name) values ({0},'{1}')", point, ObjectName+" сч."+Factory);
                App.ExecuteScalar("insert into pointparent (point,parent) values ({0},{1})", point, parent);
                App.ExecuteScalar("Insert into datasource (point,channel,sourcetype) values ({0},{1},{2})", point, 1, 7);
                App.ExecuteScalar("Insert into datasource (point,channel,sourcetype) values ({0},{1},{2})", point, 3, 7);
                App.ExecuteScalar("Insert into datasource (point,channel,sourcetype) values ({0},{1},{2})", point, 1, sourcetypeID);
                App.ExecuteScalar("Insert into datasource (point,channel,sourcetype) values ({0},{1},{2})", point, 3, sourcetypeID);
                                
                App.ExecuteScalar("Insert into pointcode (point,name,sourcetype, factory, code, koeftrans, invert, timezone, daylightsavings)" +
                "values ({0},'{1}',{2},'{3}','{3}',1,0,3,1)", point, ObjectName + " сч." + Factory,sourcetypeID,Factory);

            }

            int datasourceID = App.getDataSource(point, 1, sourcetypeID);

            DateTime periodBegin, periodEnd, DayDate;
            float value;

            for (int irow = DateRow + 1; irow < sheet.Rows; irow++)
            {
                try
                {
                    DayDate = StringToDateTime(sheet.getCell(0, irow).Value.ToString());

                    for (int icolumn = 0; icolumn < 24; icolumn++)
                    {
                        periodBegin = DayDate.AddMinutes(icolumn * 60);
                        
                        periodEnd = periodBegin.AddMinutes(30);

                        value = StringToFloat(C.Str(sheet.getCell(icolumn + 1, irow).Value));
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

/*
for (int irow = 1; irow < sheet.Rows; irow++)
{
    try
    {
        pointName = C.Str(sheet.getCell(PointName_Number, irow).Value);
        customerName = C.Str(sheet.getCell(CustomerName_Number, irow).Value);
        profile = C.Int(C.Str(sheet.getCell(Profile_Number, irow).Value));
        customerID = C.Int(C.Str(sheet.getCell(CustomerID_Number, irow).Value));

        if (profile == 1)
        {
            pointcount = App.ExecuteInt32("Select Max(id) from point");

            pointID = pointcount + 1;
            parent = App.ExecuteInt32("Select point from pointCustomer where CustomerID = {0}", customerID);

            App.ExecuteScalar("Insert into point (id, name) values ({0},'{1}')", pointID, pointName.ToString());
            App.ExecuteScalar("Insert into datasource (point,channel,sourcetype) values ({0},{1},{2})", pointID, 1, 7);
            App.ExecuteScalar("Insert into datasource (point,channel,sourcetype) values ({0},{1},{2})", pointID, 3, 7);
            App.ExecuteScalar("Insert into datasource (point,channel,sourcetype) values ({0},{1},{2})", pointID, 1, 10);
            App.ExecuteScalar("Insert into datasource (point,channel,sourcetype) values ({0},{1},{2})", pointID, 3, 10);

            App.ExecuteScalar("Insert into pointparent (point,parent) values ({0},{1})", pointID, parent);
            App.ExecuteScalar("Insert into pointcode (point,name,sourcetype, koeftrans, invert, timezone, daylightsavings)" +
            "values ({0},'{1}',10,1,0,3,1)", pointID, pointName.ToString());



        }


        /*


        customerID = C.Int(C.Str(sheet.getCell(CustomerID_Number, irow).Value));
        pointID = App.ExecuteInt32("Select point from pointCustomer where CustomerID = {0}", customerID);

        if (pointID == 0)
        { 

            pointcount = App.ExecuteInt32("Select Max(id) from point");
            pointName = C.Str(sheet.getCell(PointName_Number, irow).Value);
            customerName = C.Str(sheet.getCell(CustomerName_Number, irow).Value);
            profile = C.Int(C.Str(sheet.getCell(Profile_Number, irow).Value));


            pointID = pointcount +1;
            parent = pointID;
            App.ExecuteScalar("Insert into point (id, name) values ({0},'{1}')", pointID, customerName.ToString());
            App.ExecuteScalar("Insert into datasource (point,channel,sourcetype) values ({0},{1},{2})",pointID,1,7);
            App.ExecuteScalar("Insert into datasource (point,channel,sourcetype) values ({0},{1},{2})", pointID, 3,7);
            App.ExecuteScalar("Insert into datasource (point,channel,sourcetype) values ({0},{1},{2})", pointID, 1, 13);
            App.ExecuteScalar("Insert into pointCustomer (point,CustomerID,gtp,profile) values ({0},{1},{2},{3})", pointID, customerID, 1, profile);



            //App.ExecuteScalar("Insert into pointCustomer (point) "

        }   

    }
    catch
    {

    }
}*/


