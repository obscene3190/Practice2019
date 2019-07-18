using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;

namespace _test_email_console
{
    class Program
    {
        static System.Net.Sockets.TcpClient tcpc = null;
        static System.Net.Security.SslStream ssl = null;
        static string username, password;
        static int bytes = -1;
        static byte[] buffer;
        static StringBuilder sb = new StringBuilder();
        static byte[] dummy;
        static void Main(string[] args)
        {
            try
            {

                // there should be no gap between the imap command and the \r\n       
                // ssl.read() -- while ssl.readbyte!= eof does not work because there is no eof from server 
                // cannot check for \r\n because in case of larger response from server ex:read email message 
                // there are lot of lines so \r \n appears at the end of each line 
                //ssl.timeout sets the underlying tcp connections timeout if the read or write 
                //time out exceeds then the undelying connection is closed 
                tcpc = new System.Net.Sockets.TcpClient("imap.mail.ru", 993);

                ssl = new System.Net.Security.SslStream(tcpc.GetStream());
                ssl.AuthenticateAsClient("imap.mail.ru");
                receiveResponse("");
                
                Console.WriteLine("username : ");
                username = Console.ReadLine();

                Console.WriteLine("password : ");
                password = Console.ReadLine();
                
                receiveResponse("$ LOGIN " + username + " " + password + "\r\n");
                
                // Getting amount of Inbox
                String inbox = receiveResponse("$ SELECT INBOX\r\n");
                string[] words = inbox.Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
                words[1] = words[1].Trim(new char[] {' ', 'E'}); // " xx EXISTS"
                string subString = "EXISTS";
                int indexOfSubstring = words[1].IndexOf(subString);
                words[1] = words[1].Substring(0, indexOfSubstring-1); // "xx"
                int amount = Convert.ToInt32(words[1]);
                Console.WriteLine("Всего писем: " + amount + "\r\n");
                for(int i = 0; i < amount; i++)
                {
                    Console.WriteLine("Письмо №{0}" + ":", i+1);
                    GetMail(i);
                }
                //receiveResponse("$ LOGOUT\r\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("error: " + ex.Message);
            }
            finally
            {
                if (ssl != null)
                {
                    ssl.Close();
                    ssl.Dispose();
                }
                if (tcpc != null)
                {
                    tcpc.Close();
                }
            }


            Console.ReadKey();
        }
        static string receiveResponse(string command)
        {
            try
            {
                //Console.WriteLine("<<<" + command);
                if (command != "")
                {
                    if (tcpc.Connected)
                    {
                        dummy = Encoding.UTF8.GetBytes(command);
                        ssl.Write(dummy, 0, dummy.Length);
                    }
                    else
                    {
                        throw new ApplicationException("TCP CONNECTION DISCONNECTED");
                    }
                }

               ssl.Flush();

                buffer = new byte[2048];
                bytes = ssl.Read(buffer, 0, 2048);

                // Decode to utf8
                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                StringBuilder messageData = new StringBuilder();
                messageData.Append(chars);

                //Console.WriteLine(messageData.ToString());
                //sb.Append(Encoding.ASCII.GetString(buffer), 0, bytes - 1);
                //Console.WriteLine(">>>" + sb.ToString());
                //sb = new StringBuilder();
                //}
                return messageData.ToString();

            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }
        }
        
        static void GetMail(int number)
        {
            // Парсим

            // DATE
            String Date = receiveResponse("$ fetch " + number + " body[header.fields(date)]\r\n"); ;
            int StartIndexDate = Date.IndexOf("Date: ");
            int EndIndexDate = Date.IndexOf("$");
            Date = Date.Substring(StartIndexDate + 6, EndIndexDate - StartIndexDate - 7 - 6); //cut off the SUBJECT
            Console.WriteLine("Дата: " + Date);

            //FROM
            String From = receiveResponse("$ fetch " + number + " body[header.fields(from)]\r\n"); ;
            int StartIndexFrom = From.IndexOf("From: ");
            int EndIndexFrom = From.IndexOf("$");
            From = From.Substring(StartIndexFrom + 6, EndIndexFrom - StartIndexFrom - 7 - 6); //cut off the SUBJECT
            Console.WriteLine("От: " + From);

            // SUBJECT ???????????????????????????
            String Subject = receiveResponse("$ fetch " + number + " body[header.fields(subject)]\r\n"); ;
            int StartIndexSubject = Subject.IndexOf("=?");
            int EndIndexSubject = Subject.IndexOf("$");
            Subject = Subject.Substring(StartIndexSubject, EndIndexSubject - StartIndexSubject - 7); //cut off the SUBJECT
            Console.WriteLine("Тема: " + Subject + "\r\n");
            /*Console.WriteLine("Получаем: " + Subject);
            Subject = Subject.Replace("=?UTF-8?B?", "");
            Subject = Subject.Replace("=?UTF-8?Q?", "");
            Subject = Subject.Replace("=?utf-8?b?", "");
            Subject = Subject.Replace("=?utf-8?q?", "");
            Subject = Subject.Replace(" ", "");
            Subject = Subject.Replace("\r\n", "");
            Subject = Subject.Replace("?=", "");
            Subject = Subject + "==";
            Subject = Subject.Replace("/=", "");
            Subject = Subject.Replace("===", "==");
            Subject = Subject.Replace("==", "=");
            Console.WriteLine("Преобразовали: " + Subject);
            Subject = Subject.Replace("=", "=*");
            string[] SubjectParted = Subject.Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
            String SubjectEn = "";
            for(int i = 0; i < SubjectParted.Length; i++)
            {
                Console.WriteLine(SubjectParted[i]);
                //SubjectParted[i] = SubjectParted[i] + "==";
                //SubjectParted[i] = Subject.Replace("===", "==");
                SubjectEn = SubjectEn + Encoding.UTF8.GetString(Convert.FromBase64String(SubjectParted[i]));
                Console.WriteLine(SubjectEn);
            }
            Console.WriteLine("Тема: " + SubjectEn + "\r\n");
            */
        }
    }
}
