using System;
using System.IO;
using System.Reflection;

namespace FacebookToRSS
{
    class Logger
    {
        private static string _path;

        public static void LogMessage(string message, bool timestamp = true)
        {
            if (_path == null)
            {
                _path = Path.GetFullPath("log.txt");
                Console.WriteLine($"Create log file: {_path}");
            }

            var fullMessage = timestamp
                ? DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss:fff ") + message + Environment.NewLine
                : message + Environment.NewLine;

            File.AppendAllText(_path, fullMessage);
            Console.Write(fullMessage);
        }
    }
}
