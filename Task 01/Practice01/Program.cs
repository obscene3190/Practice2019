using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using EAGetMail; // add EAGetMail namespace
using System.Reflection;
using System.Security.Permissions;

public static class Resolver
{
    private static volatile bool _loaded;

    public static void RegisterDependencyResolver()
    {
        if (!_loaded)
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnResolve;
            _loaded = true;
        }
    }

    private static Assembly OnResolve(object sender, ResolveEventArgs args)
    {
        Assembly execAssembly = Assembly.GetExecutingAssembly();
        string resourceName = String.Format("{0}.{1}.dll",
            execAssembly.GetName().Name,
            new AssemblyName(args.Name).Name);

        using (var stream = execAssembly.GetManifestResourceStream(resourceName))
        {
            int read = 0, toRead = (int)stream.Length;
            byte[] data = new byte[toRead];

            do
            {
                int n = stream.Read(data, read, data.Length - read);
                toRead -= n;
                read += n;
            } while (toRead > 0);

            return Assembly.Load(data);
        }
    }
}

namespace receiveemail
{
    class Program
    {

        static Program()
        {
            Resolver.RegisterDependencyResolver();
        }

        static void Main(string[] args)
        {
            try
            {
                String email, pass;
                Console.WriteLine("Введите логин:");
                email = Console.ReadLine();
                Console.WriteLine("Введите пароль:");
                pass = Console.ReadLine();

                // mail.ru IMAP4 server is "imap.mail.ru"
                MailServer oServer = new MailServer("imap.mail.ru",
                        email, pass, ServerProtocol.Imap4);
                MailClient oClient = new MailClient("TryIt");

                // Set SSL connection
                oServer.SSLConnection = true;
                oServer.Port = 993;

                oClient.Connect(oServer);
                MailInfo[] infos = oClient.GetMailInfos();
                Console.WriteLine("Подключение выполнено успешно. Всего писем: {0}\r\n", infos[infos.Length - 1].Index);
                for (int i = 0; i < infos.Length; i++)
                {
                    MailInfo info = infos[i];
                    Console.WriteLine("№: {0}; Размер: {1}; UIDL: {2}",
                        info.Index, info.Size, info.UIDL);

                    // Download email
                    Mail oMail = oClient.GetMail(info);

                    Console.WriteLine("От: {0}", oMail.From.ToString());
                    Console.WriteLine("Тема: {0}\r\n", oMail.Subject);
                }
                oClient.Quit();
                Console.ReadLine();
            }
            catch (Exception ep)
            {
                Console.WriteLine(ep.Message);
                Console.ReadLine();
            }
        }
    }
}
