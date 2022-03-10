using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using AmmoLocker;
using BepInEx.Logging;

namespace TestTracing
{
    [TestClass]
    public class TestTracing
    {
        public delegate int UnaryOp(Func<int, int> orig, int x);

#if DEBUG
        [TestMethod]
        public void TestTrace()
        {
            Log.Init(new ManualLogSource("Tests"));

            var traced = Tracing.Trace<UnaryOp>(Tracer.Instance);

            Assert.AreEqual(traced(x => x * 2, 7), 12);
            
        }
#endif
    }
}
