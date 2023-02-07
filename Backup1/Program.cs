using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using System.Xml;
using System.Globalization;
using System.Security.Permissions;
using System.Security;
using Common;

namespace Import
{
    public class EErrorFormat : Exception
    {
        public EErrorFormat() : base("Неверный формат")
        {
        }
    }

    class App
    {
        static public String ConnectionString;
        static public String BaseDirectory;
        static public SqlConnection Connection;
        static public int CommandTimeout = 0;

        // "0" - означает что файл передан как первый аргумент
        [AutoSetMember("0", "file")]
        public static String fileName = "";

        [AutoSetMember("import")]
        public static string importType = "";

        [AutoSetMember("lock")]
        public static bool isDontUpdate = false;

        [AutoSetMember("path")]
        public static string path = @"..\";

        static bool LogError = false;
        static public void SimpleLogMessage(string Text)
        {
            try
            {
                string file = "log.txt";
                StreamWriter log = new StreamWriter(file, true, System.Text.Encoding.GetEncoding(1251));
                try
                {
                    log.WriteLine(Text);
                }
                finally
                {
                    log.Close();
                }
            }
            catch (Exception e)
            {
                if (!LogError)
                {
                    LogError = true;
                    //MessageBox.Show(e.Message);
                }
            }
        }
        static public void LogMessage(string Text)
        {
            SimpleLogMessage("--------------------------------------------------------------------------");
            SimpleLogMessage("@@" + DateTime.Now.ToString() + "\n");
            if (Text.Length > 0)
                SimpleLogMessage(Text);
            SimpleLogMessage("\n\n");
        }
        static public void LogMessage(System.Exception e)
        {
            LogMessage(e.ToString());
        }
        static public void LogMessage(string Text, System.Exception e)
        {
            LogMessage(Text + "\n\n" + e.ToString());
        }
        public static int ExecuteNonQuery(String CommandText)
        {
            SqlCommand command = new SqlCommand();
            command.Connection = Connection;
            command.CommandText = CommandText;
            command.CommandTimeout = CommandTimeout;
            return command.ExecuteNonQuery();
        }
        public static int ExecuteNonQuery(String CommandText, params object[] args)
        {
            return ExecuteNonQuery(String.Format(CommandText, args));
        }
        public static object ExecuteScalar(String CommandText)
        {
            try
            {
                SqlCommand command = new SqlCommand();
                command.Connection = Connection;
                command.CommandText = CommandText;
                return command.ExecuteScalar();
            }
            catch (Exception e)
            {
                App.LogMessage(e);
            }
            return null;
        }
        public static object ExecuteScalar(String CommandText, params object[] args)
        {
            return ExecuteScalar(String.Format(CommandText, args));
        }
        public static int ExecuteInt32(String CommandText, params object[] args)
        {
            object result = ExecuteScalar(String.Format(CommandText, args));
            if (result == null)
                return 0;
            else
            {
                try
                {
                    if (result.GetType().Equals(typeof(Decimal)))
                    {
                        return Decimal.ToInt32((Decimal)result);
                    }
                    else
                        return (int)result;
                }
                catch
                {
                    return 0;
                }
            }
        }

