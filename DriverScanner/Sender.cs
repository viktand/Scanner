using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DriverScanner
{
    /// <summary>
    /// Класс для отправки сканов куда-то туда
    /// </summary>
    public class Sender
    {
        private Queue<string> _que;
        private Timer _timer;
        private string _code;
        private string _pass;
        private string _url;

        public Sender()
        {
            _que = new Queue<string>();
            _code = ConfigurationManager.AppSettings["TerminalCode"];
            _pass = ConfigurationManager.AppSettings["TerminalPassword"];
            _url = ConfigurationManager.AppSettings["FileUrl"];
            _timer = new Timer(1000);
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = false;
            _timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(_que.Count == 0)
            {
                _timer.Start();
                return;
            }
            Send(_que.Dequeue());
        }

        private async void Send(string file)
        {
            try
            {
                if(! await SendData(file))
                {
                    _que.Enqueue(file);
                    return;
                }
            }
            catch
            {
                _que.Enqueue(file);
            }
            finally
            {
                _timer.Start();
            }
        }

        private Task<bool> SendData(string file)
        {
            return Task.Run(() =>
            {
                Logger.Log("Отправка скана документа куда-то на сервер");
                var url = "";
                var json = "";
                try
                {
                    var dto = new ScanData
                    {
                        files = new []
                        {
                            ToBase64(file)
                        }
                    };                   
                    var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
                    var token = ToSHA256($"{_code}/{timestamp}/{_pass}");
                    url = _url + $"?token={token}&timestamp={timestamp}&terminal-code={_code}";
                    json = JsonConvert.SerializeObject(dto);
                    var result = url.PostJsonToUrl(json);
                    Logger.Log($"Ответ удаленного сервера: {result}");
                    return true;
                }
                catch (Exception e)
                {
                    Logger.Error("ошибка при попытке отправить файл сканирования документа");
                    Logger.Error(e.Message);
                    Logger.Log("Отправка в: " + url);
                    Logger.Log("dto: " + json);
                    return false;
                }
            });
        }

        private static string ToSHA256(string v)
        {
            var crypt = new SHA256Managed();
            string hash = string.Empty;
            byte[] crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(v));
            foreach (byte theByte in crypto)
            {
                hash += theByte.ToString("x2");
            }
            return hash;
        }

        /// <summary>
        /// поставить скрин в очередь на отправку
        /// </summary>
        /// <param name="bmp">скрин</param>
        /// <param name="index">индекс пары</param>
        public void AddTask(string file)
        {
            _que.Enqueue(file);
        }

        private string ToBase64(string file)
        {
            try
            {
                var fs = new FileStream(file, FileMode.Open).ToBytes();
                var SigBase64 = Convert.ToBase64String(fs);
                return SigBase64;
            }
            catch(Exception e)
            {
                Logger.Error($"Ошибка при кодировании в base64: {e.Message}");
                return null;
            }
        }
    }

    public class SenderObject
    {
        public int Index { get; set; }
        public Bitmap Scan { get; set; }
    }

    public class ScanData
    {
        public string[] files { get; set; }
    }
}
