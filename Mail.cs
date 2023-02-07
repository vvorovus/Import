using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Import
{
    class Mail
    {
      
        public static void Parse(String FileName)
        {
            string buf;
            string text="";
            int BSAstatus = -1;
            int current_number = 0;
            int current_status = -1;

            Regex number = new Regex(@"Номер ценовой заявки - (?<number>\d+)");
            Regex buy= new Regex(@"Тип ценовой заявки - На покупку;");
            Regex negative = new Regex(@"заявка номер (?<number>\d+) отклонена");
            Regex positive = new Regex(@"Ваша ценовая заявка включена");


            Regex date = new Regex(@"Срок действия - (?<date>\d{2}\.\d{2}\.\d{4})");
            Regex point = new Regex(@"индивидуальный идентификационный код - PABAKSB\[0-9];");
            

            using (StreamReader sr = new StreamReader(FileName, System.Text.Encoding.GetEncoding(1251)))//File.OpenText(FileName))
            {
                
                while ((buf = sr.ReadLine()) != null)
                {
                    Match m = number.Match(buf);
                    if (m.Success)
                    {
                        current_number = C.Int(m.Groups["number"].Value);
                        current_status = App.ExecuteInt32("SELECT [status] FROM BSAstatus WHERE [number]={0}", current_number);
                    }
                    m = buy.Match(buf);
                    if (m.Success && (current_status == -1))
                    {
                        
                        current_status = BSAstatus = 1;
                        App.UpdateBSA(BSAstatus, current_number);

                        
                    }
                    
                    m = negative.Match(buf);
                    if (m.Success)
                    { 
                        current_number = C.Int(m.Groups["number"].Value);
                        BSAstatus = 2;
                        App.UpdateBSA(BSAstatus, current_number);
                    }
                    
                    m = positive.Match(buf);
                    if (m.Success && (current_status == 1))
                    {
                        BSAstatus = 0;
                        App.UpdateBSA(BSAstatus, current_number);
                    }
                                       
                }

                /*
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
                */
            }
        
        }

    }
}
