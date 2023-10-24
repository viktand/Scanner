using DriverScanner.Model;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Configuration;
using System.IO.Ports;
using System.Threading;
using System.Windows;

namespace DriverScanner
{
    public class KernelConnectorWeb
    {
        const string Url = "http://localhost:5000/";

        /// <summary>
        /// Настроить пин 1 разъема Кернел-чипа на чтение состояния внешней кнопки
        /// 1 - нет нажатия, 0 - есть нажате
        /// </summary>
        /// <returns></returns>
        public bool ButtonIni()
        {
            try
            {
                var url = Url + "Buttoninit";
                var result = url.GetJsonFromUrl().FromJson<ResponseBool>();
                return result.value;
            }catch (Exception ex)
            {
                MessageBox.Show($"Не получилось запустить программу. Проверьте, что сервер kernelchip запущен. Ошибка: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Проверка состояния кнопки 
        /// false - отпущена, true - нажата
        /// </summary>
        /// <returns></returns>
        public bool ButtonCheck()
        {
            var url = Url + "CheckButton";
            var result = url.GetJsonFromUrl().FromJson<ResponseBool>();
            return result.value;
        }

        /// <summary>
        /// Включить датчик бумаги
        /// </summary>
        /// <returns></returns>
        public bool PaperSwitch(bool state, out string message)
        {
            message = "";
            var url = Url + $"Paper?state={state}";
            var result = url.GetJsonFromUrl();
            try
            {
                var rslt = JsonConvert.DeserializeObject<ResponseBool>(result);
                return rslt.value;
            }
            catch 
            {
                var rslt = JsonConvert.DeserializeObject<ResponseString>(result);
                Logger.Error(rslt.value);
                return false;
            }
        }

        /// <summary>
        /// Нажать кнопку питания
        /// </summary>
        /// <returns></returns>
        public string PowerSwitch()
        {
            var url = Url + "Power";
            var result = url.GetJsonFromUrl().FromJson<ResponseString>();
            return result.value;
        }

        /// <summary>
        /// Нажать кнопку сканирования
        /// </summary>
        /// <returns></returns>
        public string ScannSwitch()
        {
            return "";
        }
    }
}
