using Chronos.Accounts;
using Chronos.Accounts.Commands;
using Chronos.Accounts.Queries;
using Xunit;
using Xunit.Abstractions;
using ZES.Interfaces.Domain;
using ZES.Interfaces.Pipes;
using ZES.Tests;

namespace Chronos.Tests
{
    public class AccountTests : ChronosTest
    {
        public AccountTests(ITestOutputHelper outputHelper) 
            : base(outputHelper)
        {
        }

        [Fact]
        public async void CanCreateAccount()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var repository = container.GetInstance<IEsRepository<IAggregate>>();

            await await bus.CommandAsync(new CreateAccount("Account", AccountType.Saving));

            var account = await repository.Find<Account>("Account");
            Assert.NotNull(account); 
            Assert.Equal("Account", account.Id);

            await bus.Equal(new AccountStatsQuery(), s => s.NumberOfAccounts, 1);
        }

        [Fact]
        public async void CanDepositAsset()
        {
            var container = CreateContainer();
            var bus = container.GetInstance<IBus>();
            var repository = container.GetInstance<IEsRepository<IAggregate>>();
            
            await await bus.CommandAsync(new CreateAccount("Account", AccountType.Trading));
            await await bus.CommandAsync(new DepositAsset("Account", "Bitcoin", 1.0));
            
            var account = await repository.Find<Account>("Account");
            Assert.Equal("Bitcoin", account.Assets[0]);
        }
    }
}