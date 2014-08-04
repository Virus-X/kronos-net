using System;
using System.Threading;
using Intelli.Kronos;
using Intelli.Kronos.Processors;
using Intelli.Kronos.Tasks;

namespace Kronos.TestApp
{
    [TaskProcessor(typeof(ConsoleWriterTaskProcessor))]
    public class ConsoleWriterTask : KronosTask
    {
        public string CurrentTime
        {
            get { return DateTime.Now.ToLongTimeString(); }
        }
    }

    public class ConsoleWriterTaskProcessor : KronosTaskProcessor<ConsoleWriterTask>
    {
        public override void Process(ConsoleWriterTask task, IKronosTaskService taskService, CancellationToken token)
        {
            Console.WriteLine(task.CurrentTime);
        }
    }
}
