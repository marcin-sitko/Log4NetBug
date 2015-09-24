using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Log4NetActivateOptionsIssue
{
    [TestClass]
    public class TestClass
    {
        private const string ConnectionString = "Data Source=[Data_Source];Initial Catalog=[Catalog];Integrated Security=True";
        private const int AmmountOfThreads = 25;
        private const int Loop = 7;

        private static string activationTimes = "";
        
        private static void Write(string msg )
        {
            lock (activationTimes)
            {
                activationTimes += msg;
            }
        }

        [TestInitialize]
        public void Setup()
        {
            Helpers.ConfigureWithDb(ConnectionString);
            var logger = LogManager.GetLogger(this.GetType());

            logger.Error("Hello");
        }

        [TestMethod]
        public void ParallelActivateOptionsIssue()
        {
            ICollection<Thread> threads = new List<Thread>();
            
            for (int i = 0; i < AmmountOfThreads; i++)
            {
                threads.Add(new Thread(() =>
                {
                    var stopWatch = new Stopwatch();
                    for (int j = 0; j < Loop; j++)
                    {
                        var hier = (Hierarchy)LogManager.GetRepository();
                        if (hier != null)
                        {
                            var appenders = hier.GetAppenders().OfType<AdoNetAppender>();
                            foreach (var appender in appenders)
                            {
                                stopWatch.Restart();
                                appender.ActivateOptions();
                                Write(string.Format("Thread: {0} , Iteration {1} , Elapsed [ms] {2} \n", Thread.CurrentThread.ManagedThreadId, j, stopWatch.ElapsedMilliseconds));
                            }
                        }
                    }
                }));
            }

            foreach (var thread in threads)
            {
                thread.Start();
            }

            var mainWatch = new Stopwatch();
            mainWatch.Start();
            do
            {
                
            } while (threads.Any(thread => thread.IsAlive));
            mainWatch.Stop();

            Assert.IsTrue(mainWatch.ElapsedMilliseconds < 10000, activationTimes);
        }
    }


    public static class Helpers
    {

        public static void ConfigureWithDb(string cs)
        {
            Hierarchy h = (Hierarchy)LogManager.GetRepository();
            h.Root.Level = Level.All;

            IAppender ado = CreateAdoNetAppender(cs);
            h.Root.AddAppender(ado);
            h.Configured = true;
        }

        public static IAppender CreateAdoNetAppender(string cs)
        {
            AdoNetAppender appender = new AdoNetAppender();
            appender.Name = "AdoNetAppender";
            appender.BufferSize = 1;
            appender.ConnectionType = "System.Data.SqlClient.SqlConnection, System.Data, Version=1.0.3300.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            appender.ConnectionString = cs;
            appender.CommandText = @"INSERT INTO LogNet 
            ([DateUtc],[Thread],[Level],[Logger],[User],[Message],[Exception]) VALUES 
            (@date,@thread,@level,@logger,@user,@message,@exception)";
            AddDateTimeParameterToAppender(appender, "date");
            AddStringParameterToAppender(appender, "thread", 20, "%thread");
            AddStringParameterToAppender(appender, "level", 10, "%level");
            AddStringParameterToAppender(appender, "logger", 200, "%logger");
            AddStringParameterToAppender(appender, "user", 20, "%property{user}");
            AddStringParameterToAppender(appender, "message", 1000, "%message%newline%property");
            AddErrorParameterToAppender(appender, "exception", 4000);
            appender.ActivateOptions();
            return appender;
        }

        public static void AddErrorParameterToAppender(this log4net.Appender.AdoNetAppender appender, string paramName, int size)
        {
            AdoNetAppenderParameter param = new AdoNetAppenderParameter();
            param.ParameterName = paramName;
            param.DbType = System.Data.DbType.String;
            param.Size = size;
            param.Layout = new Layout2RawLayoutAdapter(new ExceptionLayout());
            appender.AddParameter(param);
        }

        public static void AddStringParameterToAppender(this log4net.Appender.AdoNetAppender appender, string paramName, int size, string conversionPattern)
        {
            AdoNetAppenderParameter param = new AdoNetAppenderParameter();
            param.ParameterName = paramName;
            param.DbType = System.Data.DbType.String;
            param.Size = size;
            param.Layout = new Layout2RawLayoutAdapter(new PatternLayout(conversionPattern));
            appender.AddParameter(param);
        }

        public static void AddDateTimeParameterToAppender(this log4net.Appender.AdoNetAppender appender, string paramName)
        {
            AdoNetAppenderParameter param = new AdoNetAppenderParameter();
            param.ParameterName = paramName;
            param.DbType = System.Data.DbType.DateTime;
            param.Layout = new RawUtcTimeStampLayout();
            appender.AddParameter(param);
        }
    }
}
