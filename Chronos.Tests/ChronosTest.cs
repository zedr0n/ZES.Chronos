using System;
using System.Collections.Generic;
using Chronos.Coins;
using SimpleInjector;
using Xunit.Abstractions;
using ZES.Tests;

namespace Chronos.Tests
{
    public class ChronosTest : Test
    {
        protected ChronosTest(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }
        
        protected override Container CreateContainer(List<Action<Container>> registrations = null)
        {
            var regs = new List<Action<Container>>
            {
                Config.RegisterAll
            };
            if (registrations != null)
                regs.AddRange(registrations);

            return base.CreateContainer(regs);
        }
    }
}