# Minecraft Windows Service

A lightweight Windows Service that runs a Java-based Minecraft server (Paper, Spigot, Vanilla, etc.) as a background process. It starts the server automatically when Windows boots, captures the server console output to the Windows Event Log, and shuts the server down cleanly — running `save-all` and `stop` — when the service is stopped.

Built on .NET 8 (`Microsoft.NET.Sdk.Worker`) using the `BackgroundService` hosting model.

## Features

- Runs your Minecraft server unattended as a Windows Service (`start= auto`).
- Launches `java.exe` with configurable JVM arguments (memory limits, GC flags, etc.).
- Graceful shutdown: sends `save-all` then `stop` to the server console and waits for the JVM to exit before the service stops.
- Pipes server stdout/stderr into the Windows Event Log (stdout at `Trace`, stderr at `Error`).
- Validates configuration on startup (Java path, server directory, jar file) and fails fast with clear log messages.

## Requirements

- Windows (x64)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build (the published service is self-contained for `win-x64`)
- A Java runtime (JDK/JRE) compatible with your Minecraft server version
- A Minecraft server jar (e.g. PaperMC) in a dedicated directory
- Administrator rights to install/uninstall the Windows Service

## Configuration

Settings live in [`appsettings.json`](MinecraftService/appsettings.json) under the `Minecraft` section:

```json
{
  "Minecraft": {
    "JavaHome": "C:\\Program Files\\Microsoft\\jdk-25.0.3.9-hotspot",
    "ServerDirectory": "C:\\MinecraftServer\\PaperMC",
    "JarFileName": "paper-26.1.2-69.jar",
    "AdditionalArgs": "-Xmx8G -Xms8G"
  }
}
```

| Setting | Description |
|---|---|
| `JavaHome` | Path to the Java installation. `java.exe` is expected at `<JavaHome>\bin\java.exe`. If left empty, the `JAVA_HOME` environment variable is used. |
| `ServerDirectory` | Directory containing the server jar; used as the process working directory. |
| `JarFileName` | File name of the server jar inside `ServerDirectory`. |
| `AdditionalArgs` | JVM arguments passed before `-jar` (e.g. memory and GC flags). The server is always launched with `nogui`. |

The effective command line becomes:

```
<JavaHome>\bin\java.exe <AdditionalArgs> -jar <ServerDirectory>\<JarFileName> nogui
```

> A separate [`appsettings.Development.json`](MinecraftService/appsettings.Development.json) is used when running with the `Development` environment.

Remember to accept the Minecraft EULA by setting `eula=true` in `eula.txt` inside your server directory.

## Build

```powershell
# Restore and build
dotnet build MinecraftService/MinecraftService.csproj -c Release

# Publish a self-contained build for installation
dotnet publish MinecraftService/MinecraftService.csproj -c Release -r win-x64 --self-contained
```

The output (including `MinecraftService.exe`, `appsettings.json`, and the install/uninstall scripts) is placed in the build/publish output folder.

## Install as a Windows Service

The repo ships two helper scripts that are copied to the output directory:

- [`Service Install.cmd`](MinecraftService/Service%20Install.cmd) — creates the service:
  ```
  sc.exe create "Minecraft Service" binpath= "%~dp0MinecraftService.exe" start= auto
  ```
- [`Service Uninstall.cmd`](MinecraftService/Service%20Uninstall.cmd) — removes the service:
  ```
  sc.exe delete "Minecraft Service"
  ```

Steps:

1. Build/publish the project.
2. Edit `appsettings.json` in the output folder to match your Java and server paths.
3. Run **`Service Install.cmd` as Administrator** from the output folder.
4. Start the service:
   ```powershell
   sc.exe start "Minecraft Service"
   ```
   (or start it from `services.msc`).

To remove it later, stop the service and run **`Service Uninstall.cmd` as Administrator**.

> Tip: the install script contains a commented line to enable automatic restart on failure:
> `sc.exe failure "Minecraft Service" reset= 0 actions= restart/600000`

## Running / Debugging locally

You can run the worker directly without installing it as a service:

```powershell
dotnet run --project MinecraftService/MinecraftService.csproj
```

## Logs

The service logs to the **Windows Event Log** under the source name **`Minecraft Service`** (Application log). Open **Event Viewer → Windows Logs → Application** to follow startup, the launched command line, server console output, and shutdown progress. Log levels are configured in `appsettings.json`.

## How it works

- [`Program.cs`](MinecraftService/Program.cs) — builds the host, binds the `Minecraft` configuration section into `ServiceSettings`, registers the Windows Service and Event Log logging, and hosts `MinecraftWorker`.
- [`ServiceSettings.cs`](MinecraftService/ServiceSettings.cs) — configuration model plus a `Check` method that validates `JAVA_HOME`/`java.exe` and the server jar before startup.
- [`MinecraftWorker.cs`](MinecraftService/MinecraftWorker.cs) — the `BackgroundService` that starts the Java process, asynchronously reads its output/error streams into the log, and performs a graceful `save-all` + `stop` shutdown on service stop.

## License

Licensed under the [MIT License](LICENSE).
