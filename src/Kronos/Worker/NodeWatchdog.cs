using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Intelli.Kronos.Storage;
using log4net;
using MongoDB.Bson;

namespace Intelli.Kronos.Worker
{
    public interface INodeWatchdog
    {
        void Start();

        void Stop();
    }

    public class NodeWatchdog : INodeWatchdog
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(NodeWatchdog));

        private readonly INodeStateStorage nodeStateStorage;
        private readonly ITasksStorage tasksStorage;
        private readonly IScheduledTasksStorage scheduledTasksStorage;
        private readonly Timer watchdogTimer;
        private readonly NodeState nodeState;
        private readonly object timerLock = new object();

        public NodeWatchdog(
            ObjectId worknodeId,
            IStorageFactory storageFactory)
        {
            nodeStateStorage = storageFactory.GetNodeStateStorage();
            tasksStorage = storageFactory.GetTasksStorage();
            scheduledTasksStorage = storageFactory.GetScheduledTasksStorage();
            watchdogTimer = new Timer(OnTick);
            nodeState = new NodeState
            {
                Id = worknodeId,
                Host = Dns.GetHostName(),
                Ip = GetLocalIPAddress().ToString(),
                LastSeen = DateTime.UtcNow
            };
        }

        public void Start()
        {
            watchdogTimer.Change(0, 10000);
            Log.DebugFormat("Starting watchdog on {0}@{1}", nodeState.Id, nodeState.Ip);
        }

        public void Stop()
        {
            watchdogTimer.Change(0, 0);
        }

        private void OnTick(object state)
        {
            if (!Monitor.TryEnter(timerLock))
            {
                return;
            }

            try
            {
                nodeState.RefreshLastSeen();
                nodeStateStorage.Save(nodeState);
                ReleaseStalledTasks();
            }
            finally
            {
                Monitor.Exit(timerLock);
            }
        }

        private void ReleaseStalledTasks()
        {
            var nodes = nodeStateStorage.GetAll().ToList();
            var deadNodes = nodes.Where(x => !x.IsOnline);
            int releasedTasks = 0;
            foreach (var node in deadNodes)
            {
                releasedTasks += tasksStorage.ReleaseLockedTasks(node.Id);
                releasedTasks += scheduledTasksStorage.ReleaseAllTasks(node.Id);
                nodeStateStorage.Remove(node.Id);                
            }

            if (releasedTasks > 0)
            {
                Log.DebugFormat("Released {0} stalled tasks", releasedTasks);
            }
        }

        private IPAddress GetLocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }
    }
}
