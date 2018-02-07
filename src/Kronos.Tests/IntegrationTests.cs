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
        protected static volatile int SharedResource = 0;
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
            var db = mongoClient.GetDatabase(new MongoUrl(mongoUri).DatabaseName);

            db.DropCollection(KronosConfig.TasksCollection);
            db.DropCollection(KronosConfig.FailedTasksCollection);
            db.DropCollection(KronosConfig.NodeStateCollection);
            db.DropCollection(KronosConfig.ScheduledTasksCollection);

            host = new KronosHost(db, 1);
            taskService = new KronosTaskService(new StorageFactory(db), new NullMetricsCounter());
            processedEvent.Reset();
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
            taskService.AddTask(new StubTask());
            processedEvent.WaitOne(200000).Should().BeTrue();
        }

        [Test]
        public void SimulateTaskException()
        {
            taskService.AddTask(new StubTask { SimulateCrash = true });
            processedEvent.WaitOne(5000).Should().BeFalse();
        }

        [Test]
        public void SimulateDagTasks()
        {
            SharedResource = 0;
            var dagBuilder = new DagBuilder();
            dagBuilder.Task(new DagTaskParent { ExpectedCount = 2 })
                .DependsOn(new DagTaskChild())
                .DependsOn(new DagTaskChild());

            taskService.AddDagTasks(dagBuilder);
            processedEvent.WaitOne(200000).Should().BeTrue();
        }

        [TaskProcessor(typeof(StubTaskProcessor))]
        private class StubTask : KronosTask
        {
            public bool SimulateCrash { get; set; }

            public bool SimulateTimeout { get; set; }
        }

        [TaskProcessor(typeof(Processor))]
        private class DagTaskParent : KronosTask
        {
            public int ExpectedCount { get; set; }

            public class Processor : KronosTaskProcessor<DagTaskParent>
            {
                public override void Process(DagTaskParent task, IKronosTaskService taskService, CancellationToken token)
                {
                    if (task.ExpectedCount != SharedResource)
                    {
                        throw new InvalidOperationException("Expectation failed");
                    }
                    processedEvent.Set();
                }
            }
        }

        [TaskProcessor(typeof(Processor))]
        private class DagTaskChild : KronosTask
        {

            public class Processor : KronosTaskProcessor<DagTaskChild>
            {
                public override void Process(DagTaskChild task, IKronosTaskService taskService, CancellationToken token)
                {
                    SharedResource++;
                }
            }
        }

        private class StubTaskProcessor : KronosTaskProcessor<StubTask>
        {
            public override void Process(StubTask task, IKronosTaskService taskService, CancellationToken token)
            {
                if (task.SimulateCrash)
                {
                    throw new InvalidOperationException("oops");
                }

                if (task.SimulateTimeout)
                {
                    token.WaitHandle.WaitOne(100000);
                }

                processedEvent.Set();
            }
        }
    }
}
