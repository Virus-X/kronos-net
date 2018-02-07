using System;
using System.Collections.Generic;
using System.Linq;
using Intelli.Kronos.Tasks;

namespace Intelli.Kronos
{
    public class DagBuilder
    {
        private readonly Dictionary<KronosTask, Node> nodes;

        public DagBuilder()
        {
            nodes = new Dictionary<KronosTask, Node>();
        }

        public Node Task(KronosTask task)
        {
            Node node;
            if (nodes.TryGetValue(task, out node))
            {
                return node;
            }

            node = new Node(this, task);
            nodes[task] = node;
            return node;
        }

        public List<KronosTask> CreateDag()
        {
            foreach (var node in nodes.Values)
            {
                node.Task.GenerateId();
            }

            foreach (var node in nodes.Values)
            {
                var task = node.Task;
                if (node.Dependencies.Any())
                {
                    task.SetWaitingState();
                    task.DependsOn = node.Dependencies.Select(x => x.Task.Id).ToDictionary(x => x, x => false);
                    foreach (var dep in node.Dependencies)
                    {
                        if (dep.Task.ChildTasks == null)
                        {
                            dep.Task.ChildTasks = new List<string>();
                        }

                        dep.Task.ChildTasks.Add(task.Id);
                    }
                }
                else
                {
                    task.ResetState();
                    task.DependsOn = null;
                }
            }

            // Tasks would be written to DB in this order. 
            // We want to store all tasks with dependencies before active ones to be safe from race-conditions.
            // In C# bool sort order is: false, true, so descending is used
            return nodes.Values.Select(x => x.Task).OrderByDescending(x => x.HasDependencies).ToList();
        }

        public class Node
        {
            private readonly DagBuilder builder;
            public KronosTask Task { get; private set; }

            private readonly List<Node> dependencies;

            public IEnumerable<Node> Dependencies { get { return dependencies.AsReadOnly(); } }

            internal Node(DagBuilder builder, KronosTask task)
            {
                this.builder = builder;
                Task = task;
                dependencies = new List<Node>();
            }

            public Node DependsOn(KronosTask task)
            {
                var taskNode = builder.Task(task);

                if (taskNode == this)
                {
                    throw new InvalidOperationException("Task cannot depend on itself");
                }

                if (dependencies.Contains(taskNode))
                {
                    return this;
                }

                var cycle = DetectCycle(taskNode, new HashSet<Node> { this });

                if (cycle != null)
                {
                    cycle.Push(this);
                    throw new InvalidOperationException("Dependency introduces a cycle in the graph: "
                        + string.Join("->", cycle.Select(x => x.Task.ToString())));
                }

                dependencies.Add(taskNode);
                return this;
            }

            public Node DependsOn(IEnumerable<KronosTask> tasks)
            {
                foreach (var task in tasks)
                {
                    DependsOn(task);
                }

                return this;
            }

            public Node DependsOn(params KronosTask[] tasks)
            {
                return DependsOn(tasks.AsEnumerable());
            }

            private Stack<Node> DetectCycle(Node node, HashSet<Node> visitedNodes)
            {
                if (!visitedNodes.Add(node))
                {
                    // Node already visited! Create stack for investigation and return
                    var path = new Stack<Node>();
                    path.Push(this);
                    return path;
                }

                visitedNodes.Add(node);
                foreach (var dep in node.dependencies)
                {
                    var path = DetectCycle(dep, visitedNodes);
                    if (path != null)
                    {
                        path.Push(node);
                        return path;
                    }
                }

                visitedNodes.Remove(node);
                return null;
            }
        }

    }
}
