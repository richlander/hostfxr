#:project ../src/HostFxrLib/HostFxrLib.csproj

using System.Collections;
using System.Runtime.InteropServices;
using HostFxrLib;

// ============================================================
// dotnet-info: Replicate `dotnet --info` using hostfxr APIs
// ============================================================

if (!HostFxr.IsLoaded)
{
    Console.Error.WriteLine("Error: Could not load hostfxr. Ensure .NET is installed.");
    return 1;
}

string? dotnetRoot = HostFxr.DotnetRoot;

// --- Host ---
var envInfo = HostFxr.GetEnvironmentInfo(dotnetRoot);
Console.WriteLine();
Console.WriteLine("Host:");
Console.WriteLine($"  Version:      {envInfo.HostFxrVersion}");
Console.WriteLine($"  Architecture: {RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}");
Console.WriteLine($"  Commit:       {(envInfo.HostFxrCommitHash.Length > 10 ? envInfo.HostFxrCommitHash[..10] : envInfo.HostFxrCommitHash)}");

// --- .NET SDKs installed ---
Console.WriteLine();
Console.WriteLine(".NET SDKs installed:");
if (envInfo.Sdks.Count == 0)
{
    Console.WriteLine("  No SDKs were found.");
}
else
{
    foreach (var sdk in envInfo.Sdks)
    {
        // hostfxr returns full path (e.g., /dn/sdk/10.0.103); display parent dir like `dotnet --info`
        string displayPath = Path.GetDirectoryName(sdk.Path) ?? sdk.Path;
        Console.WriteLine($"  {sdk.Version} [{displayPath}]");
    }
}

// --- .NET runtimes installed ---
Console.WriteLine();
Console.WriteLine(".NET runtimes installed:");
if (envInfo.Frameworks.Count == 0)
{
    Console.WriteLine("  No runtimes were found.");
}
else
{
    foreach (var fw in envInfo.Frameworks)
    {
        Console.WriteLine($"  {fw.Name} {fw.Version} [{fw.Path}]");
    }
}

// --- Other architectures ---
// Note: hostfxr does not expose an API for querying other architecture installations.
// The native host uses platform-specific registered install location checks.
Console.WriteLine();
Console.WriteLine("Other architectures found:");
Console.WriteLine("  None");

// --- Environment variables ---
Console.WriteLine();
Console.WriteLine("Environment variables:");
var dotnetVars = new SortedDictionary<string, string>(StringComparer.Ordinal);
foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
{
    string name = entry.Key?.ToString() ?? "";
    if (name.StartsWith("DOTNET_", StringComparison.OrdinalIgnoreCase))
        dotnetVars[name] = entry.Value?.ToString() ?? "";
}

if (dotnetVars.Count == 0)
{
    Console.WriteLine("  Not set");
}
else
{
    foreach (var (name, value) in dotnetVars)
    {
        Console.WriteLine($"  {name,-40} [{value}]");
    }
}

// --- global.json ---
Console.WriteLine();
Console.WriteLine("global.json file:");
var sdkResult = HostFxr.ResolveSdkInfo(dotnetRoot, Environment.CurrentDirectory);
switch (sdkResult.GlobalJsonState)
{
    case GlobalJsonState.Valid:
        Console.WriteLine($"  {sdkResult.GlobalJsonPath}");
        break;
    case GlobalJsonState.NotFound:
        Console.WriteLine("  Not found");
        break;
    default:
        Console.WriteLine($"  Invalid [{sdkResult.GlobalJsonPath}]");
        break;
}

// --- Learn more / Download ---
Console.WriteLine();
Console.WriteLine("Learn more:");
Console.WriteLine("  https://aka.ms/dotnet/info");
Console.WriteLine();
Console.WriteLine("Download .NET:");
Console.WriteLine("  https://aka.ms/dotnet/download");

return 0;
