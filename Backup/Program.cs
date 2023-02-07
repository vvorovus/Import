using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using System.Xml;
using System.Globalization;

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

        public static void InsertPeriod(int datasource, DateTime begin, DateTime end, float value)
        {
            NumberFormatInfo provider = new NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";

            ExecuteNonQuery("exec INSERT_PERIOD0 {0}, '{1}', '{2}', {3}",
                datasource,
                begin.ToString("yyyyMMdd HH:mm:ss"),
                end.ToString("yyyyMMdd HH:mm:ss"),
                value.ToString(provider));
        }
        public static void BeginUpdate(int datasource, DateTime begin, DateTime end)
        {
            App.ExecuteNonQuery("exec [UPDATE_BEGIN] {0}, '{1}', '{2}'",
                datasource,
                begin.ToString("yyyyMMdd HH:mm:ss"),
                end.ToString("yyyyMMdd HH:mm:ss"));
        }
        public static void EndUpdate()
        {
            App.ExecuteNonQuery("exec [UPDATE_END]");
        }

        static App()
        {
            try
            {
                App.BaseDirectory = "";
                    //System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\";

                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                doc.Load("config.xml");
                System.Xml.XmlNode node = doc.SelectSingleNode("//connection");
                if (node != null)
                    App.ConnectionString = node.InnerText;
                Connection = new SqlConnection(App.ConnectionString);
                Connection.Open();
            }
            catch (Exception e)
            {
                App.LogMessage(e);
            }
        }

        static int Main(string[] args)
        {
            try
            {
                if (App.Connection.State != System.Data.ConnectionState.Open)
                {
                    Console.WriteLine("Connection don't open!");
                    return 2;
                }

                string FileName = args[0];
                bool delFile = false;
                if (args.Length > 1)
                {
                    string delFlag = args[1];
                    if (delFlag.ToUpper().Trim() == "/D") delFile = true;
                }

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
                        }
                    }

                    if (code == "")
                        throw new Exception("Неизвестный формат");

                    if (code == "80020")
                        F80020.Parse(doc);
                    else if (code == "51070")
                        F51070.Parse(doc);
                    else if (code == "EPQS")
                        EPQS.Parse(doc);
                    else if (code == "COM")
                        FCOM.Parse(doc);
                    else if (code == "C-01" || code=="Mer-230")
                        C_01.Parse(doc);
                    else
                        throw new Exception("Неизвестный формат: '" + code + "'");
                }
                else if (ext == ".xls")
                {
                    try
                    {
                        Excel.Parse(FileName);
                    }
                    catch (EErrorFormat)
                    {
                        ExcelMir.Parse(FileName);

                        //Excel2.Parse(FileName);
                    }
                }

                if (delFile) System.IO.File.Delete(FileName);
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
