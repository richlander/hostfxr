#:project ../src/HostFxrLib/HostFxrLib.csproj

using HostFxrLib;

// ============================================================
// error-capture: Demonstrate attributing hostfxr errors to calls
//
// Multiple hostfxr calls in one ErrorCapture scope — some succeed,
// some fail for different reasons. Drain() after each call proves
// the messages belong to the preceding call, not a shared pile.
// ============================================================

if (!HostFxr.IsLoaded)
{
    Console.Error.WriteLine("Error: Could not load hostfxr.");
    return 1;
}

// Set up two configs that fail for different reasons
string tempDir = Path.Combine(Path.GetTempPath(), "hostfxrlib-error-demo");
Directory.CreateDirectory(tempDir);

string missingVersionConfig = Path.Combine(tempDir, "missing-version.runtimeconfig.json");
File.WriteAllText(missingVersionConfig, """
    {
      "runtimeOptions": {
        "tfm": "net99.0",
        "framework": {
          "name": "Microsoft.NETCore.App",
          "version": "99.0.0"
        }
      }
    }
    """);

string bogusFrameworkConfig = Path.Combine(tempDir, "bogus-framework.runtimeconfig.json");
File.WriteAllText(bogusFrameworkConfig, """
    {
      "runtimeOptions": {
        "tfm": "net10.0",
        "framework": {
          "name": "Microsoft.BogusFramework.App",
          "version": "1.0.0"
        }
      }
    }
    """);

string nonExistentConfig = Path.Combine(tempDir, "does-not-exist.runtimeconfig.json");

try
{
    using var errors = HostFxr.CaptureErrors();

    // 1. Successful call — should produce no errors
    Console.WriteLine("1. GetEnvironmentInfo (should succeed)");
    var envInfo = HostFxr.GetEnvironmentInfo();
    string[] msgs1 = errors.Drain();
    Console.WriteLine($"   Found {envInfo.Sdks.Count} SDK(s), {envInfo.Frameworks.Count} runtime(s)");
    PrintErrors(msgs1);

    // 2. Missing framework version — hostfxr lists what's installed
    Console.WriteLine();
    Console.WriteLine("2. Initialize with Microsoft.NETCore.App v99.0.0 (missing version)");
    using var ctx2 = HostFxr.InitializeForRuntimeConfig(missingVersionConfig);
    string[] msgs2 = errors.Drain();
    Console.WriteLine($"   rc=0x{ctx2.StatusCode:X8}");
    PrintErrors(msgs2);

    // 3. Another successful call — proves Drain() cleared the previous errors
    Console.WriteLine();
    Console.WriteLine("3. GetEnvironmentInfo again (should succeed, 0 errors)");
    _ = HostFxr.GetEnvironmentInfo();
    string[] msgs3 = errors.Drain();
    PrintErrors(msgs3);

    // 4. Bogus framework name — different error than #2
    Console.WriteLine();
    Console.WriteLine("4. Initialize with Microsoft.BogusFramework.App (unknown framework)");
    using var ctx4 = HostFxr.InitializeForRuntimeConfig(bogusFrameworkConfig);
    string[] msgs4 = errors.Drain();
    Console.WriteLine($"   rc=0x{ctx4.StatusCode:X8}");
    PrintErrors(msgs4);

    // 5. Config file doesn't exist at all
    Console.WriteLine();
    Console.WriteLine("5. Initialize with non-existent config file");
    using var ctx5 = HostFxr.InitializeForRuntimeConfig(nonExistentConfig);
    string[] msgs5 = errors.Drain();
    Console.WriteLine($"   rc=0x{ctx5.StatusCode:X8}");
    PrintErrors(msgs5);
}
finally
{
    Directory.Delete(tempDir, recursive: true);
}

return 0;

static void PrintErrors(string[] messages)
{
    if (messages.Length == 0)
    {
        Console.WriteLine("   (no errors)");
        return;
    }

    Console.WriteLine($"   hostfxr errors ({messages.Length}):");
    foreach (var msg in messages)
        Console.WriteLine($"     {msg}");
}
