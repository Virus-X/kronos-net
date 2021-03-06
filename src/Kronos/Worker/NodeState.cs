﻿using System;
using MongoDB.Bson;

namespace Intelli.Kronos.Worker
{
    public class NodeState
    {
        public ObjectId Id { get; set; }

        public string Ip { get; set; }

        public string Host { get; set; }

        public bool IsOnline
        {
            get { return (DateTime.UtcNow - LastSeen).TotalSeconds < KronosConfig.DeadNodeSeconds; }
        }

        public DateTime LastSeen { get; set; }

        public void RefreshLastSeen()
        {
            LastSeen = DateTime.UtcNow;
        }
    }
}
