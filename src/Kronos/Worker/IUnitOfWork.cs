using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Intelli.Kronos.Worker
{
    public interface IUnitOfWork
    {
        int Priority { get; }

        void Process(CancellationToken token);

        void Release();
    }
}