        public static int getInvert(int pointID, int sourcetypeID)
        {
            return App.ExecuteInt32("select invert from pointcode where point = {0} and sourcetype = {1}", pointID, sourcetypeID);
        }
        public static int getChannel(string channelAbbr, int invert)
        {
            int channelID = 0;
            if (invert == 1)
            {
                channelID = (int)App.ExecuteInt32(
                    "declare @id int\n" +
                    "set @id = 0\n" +
                    "select @id = invert_id from channel \n" +
                    "where abbr = '{0}'\n" +
                    "select @id \n",
                    channelAbbr);
            }
            else
            {
                channelID = (int)App.ExecuteInt32(
                    "declare @id int\n" +
                    "set @id = 0\n" +
                    "select @id = id from channel \n" +
                    "where abbr = '{0}'\n" +
                    "select @id \n",
                    channelAbbr);
            }
            if (channelID == 0) throw new Exception("Канал не найден \"" + channelAbbr + "\"");
            return channelID;
        }
        public static int getDataSource(int point, int channel, int sourcetype)
        {
            int datasource = (int)App.ExecuteInt32(
                "exec DATASOURCE_SELECT {0}, {1}, {2}", point, channel, sourcetype);
            if (datasource == 0) 
                throw new Exception(
                    String.Format("datasource (point={0}, channel={1}, sourcetype={2}) не найден", 
                        point, channel, sourcetype));
            return datasource;
        }
        public static int InsertParent(int CustomerID, string name)
        { 
            int pointID =  App.ExecuteInt32("select MAX(id) from point where id not in (600, 601, 800, 801, 802, 803, 804)"); 
            pointID++;
            if (pointID == 600)
                pointID = 602;
            ExecuteNonQuery("insert into point (id,name,number) values ({0},'{1}',{2})", pointID, name, CustomerID);
            ExecuteNonQuery("insert into pointCustomer (point, CustomerID, gtp, profile) values ({0},{1},1,1)", pointID, CustomerID);
            ExecuteNonQuery("insert into datasource (point, channel, sourcetype ) values ({0},1,7)", pointID);
            ExecuteNonQuery("insert into datasource (point, channel, sourcetype ) values ({0},1,13)", pointID);

            return pointID;
 
        }
        public static int InsertPoint(string factory, string name, int parent)
        {
            
            int pointID = App.ExecuteInt32("select MAX(id) from point where id not in (600, 601, 800, 801, 802, 803, 804)");
            pointID++;
            if (pointID == 600)
                pointID = 602;            
            ExecuteNonQuery("insert into point (id,name) values ({0},'{1}')", pointID, name);
            ExecuteNonQuery("insert into datasource (point, channel, sourcetype ) values ({0},1,10)", pointID);
            ExecuteNonQuery("insert into datasource (point, channel, sourcetype ) values ({0},2,10)", pointID);
            ExecuteNonQuery("insert into datasource (point, channel, sourcetype ) values ({0},3,10)", pointID);
            ExecuteNonQuery("insert into datasource (point, channel, sourcetype ) values ({0},4,10)", pointID);
            ExecuteNonQuery("insert into datasource (point, channel, sourcetype ) values ({0},1,7)", pointID);
            ExecuteNonQuery("insert into datasource (point, channel, sourcetype ) values ({0},2,7)", pointID);
            ExecuteNonQuery("insert into datasource (point, channel, sourcetype ) values ({0},3,7)", pointID);
            ExecuteNonQuery("insert into datasource (point, channel, sourcetype ) values ({0},4,7)", pointID);
            ExecuteNonQuery("INSERT INTO pointcode ([point],[code],[name],[factory],[sourcetype],[koeftrans],[invert]"+
           ",[timezone],[daylightsavings]) VALUES ({0},'{1}','{2}','{3}',10,1,0,3,1)", pointID, factory,name,factory);
            ExecuteNonQuery("insert into pointparent (point, parent) values ({0},{1})", pointID, parent);

            return pointID;

        }

        public static int InsertPointH(string factory, string name, int parent)
        {

            int pointID = App.ExecuteInt32("select MAX(id) from point");
            pointID++;
            
            ExecuteNonQuery("insert into point (id,name) values ({0},'{1}')", pointID, name);
            ExecuteNonQuery("insert into datasource (point, channel, sourcetype ) values ({0},1,10)", pointID);
            ExecuteNonQuery("insert into datasource (point, channel, sourcetype ) values ({0},2,10)", pointID);
            ExecuteNonQuery("insert into datasource (point, channel, sourcetype ) values ({0},3,10)", pointID);
            ExecuteNonQuery("insert into datasource (point, channel, sourcetype ) values ({0},4,10)", pointID);
            ExecuteNonQuery("insert into datasource (point, channel, sourcetype ) values ({0},1,7)", pointID);
            ExecuteNonQuery("insert into datasource (point, channel, sourcetype ) values ({0},2,7)", pointID);
            ExecuteNonQuery("insert into datasource (point, channel, sourcetype ) values ({0},3,7)", pointID);
            ExecuteNonQuery("insert into datasource (point, channel, sourcetype ) values ({0},4,7)", pointID);
            ExecuteNonQuery("INSERT INTO pointcode ([point],[code],[name],[factory],[sourcetype],[koeftrans],[invert]" +
           ",[timezone],[daylightsavings]) VALUES ({0},'{1}','{2}','{3}',10,1,0,3,1)", pointID, factory, name, factory);
            ExecuteNonQuery("insert into pointparent (point, parent) values ({0},{1})", pointID, parent);

            return pointID;

        }

