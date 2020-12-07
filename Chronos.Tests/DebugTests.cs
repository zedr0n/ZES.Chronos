using Chronos.Hashflare.Commands;
using Chronos.Hashflare.Queries;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using ZES.Infrastructure.Domain;
using ZES.Interfaces;
using ZES.Interfaces.Pipes;
using ZES.Tests;

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