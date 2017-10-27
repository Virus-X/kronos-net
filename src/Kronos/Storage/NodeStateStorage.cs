using System.Collections.Generic;
using Intelli.Kronos.Worker;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Intelli.Kronos.Storage
{
    public interface INodeStateStorage
    {
        void Save(NodeState state);

        IEnumerable<NodeState> GetAll();

        void Remove(ObjectId nodeId);
    }

    public class NodeStateStorage : INodeStateStorage
    {
        private readonly IMongoCollection<NodeState> nodeCollection;

        public NodeStateStorage(IMongoDatabase db)
        {
            nodeCollection = db.GetCollection<NodeState>(KronosConfig.NodeStateCollection);
        }

        public void Save(NodeState state)
        {
            nodeCollection.ReplaceOne(x => x.Id == state.Id, state, new UpdateOptions { IsUpsert = true });
        }

        public IEnumerable<NodeState> GetAll()
        {
            return nodeCollection.AsQueryable().ToList();
        }

        public void Remove(ObjectId nodeId)
        {
            nodeCollection.DeleteOne(x => x.Id == nodeId);
        }
    }
}
