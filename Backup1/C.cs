using System;
using System.Collections;
using System.Text;
using System.Globalization;

public class C
{
    public static int Int(object value, int defValue)
    {
        if (value == null) return defValue;
        try
        {
            if (value is string)
                return int.Parse((string)value);
            if (value is decimal)
                return decimal.ToInt32((decimal)value);
            if (value.Equals(DBNull.Value))
                return defValue;
            if (value is bool)
                return (bool)value ? 1 : 0;
            return (int)value;
        }
        catch
        {
            return defValue;
        }
    }
    public static int Int(object value)
    {
        return Int(value, 0);
    }
    public static float Float(object value, float defValue)
    {
        if (value == null) return defValue;
        try
        {
            if (value is string)
            {
                string s = (string)value;
                s = s.Replace(',', '.');
                NumberFormatInfo NumberProvider = new NumberFormatInfo();
                NumberProvider.NumberDecimalSeparator = ".";

                return float.Parse((string)s, NumberProvider);
            }
            if (value is decimal)
                return (float)decimal.ToDouble((decimal)value);
            if (value.Equals(DBNull.Value))
                return defValue;
            if (value is bool)
                return (bool)value ? 1 : 0;
            if (value is double)
                return (float)value;
            if (value is int)
                return (int)value;
            return (float)value;
        }
        catch
        {
            return defValue;
        }
    }
    public static string Str(object value, string defValue)
    {
        if (value == null) return defValue;
        try
        {
            if (value.Equals(DBNull.Value))
                return defValue;
            else if (value is DateTime)
                return ((DateTime)value).ToShortDateString();
            else
                return value.ToString();
        }
        catch
        {
            return defValue;
        }
    }
    public static string Str(object value)
    {
        return Str(value, "");
    }
    public static DateTime Date(object value, DateTime defValue)
    {
        try
        {
            if (value == null) return defValue;
            else if (value is String)
            {
                if (value.Equals(""))
                    return defValue;
                else
                    return DateTime.Parse((string)value);
            }
            else
                return (DateTime)value;
        }
        catch
        {
            return defValue;
        }
    }
    public static DateTime Date(object value)
    {
        return Date(value, new DateTime(1, 1, 1));
    }
    public static bool Bool(object obj, bool defValue)
    {
        if (obj == null) return defValue;
        try
        {
            if (obj.Equals(DBNull.Value))
                return defValue;
            if (obj is int)
                return (int)obj == 0 ? false : true;

            return (bool)obj;
        }
        catch
        {
            return defValue;
        }
    }
    public static bool Bool(object obj)
    {
        return Bool(obj, false);
    }
}