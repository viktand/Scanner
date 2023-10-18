using ServiceStack.Messaging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DriverScanner
{
    /// <summary>
    /// Управление кернел-чипом
    /// </summary>
    public static class Kernel
    {
        /// <summary>
        /// Настроить пин 1 разъема Кернел-чипа на чтение состояния внешней кнопки
        /// 1 - нет нажатия, 0 - есть нажате
        /// </summary>
        /// <returns></returns>
        public static bool ButtonIni()
        {
            var com = ConfigurationManager.AppSettings["comport"];
            try
            {
                SerialPort port;
                port = new SerialPort
                {
                    PortName = com
                };
                port.Open();
                var send = "$KE,IO,SET,1,1,S\r\n";
                Logger.Log(send);
                port.Write(send);
                var result = port.ReadLine();
                Logger.Log(result);
                port.Close();
                return result.Contains("OK");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Проверка состояния кнопки 
        /// false - отпущена, true - нажата
        /// </summary>
        /// <returns></returns>
        public static bool ButtonCheck()
        {
            var com = ConfigurationManager.AppSettings["comport"];
            try
            {
                SerialPort port;
                port = new SerialPort
                {
                    PortName = com
                };
                port.Open();
                var send = "$KE,RD,1\r\n";
                //Logger.Log(send);
                port.Write(send);
                var result = port.ReadLine();
                //Logger.Log(result);
                port.Close();
                return result.Contains("#RD,01,0");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Включить датчик бумаги
        /// </summary>
        /// <returns></returns>
        public static bool PaperSwitch(bool state, out string message)
        {
            var com = ConfigurationManager.AppSettings["comport"];
            message = "";
            try
            {
                SerialPort port;
                port = new SerialPort
                {
                    PortName = com
                };
                port.Open();
                port.Write($"$KE,REL,3,{(state ? "1" : "0")}\r\n");
                message = port.ReadLine();
                port.Close();
                return true;
            }catch
            {
                return false;
            }
        }

        /// <summary>
        /// Нажать кнопку питания
        /// </summary>
        /// <returns></returns>
        public static string PowerSwitch()
        {
            var com = ConfigurationManager.AppSettings["comport"];
            try
            {
                SerialPort port;
                port = new SerialPort
                {
                    PortName = com
                };
                port.Open();
                port.Write($"$KE,REL,1,0\r\n");
                Thread.Sleep(500);
                port.Write($"$KE,REL,1,1\r\n");
                port.Close();
                return "";
            }
            catch(Exception e) 
            {
                return e.Message;
            }
        }

        /// <summary>
        /// Нажать кнопку сканирования
        /// </summary>
        /// <returns></returns>
        public static string ScannSwitch()
        {
            var com = ConfigurationManager.AppSettings["comport"];
            try
            {
                SerialPort port;
                port = new SerialPort
                {
                    PortName = com
                };
                port.Open();
                port.Write($"$KE,REL,2,0\r\n");
                Thread.Sleep(500);
                port.Write($"$KE,REL,2,1\r\n");
                port.Close();
                return "";
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }
}
