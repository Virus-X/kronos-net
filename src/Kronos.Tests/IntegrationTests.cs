using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using FluentAssertions;
using Intelli.Kronos;
using Intelli.Kronos.Processors;
using Intelli.Kronos.Storage;
using Intelli.Kronos.Tasks;
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

        [SetUp]
        public void SetUp()
        {
            KronosConfig.ClearProcessors();
            KronosConfig.RegisterProcessor(new StubTaskProcessor());
            var mongoUri = ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString;
            var mongoClient = new MongoClient(mongoUri);
            var db = mongoClient.GetServer().GetDatabase(new MongoUrl(mongoUri).DatabaseName);

            db.DropCollection(KronosConfig.TasksCollection);
            db.DropCollection(KronosConfig.FailedTasksCollection);
            db.DropCollection(KronosConfig.NodeStateCollection);
            db.DropCollection(KronosConfig.ScheduledTasksCollection);

            host = new KronosHost(db, 1);

            taskService = new KronosTaskService(new StorageFactory(db));
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

        private class StubTask : NodeTask
        {            
            public void PulseEvent()
            {
                IntegrationTests.processedEvent.Set();
            }
        }

        private class StubTaskProcessor : NodeTaskProcessor<StubTask>
        {
            public override void Process(StubTask task, IKronosTaskService kronosTaskService, CancellationToken token)
            {
                task.PulseEvent();                
            }
        }
    }
}
