using System;
using System.Configuration;
using Intelli.Kronos;
using log4net.Config;
using MongoDB.Driver;

namespace Kronos.TestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BasicConfigurator.Configure();
            var mongoUri = ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString;
            var mongoClient = new MongoClient(mongoUri);
            var db = mongoClient.GetDatabase(new MongoUrl(mongoUri).DatabaseName);
            KronosConfig.CappedCollectionCount = 100;

            var taskService = new KronosTaskService(db);
            taskService.CancelAllByDiscriminator("ConsoleWriterTask");

            //var id = taskService.ScheduleTask(new ConsoleWriterTask { Text = "Hello, world!" }, DateTime.UtcNow, TimeSpan.FromSeconds(5), "TestTask");
            //Console.WriteLine("Task with id " + id + " scheduled");

            var dag = new DagBuilder();
            var taskA = new ConsoleWriterTask("I depend on B and cannot run before it");
            var taskB = new ConsoleWriterTask("I'm B and I can run instantly");
            var taskC = new ConsoleWriterTask("I'm C and I can run only after those two guys");
            dag.Task(taskA).DependsOn(taskB);
            dag.Task(taskC).DependsOn(taskA, taskB);

            taskService.AddDagTasks(dag);

            var host = new KronosHost(db, 2);
            host.Start();

            Console.ReadLine();
            taskService.CancelAllByDiscriminator("ConsoleWriterTask");
            host.Stop();
            host.Dispose();
        }
    }
}
