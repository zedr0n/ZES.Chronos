using Xunit.Abstractions;

namespace Chronos.Tests
{
    public class DebugTests : ChronosTest
    {
        public DebugTests(ITestOutputHelper outputHelper) 
            : base(outputHelper)
        {
        }

        protected override string LogEnabled => "INFO";
    }
}