using System;
using System.Collections.Generic;
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
        
        protected override IEnumerable<Type> Configs => new List<Type>
        {
            typeof(Core.Config),
            typeof(Coins.Config),
            typeof(Accounts.Config),
            typeof(Hashflare.Config),
        };

        protected override Container CreateContainer(List<Action<Container>> registrations = null)
        {
            var regs = new List<Action<Container>>
            {
                Core.Config.RegisterAll,
                Coins.Config.RegisterAll,
                Accounts.Config.RegisterAll,
                Hashflare.Config.RegisterAll,
            };
            if (registrations != null)
                regs.AddRange(registrations);

            return base.CreateContainer(regs);
        }
    }
}