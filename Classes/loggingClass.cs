﻿using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TEPSClientInstallService_UpdateUtility.Classes
{
    internal class loggingClass
    {
        private static Logger _logger;
        private readonly string logFileName = "ClientAdminToolLog.json";

        private string errorID;

        //adds log messages to log collection (which is then seen via the internal log viewer view
        public void logEntryWriter(string logMessage, string level)
        {
           

            nLogLogger(logMessage, level);
        }

        

        public void initializeNLogLogger()
        {
            var config = new LoggingConfiguration();

            var target =
                new FileTarget
                {
                    FileName = logFileName
                };

            config.AddTarget("logfile", target);

            var rule = new LoggingRule("*", LogLevel.Debug, target);

            config.LoggingRules.Add(rule);

            LogManager.Configuration = config;
        }

        //more performant logging for moving files
        public void nLogLogger(string message, string level)
        {
            _logger = LogManager.GetLogger(level);

            switch (level)
            {
                case "info":
                    _logger.Info(message);
                    break;

                case "debug":
                    _logger.Debug(message);
                    break;

                case "error":
                    _logger.Error(message);
                    break;

                default:
                    break;
            }
        }
    }
}