        public static void InsertPeriod(int datasource, DateTime begin, DateTime end, float value)
        {
            InsertPeriod(datasource, begin, end, value, null);
        }
        public static void InsertPeriod(int datasource, DateTime begin, DateTime end, float value, float? indication)
        {
            NumberFormatInfo provider = new NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";

            string strIndication = indication == null ? "NULL" : ((float)indication).ToString(provider);
            if (App.importType == "recalc")
            {
                ExecuteNonQuery("exec INSERT_PERIOD0 {0}, '{1}', '{2}', {3}, {4}",
                    datasource,
                    begin.ToString("yyyyMMdd HH:mm:ss"),
                    end.ToString("yyyyMMdd HH:mm:ss"),
                    value.ToString(provider),
                    strIndication);
            }
                /*ExecuteNonQuery("exec INSERT_PERIOD0 {0}, '{1}', '{2}', {3}, {4}",
                    datasource,
                    begin.ToString("yyyyMMdd HH:mm:ss"),
                    end.ToString("yyyyMMdd HH:mm:ss"),
                    value.ToString(provider),
                    strIndication);*/
            else if (App.importType == "sum")
                ExecuteNonQuery("exec INSERT_PERIOD2 {0}, '{1}', '{2}', {3}, {4}",
                    datasource,
                    begin.ToString("yyyyMMdd HH:mm:ss"),
                    end.ToString("yyyyMMdd HH:mm:ss"),
                    value.ToString(provider),
                    strIndication);
            else
                ExecuteNonQuery("exec INSERT_PERIOD1 {0}, '{1}', '{2}', {3}, {4}",
                    datasource,
                    begin.ToString("yyyyMMdd HH:mm:ss"),
                    end.ToString("yyyyMMdd HH:mm:ss"),
                    value.ToString(provider),
                    strIndication);
        }
        public static void BeginUpdate(int datasource, DateTime begin, DateTime end)
        {
            App.ExecuteNonQuery("exec [UPDATE_BEGIN] {0}, '{1}', '{2}'",
                datasource,
                begin.ToString("yyyyMMdd HH:mm:ss"),
                end.ToString("yyyyMMdd HH:mm:ss"));
        }
        public static void InsertFobos(DateTime date, int Hour,int Cloud,int Precip,int MaxP,int MinP,int MaxT,int MinT,int MinW,int MaxW,int RumbW,int MaxRW,int MinRW,int MaxHT,int MinHT, DateTime date3, int hour3)
        {
            App.ExecuteNonQuery("exec [INSERT_FOBOS] '{0}',{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},'{15}',{16}",
                date.ToString("yyyyMMdd"), Hour,Cloud,Precip,MaxP,MinP,MaxT,MinT,
                MinW, MaxW, RumbW, MaxRW, MinRW, MaxHT, MinHT, date3.ToString("yyyyMMdd"), hour3);
        }
        public static void Insert80021(int point, int status, int number, string filename, DateTime date)
        {
            App.ExecuteNonQuery("EXEC [dbo].[INSERT_80021] {0},{1},{2},'{3}','{4}'",
                point, status, number, filename, date.ToString("yyyyMMdd"));
        }
        public static void UpdateBSA(int status, int number)
        {
                      
                App.ExecuteNonQuery("UPDATE BSAstatus SET [status] = {0} WHERE [number] = {1}",
                    status, number);
            
           
        }
        public static void EndUpdate()
        {
            if (!isDontUpdate)
                App.ExecuteNonQuery("exec [UPDATE_END]");
        }
        
        public static int GetPeriod(DateTime begin, DateTime end)
        {
            return ExecuteInt32("select id from period where [begin] = '{0}' and [end] = '{1}'", begin.ToString("yyyyMMdd HH:mm:ss"), end.ToString("yyyyMMdd HH:mm:ss"));
        }
        
        public static int GetData(int datasource, int period)
        {
            return ExecuteInt32("select id from data  where datasource = {0} and period = {1}", datasource, period);
        }

