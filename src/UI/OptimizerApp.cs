using System;
using Il2CppScheduleOne.UI;

namespace ClientAssignmentOptimizer.UI
{
    // Spike (issue #10): minimum-viable App<T> subclass to test if Il2CppInterop's
    // ClassInjector accepts a generic-base subclass. No UI is built here — the
    // only goal is to learn whether the type can be registered at startup
    // without an exception. If this works, the rest of #10 is mostly mechanical
    // refactor of the existing OptimizerTab panel-build code into this class.
    public class OptimizerApp : App<OptimizerApp>
    {
        public OptimizerApp(IntPtr ptr) : base(ptr) { }
    }
}
