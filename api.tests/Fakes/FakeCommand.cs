using Boltzenberg.Functions.Domain.Telegram;

namespace ApiTests.Fakes
{
    public class FakeCommand : ICommand
    {
        public string Name { get; }
        public bool RequiresAuthorization { get; }

        private readonly CommandResult _result;
        public CommandContext? LastContext { get; private set; }
        public int ExecuteCount { get; private set; }

        public FakeCommand(string name, bool requiresAuth, CommandResult result)
        {
            Name = name;
            RequiresAuthorization = requiresAuth;
            _result = result;
        }

        public Task<CommandResult> ExecuteAsync(CommandContext context)
        {
            LastContext = context;
            ExecuteCount++;
            return Task.FromResult(_result);
        }
    }
}
