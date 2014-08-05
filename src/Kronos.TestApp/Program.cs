using System;
using System.Configuration;
using Intelli.Kronos;
using MongoDB.Driver;
using log4net.Config;

namespace Kronos.TestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BasicConfigurator.Configure();
            var mongoUri = ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString;
            var mongoClient = new MongoClient(mongoUri);
            var db = mongoClient.GetServer().GetDatabase(new MongoUrl(mongoUri).DatabaseName);

            var taskService = new KronosTaskService(db);
            var id = taskService.ScheduleTask(new ConsoleWriterTask(), DateTime.UtcNow, TimeSpan.FromSeconds(5), "TestTask");

            Console.WriteLine("Task with id " + id + " scheduled");

            var host = new KronosHost(db, 2);
            host.Start();

            Console.ReadLine();
            Console.WriteLine("Removing test task");
            taskService.UnscheduleTask(id);
            host.Stop();
            host.Dispose();
        }
    }
}
