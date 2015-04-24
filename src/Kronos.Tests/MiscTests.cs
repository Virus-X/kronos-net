using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using FluentAssertions;

using NUnit.Framework;

namespace Kronos.Tests
{
    [TestFixture]
    public class MiscTests
    {
        [Test]
        public void LinkedCancelationTest()
        {
            var cts1 = new CancellationTokenSource();
            var cts2 = CancellationTokenSource.CreateLinkedTokenSource(cts1.Token);
            var cts3 = CancellationTokenSource.CreateLinkedTokenSource(cts1.Token);

            cts2.Cancel();
            cts2.Token.IsCancellationRequested.Should().BeTrue();
            cts1.Token.IsCancellationRequested.Should().BeFalse();

            cts1.Cancel();
            cts3.Token.IsCancellationRequested.Should().BeTrue();
        }
    }
}
