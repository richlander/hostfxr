# HostFxrLib

Managed wrapper for the [hostfxr](https://github.com/dotnet/runtime/blob/main/docs/design/features/native-hosting.md) native API. Query installed SDKs and runtimes, resolve frameworks, capture error messages, and host .NET components — all from managed code.

## Usage

```csharp
using HostFxrLib;

// Check what's installed
var env = HostFxr.GetEnvironmentInfo();
foreach (var sdk in env.Sdks)
    Console.WriteLine($"SDK {sdk.Version} at {sdk.Path}");

foreach (var fw in env.Frameworks)
    Console.WriteLine($"{fw.Name} {fw.Version}");

// Resolve SDK from global.json
var sdk = HostFxr.ResolveSdkInfo(workingDir: Environment.CurrentDirectory);
Console.WriteLine($"Resolved SDK: {sdk.ResolvedSdkDir}");

// Check if a runtime config can be satisfied
var result = HostFxr.ResolveFrameworks("app.runtimeconfig.json");
if (result.Unresolved.Count > 0)
    Console.WriteLine($"Missing: {result.Unresolved[0].Name} {result.Unresolved[0].RequestedVersion}");

// Capture hostfxr error messages
using var errors = HostFxr.CaptureErrors();
var frameworks = HostFxr.ResolveFrameworks("missing.runtimeconfig.json");
foreach (var msg in errors.Drain())
    Console.WriteLine(msg);

// Host a .NET component
using var ctx = HostFxr.InitializeForRuntimeConfig("component.runtimeconfig.json");
if (HostFxrStatus.IsSuccess(ctx.StatusCode))
{
    ctx.GetRuntimeProperties(out var props);
    foreach (var (key, value) in props)
        Console.WriteLine($"  {key} = {value}");
}
```

## API Surface

**High-level APIs** (managed types, automatic marshalling):
- `HostFxr.GetEnvironmentInfo()` — installed SDKs and runtimes
- `HostFxr.ResolveSdkInfo()` — SDK resolution with global.json support
- `HostFxr.GetAvailableSdkDirs()` — all installed SDK paths
- `HostFxr.ResolveFrameworks()` — framework resolution for a runtime config
- `HostFxr.InitializeForRuntimeConfig()` — host context for a runtime config
- `HostFxr.InitializeForCommandLine()` — host context for command line execution
- `HostFxr.CaptureErrors()` — scoped error message capture with `Drain()`

**HostContextHandle** instance methods:
- `RunApp()`, `GetRuntimeDelegate()`, `GetRuntimePropertyValue()`, `SetRuntimePropertyValue()`, `GetRuntimeProperties()`

**Raw APIs** — all 18 hostfxr exports are available as `[LibraryImport]` methods for advanced scenarios.

## Requirements

- .NET 10+
- A .NET runtime installation (hostfxr is discovered automatically via `DOTNET_ROOT` or `PATH`)

## License

MIT
