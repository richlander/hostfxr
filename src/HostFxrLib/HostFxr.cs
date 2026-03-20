using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HostFxrLib;

/// <summary>
/// Provides access to all hostfxr native APIs.
/// Loads the library dynamically via NativeLibrary for NativeAOT compatibility.
/// </summary>
public static unsafe class HostFxr
{
    private static string? s_dotnetRoot;

    // Intentionally never freed — hostfxr lives for the process lifetime.
    private static nint s_handle;

    static HostFxr()
    {
        try
        {
            s_dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");

            if (string.IsNullOrEmpty(s_dotnetRoot))
            {
                string dotnetExe = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
                string? pathVar = Environment.GetEnvironmentVariable("PATH");
                if (pathVar is not null)
                {
                    char sep = OperatingSystem.IsWindows() ? ';' : ':';
                    foreach (string dir in pathVar.Split(sep, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string candidate = Path.Combine(dir, dotnetExe);
                        if (File.Exists(candidate))
                        {
                            var info = new FileInfo(candidate);
                            string resolved = info.ResolveLinkTarget(returnFinalTarget: true)?.FullName
                                ?? candidate;
                            s_dotnetRoot = Path.GetDirectoryName(resolved);
                            break;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(s_dotnetRoot))
            {
                if (OperatingSystem.IsMacOS())
                    s_dotnetRoot = "/usr/local/share/dotnet";
                else if (OperatingSystem.IsWindows())
                    s_dotnetRoot = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet");
                else
                    s_dotnetRoot = "/usr/share/dotnet";
            }

            string? path = FindHostFxrPath();
            if (path is not null)
                NativeLibrary.TryLoad(path, out s_handle);
        }
        catch
        {
            // Swallow filesystem/permission errors so the type remains usable.
            // IsLoaded will be false and callers can check that.
        }
    }

    /// <summary>The discovered dotnet root directory.</summary>
    public static string? DotnetRoot => s_dotnetRoot;

    /// <summary>Whether hostfxr was successfully loaded.</summary>
    public static bool IsLoaded => s_handle != 0;

    // ========================================================================
    // Raw API wrappers — all 18 hostfxr exports
    // ========================================================================

    // 1. hostfxr_main
    public static int Main(int argc, nint argv)
    {
        nint fn = GetExport("hostfxr_main");
        return ((delegate* unmanaged[Cdecl]<int, nint, int>)fn)(argc, argv);
    }

    // 2. hostfxr_main_startupinfo
    public static int MainStartupInfo(int argc, nint argv, nint hostPath, nint dotnetRoot, nint appPath)
    {
        nint fn = GetExport("hostfxr_main_startupinfo");
        return ((delegate* unmanaged[Cdecl]<int, nint, nint, nint, nint, int>)fn)(
            argc, argv, hostPath, dotnetRoot, appPath);
    }

    // 3. hostfxr_main_bundle_startupinfo
    public static int MainBundleStartupInfo(int argc, nint argv, nint hostPath, nint dotnetRoot, nint appPath, long bundleHeaderOffset)
    {
        nint fn = GetExport("hostfxr_main_bundle_startupinfo");
        return ((delegate* unmanaged[Cdecl]<int, nint, nint, nint, nint, long, int>)fn)(
            argc, argv, hostPath, dotnetRoot, appPath, bundleHeaderOffset);
    }

    // 4. hostfxr_resolve_sdk (obsolete — use ResolveSdk2)
    [Obsolete("Use ResolveSdk2 instead")]
    public static int ResolveSdk(nint exeDir, nint workingDir, nint buffer, int bufferSize)
    {
        nint fn = GetExport("hostfxr_resolve_sdk");
        return ((delegate* unmanaged[Cdecl]<nint, nint, nint, int, int>)fn)(
            exeDir, workingDir, buffer, bufferSize);
    }

    // 5. hostfxr_resolve_sdk2
    public static int ResolveSdk2(nint exeDir, nint workingDir, int flags,
        delegate* unmanaged[Cdecl]<int, nint, void> result)
    {
        nint fn = GetExport("hostfxr_resolve_sdk2");
        return ((delegate* unmanaged[Cdecl]<nint, nint, int,
            delegate* unmanaged[Cdecl]<int, nint, void>, int>)fn)(
            exeDir, workingDir, flags, result);
    }

    // 6. hostfxr_get_available_sdks
    public static int GetAvailableSdks(nint exeDir,
        delegate* unmanaged[Cdecl]<int, nint, void> result)
    {
        nint fn = GetExport("hostfxr_get_available_sdks");
        return ((delegate* unmanaged[Cdecl]<nint,
            delegate* unmanaged[Cdecl]<int, nint, void>, int>)fn)(exeDir, result);
    }

    // 7. hostfxr_get_dotnet_environment_info
    public static int GetDotnetEnvironmentInfo(nint dotnetRoot, nint reserved,
        delegate* unmanaged[Cdecl]<nint, nint, void> result, nint resultContext)
    {
        nint fn = GetExport("hostfxr_get_dotnet_environment_info");
        return ((delegate* unmanaged[Cdecl]<nint, nint,
            delegate* unmanaged[Cdecl]<nint, nint, void>, nint, int>)fn)(
            dotnetRoot, reserved, result, resultContext);
    }

    // 8. hostfxr_get_native_search_directories
    public static int GetNativeSearchDirectories(int argc, nint argv, nint buffer, int bufferSize, int* requiredBufferSize)
    {
        nint fn = GetExport("hostfxr_get_native_search_directories");
        return ((delegate* unmanaged[Cdecl]<int, nint, nint, int, int*, int>)fn)(
            argc, argv, buffer, bufferSize, requiredBufferSize);
    }

    // 9. hostfxr_set_error_writer
    public static nint SetErrorWriter(delegate* unmanaged[Cdecl]<nint, void> errorWriter)
    {
        nint fn = GetExport("hostfxr_set_error_writer");
        return ((delegate* unmanaged[Cdecl]<
            delegate* unmanaged[Cdecl]<nint, void>, nint>)fn)(errorWriter);
    }

    // 10. hostfxr_initialize_for_dotnet_command_line
    public static int InitializeForDotnetCommandLine(int argc, nint argv,
        HostFxrInitializeParameters* parameters, nint* hostContextHandle)
    {
        nint fn = GetExport("hostfxr_initialize_for_dotnet_command_line");
        return ((delegate* unmanaged[Cdecl]<int, nint, HostFxrInitializeParameters*, nint*, int>)fn)(
            argc, argv, parameters, hostContextHandle);
    }

    // 11. hostfxr_initialize_for_runtime_config
    public static int InitializeForRuntimeConfig(nint runtimeConfigPath,
        HostFxrInitializeParameters* parameters, nint* hostContextHandle)
    {
        nint fn = GetExport("hostfxr_initialize_for_runtime_config");
        return ((delegate* unmanaged[Cdecl]<nint, HostFxrInitializeParameters*, nint*, int>)fn)(
            runtimeConfigPath, parameters, hostContextHandle);
    }

    // 12. hostfxr_resolve_frameworks_for_runtime_config
    public static int ResolveFrameworksForRuntimeConfig(nint runtimeConfigPath, nint parameters,
        delegate* unmanaged[Cdecl]<nint, nint, void> callback, nint resultContext)
    {
        nint fn = GetExport("hostfxr_resolve_frameworks_for_runtime_config");
        return ((delegate* unmanaged[Cdecl]<nint, nint,
            delegate* unmanaged[Cdecl]<nint, nint, void>, nint, int>)fn)(
            runtimeConfigPath, parameters, callback, resultContext);
    }

    // 13. hostfxr_run_app
    public static int RunApp(nint hostContextHandle)
    {
        nint fn = GetExport("hostfxr_run_app");
        return ((delegate* unmanaged[Cdecl]<nint, int>)fn)(hostContextHandle);
    }

    // 14. hostfxr_get_runtime_delegate
    public static int GetRuntimeDelegate(nint hostContextHandle, int type, nint* @delegate)
    {
        nint fn = GetExport("hostfxr_get_runtime_delegate");
        return ((delegate* unmanaged[Cdecl]<nint, int, nint*, int>)fn)(
            hostContextHandle, type, @delegate);
    }

    // 15. hostfxr_get_runtime_property_value
    public static int GetRuntimePropertyValue(nint hostContextHandle, nint name, nint* value)
    {
        nint fn = GetExport("hostfxr_get_runtime_property_value");
        return ((delegate* unmanaged[Cdecl]<nint, nint, nint*, int>)fn)(
            hostContextHandle, name, value);
    }

    // 16. hostfxr_set_runtime_property_value
    public static int SetRuntimePropertyValue(nint hostContextHandle, nint name, nint value)
    {
        nint fn = GetExport("hostfxr_set_runtime_property_value");
        return ((delegate* unmanaged[Cdecl]<nint, nint, nint, int>)fn)(
            hostContextHandle, name, value);
    }

    // 17. hostfxr_get_runtime_properties
    public static int GetRuntimeProperties(nint hostContextHandle, nuint* count, nint* keys, nint* values)
    {
        nint fn = GetExport("hostfxr_get_runtime_properties");
        return ((delegate* unmanaged[Cdecl]<nint, nuint*, nint*, nint*, int>)fn)(
            hostContextHandle, count, keys, values);
    }

    // 18. hostfxr_close
    public static int Close(nint hostContextHandle)
    {
        nint fn = GetExport("hostfxr_close");
        return ((delegate* unmanaged[Cdecl]<nint, int>)fn)(hostContextHandle);
    }

    // ========================================================================
    // High-level managed APIs
    // ========================================================================

    /// <summary>
    /// Get comprehensive .NET environment info (SDKs, runtimes, hostfxr version).
    /// </summary>
    public static EnvironmentInfo GetEnvironmentInfo(string? dotnetRoot = null)
    {
        ThrowIfNotLoaded();

        dotnetRoot ??= s_dotnetRoot;

        var handle = GCHandle.Alloc(new StrongBox<EnvironmentInfo?>(null));
        nint rootPtr = dotnetRoot is not null ? PlatformStringMarshaller.ConvertToUnmanaged(dotnetRoot) : 0;
        try
        {
            int rc = GetDotnetEnvironmentInfo(rootPtr, 0, &OnEnvironmentInfo, (nint)handle);
            var result = ((StrongBox<EnvironmentInfo?>)handle.Target!).Value ?? new EnvironmentInfo();
            result.StatusCode = rc;
            return result;
        }
        finally
        {
            PlatformStringMarshaller.Free(rootPtr);
            handle.Free();
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnEnvironmentInfo(nint infoPtr, nint context)
    {
        ref var info = ref Unsafe.AsRef<DotnetEnvironmentInfo>((void*)infoPtr);
        var box = (StrongBox<EnvironmentInfo?>)GCHandle.FromIntPtr(context).Target!;

        var sdks = new SdkInfo[(int)info.SdkCount];
        for (int i = 0; i < sdks.Length; i++)
        {
            ref var sdk = ref info.Sdks[i];
            sdks[i] = new SdkInfo(sdk.Version, sdk.Path);
        }

        var frameworks = new FrameworkInfo[(int)info.FrameworkCount];
        for (int i = 0; i < frameworks.Length; i++)
        {
            ref var fw = ref info.Frameworks[i];
            frameworks[i] = new FrameworkInfo(fw.Name, fw.Version, fw.Path);
        }

        box.Value = new EnvironmentInfo
        {
            HostFxrVersion = info.HostFxrVersion,
            HostFxrCommitHash = info.HostFxrCommitHash,
            Sdks = sdks,
            Frameworks = frameworks,
        };
    }

    /// <summary>
    /// Resolve SDK location and global.json info.
    /// </summary>
    /// <remarks>
    /// Uses thread-local storage because hostfxr_resolve_sdk2's callback has no context
    /// parameter. This assumes hostfxr invokes the callback on the calling thread.
    /// </remarks>
    [ThreadStatic]
    private static SdkResolutionResult? t_sdkResult;

    public static SdkResolutionResult ResolveSdkInfo(string? exeDir = null, string? workingDir = null,
        ResolveSdk2Flags flags = ResolveSdk2Flags.None)
    {
        ThrowIfNotLoaded();

        var result = new SdkResolutionResult();
        t_sdkResult = result;

        nint exeDirPtr = exeDir is not null ? PlatformStringMarshaller.ConvertToUnmanaged(exeDir) : 0;
        nint workingDirPtr = workingDir is not null ? PlatformStringMarshaller.ConvertToUnmanaged(workingDir) : 0;
        try
        {
            result.StatusCode = ResolveSdk2(exeDirPtr, workingDirPtr, (int)flags, &OnResolveSdk2Result);
        }
        finally
        {
            PlatformStringMarshaller.Free(exeDirPtr);
            PlatformStringMarshaller.Free(workingDirPtr);
            t_sdkResult = null;
        }

        return result;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnResolveSdk2Result(int key, nint value)
    {
        var result = t_sdkResult;
        if (result is null) return;

        string? str = PlatformStringMarshaller.ConvertToManaged(value);
        switch ((ResolveSdk2ResultKey)key)
        {
            case ResolveSdk2ResultKey.ResolvedSdkDir:
                result.ResolvedSdkDir = str;
                break;
            case ResolveSdk2ResultKey.GlobalJsonPath:
                result.GlobalJsonPath = str;
                break;
            case ResolveSdk2ResultKey.RequestedVersion:
                result.RequestedVersion = str;
                break;
            case ResolveSdk2ResultKey.GlobalJsonState:
                result.GlobalJsonState = str switch
                {
                    "not_found" => GlobalJsonState.NotFound,
                    "valid" => GlobalJsonState.Valid,
                    "__invalid_data_no_fallback" => GlobalJsonState.InvalidDataNoFallback,
                    "invalid_json" => GlobalJsonState.InvalidJson,
                    _ => GlobalJsonState.Unknown,
                };
                break;
        }
    }

    /// <summary>
    /// Get all available SDK directories.
    /// </summary>
    /// <remarks>
    /// Uses thread-local storage because hostfxr_get_available_sdks's callback has no context
    /// parameter. This assumes hostfxr invokes the callback on the calling thread.
    /// </remarks>
    [ThreadStatic]
    private static List<string>? t_sdkDirs;

    public static AvailableSdksResult GetAvailableSdkDirs(string? exeDir = null)
    {
        ThrowIfNotLoaded();

        t_sdkDirs = [];
        nint exeDirPtr = exeDir is not null ? PlatformStringMarshaller.ConvertToUnmanaged(exeDir) : 0;
        try
        {
            int rc = GetAvailableSdks(exeDirPtr, &OnAvailableSdks);
            return new AvailableSdksResult { StatusCode = rc, SdkDirs = t_sdkDirs.ToArray() };
        }
        finally
        {
            PlatformStringMarshaller.Free(exeDirPtr);
            t_sdkDirs = null;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnAvailableSdks(int sdkCount, nint sdkDirs)
    {
        var list = t_sdkDirs;
        if (list is null) return;

        for (int i = 0; i < sdkCount; i++)
        {
            nint ptr = ((nint*)sdkDirs)[i];
            string? dir = PlatformStringMarshaller.ConvertToManaged(ptr);
            if (dir is not null)
                list.Add(dir);
        }
    }

    /// <summary>
    /// Resolve frameworks for a runtime config file.
    /// </summary>
    public static FrameworkResolutionResult ResolveFrameworks(string runtimeConfigPath, string? dotnetRoot = null)
    {
        ArgumentNullException.ThrowIfNull(runtimeConfigPath);
        ThrowIfNotLoaded();

        FrameworkResolutionResult? result = null;
        var handle = GCHandle.Alloc(new StrongBox<FrameworkResolutionResult?>(null));

        nint pathPtr = PlatformStringMarshaller.ConvertToUnmanaged(runtimeConfigPath);
        nint rootPtr = dotnetRoot is not null ? PlatformStringMarshaller.ConvertToUnmanaged(dotnetRoot) : 0;

        nint paramsPtr = 0;
        HostFxrInitializeParameters initParams = default;
        if (rootPtr != 0)
        {
            initParams = HostFxrInitializeParameters.Create(0, rootPtr);
            paramsPtr = (nint)(&initParams);
        }

        try
        {
            int rc = ResolveFrameworksForRuntimeConfig(pathPtr, paramsPtr, &OnResolveFrameworks, (nint)handle);
            result = ((StrongBox<FrameworkResolutionResult?>)handle.Target!).Value ?? new FrameworkResolutionResult();
            result.StatusCode = rc;
        }
        finally
        {
            PlatformStringMarshaller.Free(pathPtr);
            PlatformStringMarshaller.Free(rootPtr);
            handle.Free();
        }

        return result;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnResolveFrameworks(nint resultPtr, nint context)
    {
        ref var r = ref Unsafe.AsRef<ResolveFrameworksResult>((void*)resultPtr);
        var box = (StrongBox<FrameworkResolutionResult?>)GCHandle.FromIntPtr(context).Target!;

        var resolved = new ResolvedFramework[(int)r.ResolvedCount];
        for (int i = 0; i < resolved.Length; i++)
        {
            ref var fw = ref r.ResolvedFrameworks[i];
            resolved[i] = new ResolvedFramework(fw.Name, fw.RequestedVersion, fw.ResolvedVersion, fw.ResolvedPath);
        }

        var unresolved = new ResolvedFramework[(int)r.UnresolvedCount];
        for (int i = 0; i < unresolved.Length; i++)
        {
            ref var fw = ref r.UnresolvedFrameworks[i];
            unresolved[i] = new ResolvedFramework(fw.Name, fw.RequestedVersion, fw.ResolvedVersion, fw.ResolvedPath);
        }

        box.Value = new FrameworkResolutionResult { Resolved = resolved, Unresolved = unresolved };
    }

    /// <summary>
    /// Begin capturing hostfxr error messages. Dispose to restore the previous writer.
    /// </summary>
    public static ErrorCapture CaptureErrors() => new();

    // ========================================================================
    // Library discovery & loading
    // ========================================================================

    private static string? FindHostFxrPath()
    {
        if (s_dotnetRoot is null) return null;

        string fxrDir = Path.Combine(s_dotnetRoot, "host", "fxr");
        if (!Directory.Exists(fxrDir)) return null;

        string? best = Directory.GetDirectories(fxrDir)
            .OrderByDescending(d => Version.TryParse(Path.GetFileName(d), out var v) ? v : new Version())
            .FirstOrDefault();

        if (best is null) return null;

        string libName = OperatingSystem.IsWindows() ? "hostfxr.dll" :
                         OperatingSystem.IsMacOS() ? "libhostfxr.dylib" :
                         "libhostfxr.so";

        string libPath = Path.Combine(best, libName);
        return File.Exists(libPath) ? libPath : null;
    }

    // ========================================================================
    // Internal helpers
    // ========================================================================

    private static nint GetExport(string name)
    {
        ThrowIfNotLoaded();

        if (!NativeLibrary.TryGetExport(s_handle, name, out nint fn))
            throw new EntryPointNotFoundException($"hostfxr export not found: {name}");

        return fn;
    }

    private static void ThrowIfNotLoaded()
    {
        if (s_handle == 0)
            throw new DllNotFoundException(
                "hostfxr could not be loaded. Ensure .NET is installed and DOTNET_ROOT is set.");
    }
}
