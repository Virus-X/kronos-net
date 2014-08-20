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
        public string Text { get; set; }

        public ConsoleWriterTask()
        {
        }

        public ConsoleWriterTask(string rext)
        {
            Text = rext;
        }
    }

    public class ConsoleWriterTaskProcessor : KronosTaskProcessor<ConsoleWriterTask>
    {
        public override void Process(ConsoleWriterTask task, IKronosTaskService taskService, CancellationToken token)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(task.Text);
            Console.ResetColor();
        }
    }
}
