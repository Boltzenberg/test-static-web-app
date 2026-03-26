using Boltzenberg.Functions.Commands.Telegram;
using Boltzenberg.Functions.Domain.Telegram;
using Boltzenberg.Functions.Storage;
using Boltzenberg.Functions.Storage.Documents;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

// Storage
builder.Services.AddSingleton<IJsonStore<GroceryListDocument>, JsonStore<GroceryListDocument>>();
builder.Services.AddSingleton<IJsonStore<AddressBookDocument>, JsonStore<AddressBookDocument>>();
builder.Services.AddSingleton<IJsonStore<SecretSantaConfigDocument>, JsonStore<SecretSantaConfigDocument>>();
builder.Services.AddSingleton<IJsonStore<SecretSantaEventDocument>, JsonStore<SecretSantaEventDocument>>();
builder.Services.AddSingleton<IJsonStore<RefreshableTokenDocument>, JsonStore<RefreshableTokenDocument>>();
builder.Services.AddSingleton<IJsonStore<AuthLogDocument>, JsonStore<AuthLogDocument>>();

// Telegram commands
builder.Services.AddSingleton<ICommand, PingCommand>();
builder.Services.AddSingleton<ICommand>(sp =>
    new AddItemCommand(sp.GetRequiredService<IJsonStore<GroceryListDocument>>()));
builder.Services.AddSingleton<ICommand>(sp =>
    new RemoveItemCommand(sp.GetRequiredService<IJsonStore<GroceryListDocument>>()));
builder.Services.AddSingleton<ICommand>(sp =>
    new ListCommand(sp.GetRequiredService<IJsonStore<GroceryListDocument>>()));

builder.Services.AddSingleton<CommandDispatcher>(sp =>
    new CommandDispatcher(sp.GetServices<ICommand>()));

builder.Build().Run();
