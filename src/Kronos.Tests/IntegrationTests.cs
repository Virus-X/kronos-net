using System;
using System.Configuration;
using System.Threading;
using FluentAssertions;
using Intelli.Kronos;
using Intelli.Kronos.Processors;
using Intelli.Kronos.Storage;
using Intelli.Kronos.Tasks;
using log4net.Config;
using MongoDB.Driver;
using NUnit.Framework;

namespace Kronos.Tests
{
    [TestFixture(Category = "Integration")]
    public class IntegrationTests
    {
        protected static ManualResetEvent processedEvent = new ManualResetEvent(false);
        private IKronosHost host;
        private IKronosTaskService taskService;

        [TestFixtureSetUp]
        public void SetUpFixture()
        {
            BasicConfigurator.Configure();
        }

        [SetUp]
        public void SetUp()
        {
            var mongoUri = ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString;
            var mongoClient = new MongoClient(mongoUri);
            var db = mongoClient.GetServer().GetDatabase(new MongoUrl(mongoUri).DatabaseName);

            db.DropCollection(KronosConfig.TasksCollection);
            db.DropCollection(KronosConfig.FailedTasksCollection);
            db.DropCollection(KronosConfig.NodeStateCollection);
            db.DropCollection(KronosConfig.ScheduledTasksCollection);

            host = new KronosHost(db, 1);            
            taskService = new KronosTaskService(new StorageFactory(db));
            host.Start();
        }

        [TearDown]
        public void TearDown()
        {
            host.Stop();
        }


        [Test]
        public void RunStubTasks()
        {
            host.Start();
            taskService.AddTask(new StubTask());
            processedEvent.WaitOne(200000).Should().BeTrue();
        }

        [Test]
        public void SimulateTaskException()
        {
            host.Start();
            taskService.AddTask(new StubTask { SimulateCrash = true });
            processedEvent.WaitOne(200000).Should().BeTrue();
        }        

        [TaskProcessor(typeof(StubTaskProcessor))]
        private class StubTask : KronosTask
        {
            public bool SimulateCrash { get; set; }
        }

        private class StubTaskProcessor : KronosTaskProcessor<StubTask>
        {
            public override void Process(StubTask task, IKronosTaskService taskService, CancellationToken token)
            {
                if (task.SimulateCrash)
                {
                    throw new InvalidOperationException("oops");
                }

                processedEvent.Set();
            }
        }
    }
}
