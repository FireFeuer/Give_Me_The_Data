using Microsoft.Win32;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;


// код конфигурационного файла config.env
partial class Program
{

    static async Task Main(string[] args)
    {
        string path = Directory.GetCurrentDirectory();

        Console.WriteLine(path);
        string pathToEnvFile = Path.Combine(path, "config.env");
        string ip = "";
        int port = 0;
        string autoload = "";

        if (!File.Exists(pathToEnvFile))
        {
            await File.WriteAllTextAsync(pathToEnvFile, $"IP={GetLocalIPAddress()}\r\nPORT=3333");
        }
        using (var streamReader = new StreamReader(pathToEnvFile))
        {
            string fileContents = await streamReader.ReadToEndAsync();
            string[] lines = fileContents.Split('\n');

            foreach (string line in lines)
            {
                string[] keyValue = line.Split('=');
                if (keyValue.Length == 2)
                {
                    string key_env = keyValue[0].Trim();
                    string value = keyValue[1].Trim();

                    switch (key_env)
                    {
                        case "IP":
                            ip = value;
                            break;
                        case "PORT":
                            port = int.Parse(value);
                            break;
                        case "AUTOLOAD":
                            autoload = value;
                            break;
                    }
                }
            }

            if (autoload == "TRUE")
            {
                const string applicationName = "PCInfo";
                const string pathRegistryKeyStartup =
                            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

                using (RegistryKey registryKeyStartup = Registry.CurrentUser.OpenSubKey(pathRegistryKeyStartup, true))
                {
                    registryKeyStartup.SetValue(
                        applicationName,
                        string.Format("\"{0}\"", System.Reflection.Assembly.GetExecutingAssembly().Location.Replace("dll", "exe")));
                }
            }

            IPAddress ipAddress = IPAddress.Parse(ip);
            TcpListener server = new TcpListener(ipAddress, port);
            server.Start();

            Console.WriteLine($"" +
                $"Сервер запущен\n" +
                $"Ip - {ip}\n" +
                $"Порт - {port}");

            while (true)
            {
                TcpClient client = await server.AcceptTcpClientAsync(); // Принимаем подключение от клиента

                // Читаем данные от клиента
                StreamReader reader = new StreamReader(client.GetStream());
                string data = "";
                try
                {
                    data = await reader.ReadToEndAsync();
                }
                catch
                {
                    Console.WriteLine("Поизощла ошибка на стороне отправителя");
                }



                // Создаем настройки для десириализации
                var options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),
                    WriteIndented = true
                };


                data = data.Remove(data.LastIndexOf(","), 1);
                Console.WriteLine(data);


                byte[] byteArray = Encoding.UTF8.GetBytes(data);
                MemoryStream stream = new MemoryStream(byteArray);

                // Десиарилизируем полученную строку в словарь 
                List<Dictionary<string, string>> dictitionariesList = await JsonSerializer.DeserializeAsync<List<Dictionary<string, string>>>(stream, options);
                client.Close();


                foreach (Dictionary<string, string> dict in dictitionariesList)
                {
                    string fullInfo = $"" +
                       $"{dict["name"]}\n" +
                       $"{dict["driveInfo"]}\n" +
                       $"Операционная система - {dict["os"]}\n" +
                       $"{dict["IsWindowsUpdateNeeded"]}\n\n" +
                       $"Список установленных программ\n" +
                       $"{dict["programNames"]}\n\n" +
                       $"Список ошибок системы\n" +
                       $"{dict["eventsSystem"]}\n" +
                       $"Список ошибок приложений\n" +
                       $"{dict["eventsApplication"]}\n";

                    Console.WriteLine(fullInfo);

                    // Сохраняем данные в текстовый файл
                    string fileName = $"{dict["name"]} инфо - {dict["time"]}.txt";

                    string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    folderPath = Path.Combine(folderPath, "PCInfo");
                    folderPath = Path.Combine(folderPath, dict["name"]);
                    string fullpath = Path.Combine(folderPath, fileName);

                    DirectoryInfo dirInfo = new DirectoryInfo(folderPath);

                    if (!dirInfo.Exists)
                    {
                        dirInfo.Create();
                    }

                    await File.WriteAllTextAsync(fullpath, fullInfo);
                }
            }
        }
    }

    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }
}


