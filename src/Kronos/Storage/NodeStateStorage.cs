using System;
using System.Collections.Generic;
using Intelli.Kronos.Worker;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Intelli.Kronos.Storage
{
    public interface INodeStateStorage
    {
        void Save(NodeState state);

        IEnumerable<NodeState> GetAll();

        void Remove(Guid nodeId);
    }

    public class NodeStateStorage : INodeStateStorage
    {
        private readonly MongoCollection<NodeState> nodeCollection;

        public NodeStateStorage(MongoDatabase db)
        {
            nodeCollection = db.GetCollection<NodeState>(KronosConfig.NodeStateCollection);
        }

        public void Save(NodeState state)
        {
            nodeCollection.Save(state);
        }

        public IEnumerable<NodeState> GetAll()
        {
            return nodeCollection.FindAll();
        }

        public void Remove(Guid nodeId)
        {
            nodeCollection.Remove(Query<NodeState>.EQ(x => x.Id, nodeId));
        }
    }
}
