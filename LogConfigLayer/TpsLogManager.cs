using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using log4net.Config;
using log4net;

namespace LogConfigLayer
{

    public class LogsCollection
    {
        private static Dictionary<Int64, LogEntity> logEntityCollection = new Dictionary<Int64, LogEntity>();
        private static Int64 count = 0;

        public static string AddToDictionaryString { get; set; }

        public static int MaximumLogCount { get; set; }


        public static Dictionary<Int64, LogEntity> Logs
        {
            get
            {
                return logEntityCollection;
            }
        }

        internal static void Add(LogEntity log)
        {
            if (string.IsNullOrEmpty(AddToDictionaryString)) AddToDictionaryString = "";
            if (AddToDictionaryString.ToLower().Contains(log.Type.ToLower()))
            {
                lock (logEntityCollection)
                {
                    if (count > MaximumLogCount)
                    {
                        count = 1;
                        logEntityCollection.Clear();
                        logEntityCollection.Add(count, log);
                    }
                    else
                    {
                        count++;
                        logEntityCollection.Add(count, log);
                    }
                }
            }
        }

        public static void Clear(string logType)
        {
            if (logType == null)
            {
                count = 0;
                logEntityCollection.Clear();
            }
            else
            {
                foreach (
                    var item in
                        logEntityCollection.Where(kvp => ((LogEntity) kvp.Value).Type.ToLower() == logType.ToLower())
                            .ToList())
                {
                    logEntityCollection.Remove(item.Key);
                }

            }
        }
    }

    public static class TpsLogManager<T>
    {

        private static readonly ILog _logger;

        public static void ConfigLog(string onlineLogAvailableFor, int maximumLogCount)
        {
            LogsCollection.AddToDictionaryString = (string.IsNullOrEmpty(onlineLogAvailableFor)) ? "" : onlineLogAvailableFor.ToLower();
            LogsCollection.MaximumLogCount = maximumLogCount;
            XmlConfigurator.Configure();
        }
        static TpsLogManager()
        {
            _logger = LogManager.GetLogger(typeof(T));
        }

        public static void Debug(object message, Exception exception)
        {
            
                LogsCollection.Add(new LogEntity
                {
                    Exception = exception,
                    Type = "DEBUG",
                    When = DateTime.Now,
                    Message = message,
                    Source = typeof (T).ToString()
                });
            
            _logger.Debug(message, exception);
        }

        public static void Debug(object message)
        {
            LogsCollection.Add(new LogEntity
                {
                    Exception = "",
                    Type = "DEBUG",
                    When = DateTime.Now,
                    Message = message,
                    Source = typeof (T).ToString()
                });
            
            _logger.Debug(message);
        }

        public static void Error(object message, Exception exception)
        {
            LogsCollection.Add(new LogEntity
                {
                    Exception = exception,
                    Type = "ERROR",
                    When = DateTime.Now,
                    Message = message,
                    Source = typeof (T).ToString()
                });
            _logger.Error(message, exception);
        }

        public static void Error(object message)
        {
           LogsCollection.Add(new LogEntity
                {
                    Exception = "",
                    Type = "ERROR",
                    When = DateTime.Now,
                    Message = message,
                    Source = typeof (T).ToString()
                });

            _logger.Error(message);
        }

        public static void Fatal(object message, Exception exception)
        {
            _logger.Fatal(message, exception);
        }

        public static void Fatal(object message)
        {
            _logger.Fatal(message);
        }

        public static void Info(object message, Exception exception)
        {
           LogsCollection.Add(new LogEntity
                {
                    Exception = exception,
                    Type = "INFO",
                    When = DateTime.Now,
                    Message = message,
                    Source = typeof (T).ToString()
                });
            _logger.Info(message, exception);
        }

        public static void Info(object message)
        {
           LogsCollection.Add(new LogEntity
                {
                    Exception = "",
                    Type = "INFO",
                    When = DateTime.Now,
                    Message = message,
                    Source = typeof (T).ToString()
                });
            _logger.Info(message);
        }

        public static void Warn(object message, Exception exception)
        {
           LogsCollection.Add(new LogEntity
                {
                    Exception = exception,
                    Type = "WARN",
                    When = DateTime.Now,
                    Message = message,
                    Source = typeof (T).ToString()
                });
            _logger.Warn(message, exception);
        }

        public static void Warn(object message)
        {
           LogsCollection.Add(new LogEntity
                {
                    Exception = "",
                    Type = "WARN",
                    When = DateTime.Now,
                    Message = message,
                    Source = typeof (T).ToString()
                });
            _logger.Warn(message);
        }

    }
}
