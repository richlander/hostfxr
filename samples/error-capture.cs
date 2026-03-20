#:property AllowUnsafeBlocks=true
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
    int rc2 = InitializeForConfig(missingVersionConfig, out nint handle2);
    string[] msgs2 = errors.Drain();
    Console.WriteLine($"   rc=0x{rc2:X8}");
    PrintErrors(msgs2);
    if (handle2 != 0) HostFxr.Close(handle2);

    // 3. Another successful call — proves Drain() cleared the previous errors
    Console.WriteLine();
    Console.WriteLine("3. GetEnvironmentInfo again (should succeed, 0 errors)");
    _ = HostFxr.GetEnvironmentInfo();
    string[] msgs3 = errors.Drain();
    PrintErrors(msgs3);

    // 4. Bogus framework name — different error than #2
    Console.WriteLine();
    Console.WriteLine("4. Initialize with Microsoft.BogusFramework.App (unknown framework)");
    int rc4 = InitializeForConfig(bogusFrameworkConfig, out nint handle4);
    string[] msgs4 = errors.Drain();
    Console.WriteLine($"   rc=0x{rc4:X8}");
    PrintErrors(msgs4);
    if (handle4 != 0) HostFxr.Close(handle4);

    // 5. Config file doesn't exist at all
    Console.WriteLine();
    Console.WriteLine("5. Initialize with non-existent config file");
    int rc5 = InitializeForConfig(nonExistentConfig, out nint handle5);
    string[] msgs5 = errors.Drain();
    Console.WriteLine($"   rc=0x{rc5:X8}");
    PrintErrors(msgs5);
    if (handle5 != 0) HostFxr.Close(handle5);
}
finally
{
    Directory.Delete(tempDir, recursive: true);
}

return 0;

static unsafe int InitializeForConfig(string configPath, out nint handle)
{
    nint pathPtr = PlatformStringMarshaller.ConvertToUnmanaged(configPath);
    try
    {
        nint h = 0;
        int rc = HostFxr.InitializeForRuntimeConfig(pathPtr, null, &h);
        handle = h;
        return rc;
    }
    finally
    {
        PlatformStringMarshaller.Free(pathPtr);
    }
}

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
