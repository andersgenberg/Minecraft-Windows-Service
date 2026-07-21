namespace MinecraftService;

public class ServiceSettings {

	/// <summary>
	/// Path to the Java installation directory. If not set, will attempt to use the JAVA_HOME environment variable.
	/// </summary>
	public string? JavaHome { get; set; }

	/// <summary>
	/// Path to the Minecraft server directory. This should be the directory containing the server jar file.
	/// </summary>
	public string? ServerDirectory { get; set; }

	/// <summary>
	/// Name of the Minecraft server jar file.
	/// </summary>
	public string? JarFileName { get; set; }

	/// <summary>
	/// Additional command line arguments to pass to the Java process when starting the Minecraft server. 
	/// This can include things like memory settings (e.g. -Xmx8G -Xms8G) or other JVM options.
	/// </summary>
	public string AdditionalArgs { get; set; } = "";

	/// <summary>
	/// Full path to the java.exe executable. This is determined based on the JavaHome property and is used to start the Minecraft server process.
	/// </summary>
	public string? JavaExe { get; private set; }

	/// <summary>
	/// Full path to the Minecraft server jar file. This is determined based on the ServerDirectory and JarFileName properties.
	/// </summary>
	public string? JarFullPath { get; private set; }

	/// <summary>
	/// Checks that the settings are valid and that the necessary files exist. 
	/// This includes checking that JavaHome is set and points to a valid java.exe, and that the Minecraft server jar file exists in the specified ServerDirectory.
	/// </summary>
	/// <param name="logger">The logger to use for logging errors and information.</param>
	/// <returns>True if the settings are valid and the necessary files exist; otherwise, false.</returns>
	public bool Check(ILogger logger) {

		// Check java
		JavaHome ??= Environment.GetEnvironmentVariable("JAVA_HOME");
		logger.LogInformation("JAVA_HOME: '{home}'", JavaHome);
		if (string.IsNullOrEmpty(JavaHome)) {
			logger.LogError("JAVA_HOME is not set");
			return false;
		}

		JavaExe = Path.Combine(JavaHome, "bin", "java.exe");
		if (!File.Exists(JavaExe)) {
			logger.LogError("java.exe not found in JAVA_HOME");
			return false;
		}

		if (string.IsNullOrEmpty(ServerDirectory)) {
			logger.LogError("ServerDirectory is not set");
			return false;
		}

		if (string.IsNullOrEmpty(JarFileName)) {
			logger.LogError("JarFileName is not set");
			return false;
		}

		logger.LogInformation("ServerDirectory: {ServerDirectory}, JarFileName: {JarFileName}", ServerDirectory, JarFileName);
		JarFullPath = Path.Combine(ServerDirectory, JarFileName);
		if (!File.Exists(JarFullPath)) {
			logger.LogError("Server jar file not found");
			return false;
		}

		return true;
	}
}