        static App()
        {
            String configFile = "";
            try
            {
                PermissionSet ps1 = new PermissionSet(PermissionState.None);
                ps1.AddPermission(new SqlClientPermission(PermissionState.Unrestricted));
                ps1.Demand();
                App.BaseDirectory = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                configFile = System.IO.Path.Combine(App.BaseDirectory, "config.xml");

                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                doc.Load(configFile);
                System.Xml.XmlNode node = doc.SelectSingleNode("//connection");
                if (node != null)
                    App.ConnectionString = node.InnerText;
                Connection = new SqlConnection(App.ConnectionString);
                Connection.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                App.LogMessage(e);
            }
        }

        static int Main(string[] args)
        {
            try
            {
                ArgumentParser parser = new ArgumentParser(ArgumentFormats.NamedValue, true);
                StringDictionary theArgs = parser.Parse(args);

                parser.AutoSetMembers(typeof(App));

                
                if (App.Connection.State != System.Data.ConnectionState.Open)
                {
                    Console.WriteLine("Connection don't open!");
                    return 2;
                }

                if (importType == "commit")
                {
                    EndUpdate();
                    return 0;
                }

                string FileName = App.fileName;

                string ext = System.IO.Path.GetExtension(FileName).ToLower();

                if (ext == ".xml")
                {
                    System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                    doc.Load(FileName);

                    string code = "";

                    XmlNode node = doc.SelectSingleNode("./package");
                    if (node != null)
                        code = node.Attributes["class"].Value;
                    else
                    {
                        node = doc.SelectSingleNode("./message");
                        if (node != null)
                            code = node.Attributes["class"].Value;
                        else
                        {
                            node = doc.SelectSingleNode("./data");
                            if (node != null)
                                code = "COM";
                            else
                            {
                                node = doc.SelectSingleNode("./DEVICE");
                                if (node != null)
                                    code = "XML";
                                else
                                {
                                    node = doc.SelectSingleNode("./receipt");
                                    if (node != null)
                                        code = "BSA";                                    
                                }
                            }

                        }
                    }

                    if (code == "")
                    {
                        throw new Exception("Неизвестный формат");
                    }

                    if (code == "80020" || code == "80040")
                        F80020.Parse(doc);
                    else if (code == "80021")
                        F80021.Parse(doc);
                    else if (code == "51070")
                    {
                        try
                        {
                            F51070.Parse(doc);
                        }
                        catch (EErrorFormat)
                        {
                            E51070.Parse(doc);
                        }
                    }
                    else if (code == "EPQS")
                        EPQS.Parse(doc);
                    else if (code == "COM")
                        FCOM.Parse(doc);
                    else if (code == "STEB")
                        FCOM.Parse(doc);
                    else if (code == "C-01" || code == "Mer-230")
                        C_01.Parse(doc);
                    else if (code == "XML")
                        XML.Parse(doc);
                    else if (code == "BSA")
                        BSA.Parse(doc);
                    else
                        throw new Exception("Неизвестный формат: '" + code + "'");
                }
                else if (ext == "")
                {
                    if(System.IO.Directory.Exists(FileName))
                    {
                        RD.Parse(FileName);
                    }
                }
                else if (ext == ".xls")
                {
                    try
                    {
                        //Excel.Parse(FileName);
                        PlanExcel.Parse(FileName);

                    }
                    catch (EErrorFormat)
                    {
                        try
                        {
                            ExcelMir.Parse(FileName);

                            //Excel2.Parse(FileName);
                        }
                        catch (EErrorFormat)
                        {

                            try
                            {
                                //Correct.Parse(FileName);
                                Buildings.Parse(FileName);
                            }
                            catch (EErrorFormat)
                            {
                                try
                                {
                                    //Arena.Parse(FileName);
                                    //ExcelEPQS.Parse(FileName);
                                    ExcelPoint.Parse(FileName);
                                }
                                catch (EErrorFormat)
                                {
                                    try
                                    {
                                        calcfacthour.Parse(FileName);
                                    }
                                    catch (EErrorFormat)
                                    {
                                        Excel.Parse(FileName);
                                    }
                                }
                                
                            }
                            
                        }

                    }
                }
                else if (ext == ".txt")
                {

                    using (StreamReader sr = File.OpenText(FileName))
                    {
                        string buf = sr.ReadLine();
                        if (buf == "<pre>")
                            Fobos.Parse(FileName);
                        else
                            Mail.Parse(FileName);
                    }
                                                            
                }
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return 1;
                
            }
        }
    }
}
