
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

using MinecraftService;

// Create the host builder
var builder = Host.CreateApplicationBuilder(args);

// Load settings from configuration
var settings = new ServiceSettings {
	JavaHome = builder.Configuration["Minecraft:JavaHome"],
	ServerDirectory = builder.Configuration["Minecraft:ServerDirectory"],
	JarFileName = builder.Configuration["Minecraft:JarFileName"],
	AdditionalArgs = builder.Configuration["Minecraft:AdditionalArgs"]
};
// Make settings available for dependency injection
builder.Services.AddSingleton(settings);

// Configure logging to use the Windows Event Log
builder.Services.AddWindowsService(options => {
	options.ServiceName = "Minecraft Service";
});
LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(builder.Services);

// Register the background service that will manage the Minecraft server process
builder.Services.AddHostedService<MinecraftWorker>();

// Build and run the host
var host = builder.Build();
host.Run();

