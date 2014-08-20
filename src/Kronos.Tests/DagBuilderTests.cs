using System;
using FluentAssertions;
using Intelli.Kronos;
using NUnit.Framework;

namespace Kronos.Tests
{
    [TestFixture]
    public class DagBuilderTests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void CreateDag_SimpleTaskDependency_OK()
        {
            var dag = new DagBuilder();
            var taskA = new FakeTask();
            var taskB = new FakeTask();
            dag.Task(taskB).DependsOn(taskA);
            dag.CreateDag().Count.Should().Be(2);

            taskA.ChildTasks.Count.Should().Be(1);
            taskA.ChildTasks.Should().Contain(taskB.Id);
            taskA.DependsOn.Should().BeNullOrEmpty();

            taskB.ChildTasks.Should().BeNullOrEmpty();
            taskB.DependsOn.Count.Should().Be(1);
            taskB.DependsOn.Should().Contain(taskA.Id);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DependsOn_SimpleCycle_ThrowsException()
        {
            var dag = new DagBuilder();
            var taskA = new FakeTask();
            var taskB = new FakeTask();
            dag.Task(taskB).DependsOn(taskA);
            dag.Task(taskA).DependsOn(taskB);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DependsOn_AdvancedCycle_ThrowsException()
        {
            var dag = new DagBuilder();
            var taskA = new FakeTask();
            var taskB = new FakeTask();
            var taskC = new FakeTask();
            dag.Task(taskB).DependsOn(taskA);
            dag.Task(taskC).DependsOn(taskB);
            dag.Task(taskA).DependsOn(taskC);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DependsOn_SelfReference_ThrowsException()
        {
            var dag = new DagBuilder();
            var taskA = new FakeTask();
            dag.Task(taskA).DependsOn(taskA);
        }

        [Test]
        public void DependsOn_NoCycle_OK()
        {
            var dag = new DagBuilder();
            var taskA = new FakeTask();
            var taskB = new FakeTask();
            var taskC = new FakeTask();
            dag.Task(taskB).DependsOn(taskA);
            dag.Task(taskC).DependsOn(taskB);
            dag.Task(taskC).DependsOn(taskA);
        }
    }
}
