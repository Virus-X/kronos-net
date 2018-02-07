using System.Configuration;
using System.Threading;
using FluentAssertions;
using Intelli.Kronos;
using Intelli.Kronos.Processors;
using Intelli.Kronos.Storage;
using Intelli.Kronos.Tasks;
using log4net.Config;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace Kronos.Tests
{
    [TestFixture(Category = "Integration")]
    public class CappedCollectionTest
    {
        private IKronosHost host;

        private IKronosTaskService taskService;
        private IMongoDatabase db;

        [TestFixtureSetUp]
        public void SetUpFixture()
        {
            BasicConfigurator.Configure();
        }

        [SetUp]
        public void SetUp()
        {
            KronosConfig.CappedCollectionCount = 10;

            var mongoUri = ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString;
            var mongoClient = new MongoClient(mongoUri);
            db = mongoClient.GetDatabase(new MongoUrl(mongoUri).DatabaseName);

            db.DropCollection(KronosConfig.TasksCollection);
            db.DropCollection(KronosConfig.FailedTasksCollection);
            db.DropCollection(KronosConfig.NodeStateCollection);
            db.DropCollection(KronosConfig.ScheduledTasksCollection);

            host = new KronosHost(db, 1);
            taskService = new KronosTaskService(new StorageFactory(db), new NullMetricsCounter());
            host.Start();
        }

        [TearDown]
        public void TearDown()
        {
            KronosConfig.CappedCollectionCount = 200000;
            host.Stop();
        }

        [Test]
        public void RunStubTasks()
        {
            for (int i = 0; i < 100; i++)
            {
                taskService.AddTask(new StubTask { Index = i });
            }

            db.GetCollection<BsonDocument>(KronosConfig.TasksCollection).Count(Builders<BsonDocument>.Filter.Exists("_id")).Should().Be(10);
        }

        [TaskProcessor(typeof(StubTaskProcessor))]
        private class StubTask : KronosTask
        {
            public int Index { get; set; }
        }

        private class StubTaskProcessor : KronosTaskProcessor<StubTask>
        {
            public override void Process(StubTask task, IKronosTaskService taskService, CancellationToken token)
            {
            }
        }
    }
}
