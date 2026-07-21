using System.Diagnostics;

namespace MinecraftService;

public class MinecraftWorker(ILogger<MinecraftWorker> logger, ServiceSettings settings) : BackgroundService {

	private Process? Process;
	private Task? OutputReaderTask;
	private Task? ErrorReaderTask;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {

		try {
			if (!settings.Check(logger)) {
				throw new Exception("Invalid settings");
			}

			logger.LogInformation("Minecraft server directory {directory}", settings.ServerDirectory);
			if (!Directory.Exists(settings.ServerDirectory)) {
				throw new Exception("Minecraft server directory not found");
			}

			// Start the process
			var processStartInfo = CreateProcessStartInfo();
			Process = Process.Start(processStartInfo)
							?? throw new Exception("Process could not be started");

			// Start reading output and error streams
			OutputReaderTask = CreateReaderTask(stoppingToken, Process.StandardOutput);
			ErrorReaderTask = CreateReaderTask(stoppingToken, Process.StandardError, isError: true);

			// Wait until the process exits or cancellation is requested
			while (!stoppingToken.IsCancellationRequested) {
				await Task.Delay(1000, stoppingToken);
			}

		} catch (Exception e) when (e is not TaskCanceledException) {
			// Startup failed, log the error and exit with a non-zero code to indicate failure
			logger.LogCritical(e, "Error in service");
			Environment.Exit(1);
		}
	}

	private ProcessStartInfo CreateProcessStartInfo() {

		// Build the command line arguments
		var processStartInfo = new ProcessStartInfo(settings.JavaExe!) {
			CreateNoWindow = true,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			WindowStyle = ProcessWindowStyle.Hidden,
			WorkingDirectory = settings.ServerDirectory,
		};

		if (settings.JavaHome != null) {
			processStartInfo.Environment.Add("JAVA_HOME", settings.JavaHome);
		}

		foreach (string arg in settings.AdditionalArgs.Split(' ')) {
			processStartInfo.ArgumentList.Add(arg);
		}
		processStartInfo.ArgumentList.Add("-jar");
		processStartInfo.ArgumentList.Add(settings.JarFullPath!);
		processStartInfo.ArgumentList.Add("nogui");

		logger.LogInformation("Start: {commandLine}", $"{processStartInfo.FileName} {string.Join(" ", processStartInfo.ArgumentList)}");
		return processStartInfo;
	}


	private Task CreateReaderTask(CancellationToken stoppingToken, StreamReader reader, bool isError = false) {

		return Task.Run(async () => {

			while (!stoppingToken.IsCancellationRequested) {

				string? line = await reader.ReadLineAsync();
				if (line == null) {
					// End of stream reached, process has likely exited
					return;
				}

				if (isError) {
					logger.LogError("{line}", line);
				} else {
					logger.LogTrace("{line}", line);
				}
			}
		});
	}

	public override async Task StopAsync(CancellationToken cancellationToken) {

		if (Process != null) {
			logger.LogInformation("Send save-all");
			await Process.StandardInput.WriteLineAsync("save-all");

			logger.LogInformation("Send stop");
			await Process.StandardInput.WriteLineAsync("stop");

			logger.LogInformation("Waiting for java.exe to exit");
			await Process.WaitForExitAsync(cancellationToken);
			logger.LogInformation("java.exe has exited");

			await Task.WhenAll(OutputReaderTask!, ErrorReaderTask!);
			logger.LogInformation("Output reader tasks have completed. Service has been stopped.");
		}

		await base.StopAsync(cancellationToken);
	}

}
