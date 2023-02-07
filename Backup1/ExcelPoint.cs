using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Globalization;
using NExcel;

namespace Import
{
    class ExcelPoint
    {
        public const string CustomerID = "Клиент";
        public const string PointName = "Наименование объекта";
        public const string CustomerName = "Наименование клиента";
        public const string Profile = "Профиль";

        public static int GlobalIndex = 0;
        public static int GlobalPercent = 0;
        public static int GlobalCount = 0;

        public static NumberFormatInfo NumberProvider;
        static ExcelPoint()
        {
            NumberProvider = new NumberFormatInfo();
            NumberProvider.NumberDecimalSeparator = ",";
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
            Workbook workbook = Workbook.getWorkbook(FileName);
            Sheet sheet = workbook.Sheets[0];
            GlobalCount = sheet.Rows * 4;

            int CustomerID_Number = getColumnIndex(sheet, CustomerID);
            int PointName_Number = getColumnIndex(sheet, PointName);
            int CustomerName_Number = getColumnIndex(sheet, CustomerName);
            int Profile_Number = getColumnIndex(sheet, Profile);

            int customerID;
            int pointID;
            int pointcount;
            int parent;
            int profile;
            string pointName;
            string customerName;


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

                    }   */                
                    
                }
                catch
                {

                }
            }
            
        }
    }
}
