using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HostFxrLib;

// ---- Enums ----

/// <summary>
/// Delegate types for <see cref="HostFxr.GetRuntimeDelegate"/>.
/// </summary>
public enum HostFxrDelegateType
{
    ComActivation,
    LoadInMemoryAssembly,
    WinRtActivation,
    ComRegister,
    ComUnregister,
    LoadAssemblyAndGetFunctionPointer,
    GetFunctionPointer,
    LoadAssembly,
    LoadAssemblyBytes,
}

/// <summary>
/// Flags for <see cref="HostFxr.ResolveSdk2"/>.
/// </summary>
[Flags]
public enum ResolveSdk2Flags : int
{
    None = 0,
    DisallowPrerelease = 0x1,
}

/// <summary>
/// Key types returned by the <see cref="HostFxr.ResolveSdk2"/> callback.
/// </summary>
public enum ResolveSdk2ResultKey : int
{
    ResolvedSdkDir = 0,
    GlobalJsonPath = 1,
    RequestedVersion = 2,
    GlobalJsonState = 3,
}

/// <summary>
/// State of the global.json file as reported by <see cref="HostFxr.ResolveSdk2"/>.
/// </summary>
public enum GlobalJsonState
{
    /// <summary>No global.json was found.</summary>
    NotFound,
    /// <summary>global.json was found and the requested SDK resolved.</summary>
    Valid,
    /// <summary>global.json was found but the requested SDK is not installed, and rollforward is disallowed.</summary>
    InvalidDataNoFallback,
    /// <summary>global.json exists but contains invalid JSON.</summary>
    InvalidJson,
    /// <summary>The state string from hostfxr was not recognized.</summary>
    Unknown,
}

// ---- Native structs ----

/// <summary>
/// Parameters for hostfxr initialization functions.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct HostFxrInitializeParameters
{
    public nuint Size;
    public nint HostPath;
    public nint DotnetRoot;

    public static HostFxrInitializeParameters Create(nint hostPath, nint dotnetRoot) => new()
    {
        Size = (nuint)Unsafe.SizeOf<HostFxrInitializeParameters>(),
        HostPath = hostPath,
        DotnetRoot = dotnetRoot,
    };
}

/// <summary>
/// SDK info returned by <see cref="HostFxr.GetDotnetEnvironmentInfo"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DotnetEnvironmentSdkInfo
{
    public nuint Size;
    public PlatformString Version;
    public PlatformString Path;
}

/// <summary>
/// Framework/runtime info returned by <see cref="HostFxr.GetDotnetEnvironmentInfo"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DotnetEnvironmentFrameworkInfo
{
    public nuint Size;
    public PlatformString Name;
    public PlatformString Version;
    public PlatformString Path;
}

/// <summary>
/// Complete environment info returned by <see cref="HostFxr.GetDotnetEnvironmentInfo"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct DotnetEnvironmentInfo
{
    public nuint Size;
    public PlatformString HostFxrVersion;
    public PlatformString HostFxrCommitHash;
    public nuint SdkCount;
    public DotnetEnvironmentSdkInfo* Sdks;
    public nuint FrameworkCount;
    public DotnetEnvironmentFrameworkInfo* Frameworks;
}

/// <summary>
/// Individual framework resolution result.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct FrameworkResult
{
    public nuint Size;
    public PlatformString Name;
    public PlatformString RequestedVersion;
    public PlatformString ResolvedVersion;
    public PlatformString ResolvedPath;
}

/// <summary>
/// Result of <see cref="HostFxr.ResolveFrameworksForRuntimeConfig"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ResolveFrameworksResult
{
    public nuint Size;
    public nuint ResolvedCount;
    public FrameworkResult* ResolvedFrameworks;
    public nuint UnresolvedCount;
    public FrameworkResult* UnresolvedFrameworks;
}

// ---- Status codes ----

/// <summary>
/// Helpers and well-known constants for hostfxr status codes.
/// Values match the native StatusCode enum in error_codes.h.
/// </summary>
public static class HostFxrStatus
{
    // Success codes (non-negative)
    public const int Success = 0;
    public const int SuccessHostAlreadyInitialized = 0x00000001;
    public const int SuccessDifferentRuntimeProperties = 0x00000002;

    // Error codes
    public const int InvalidArgFailure = unchecked((int)0x80008081);
    public const int CoreHostLibLoadFailure = unchecked((int)0x80008082);
    public const int CoreHostLibMissingFailure = unchecked((int)0x80008083);
    public const int CoreHostEntryPointFailure = unchecked((int)0x80008084);
    public const int CurrentHostFindFailure = unchecked((int)0x80008085);
    public const int CoreClrResolveFailure = unchecked((int)0x80008087);
    public const int CoreClrBindFailure = unchecked((int)0x80008088);
    public const int CoreClrInitFailure = unchecked((int)0x80008089);
    public const int CoreClrExeFailure = unchecked((int)0x8000808a);
    public const int ResolverInitFailure = unchecked((int)0x8000808b);
    public const int ResolverResolveFailure = unchecked((int)0x8000808c);
    public const int LibHostInitFailure = unchecked((int)0x8000808e);
    public const int LibHostInvalidArgs = unchecked((int)0x80008092);
    public const int InvalidConfigFile = unchecked((int)0x80008093);
    public const int AppArgNotRunnable = unchecked((int)0x80008094);
    public const int AppHostExeNotBoundFailure = unchecked((int)0x80008095);
    public const int FrameworkMissingFailure = unchecked((int)0x80008096);
    public const int HostApiFailed = unchecked((int)0x80008097);
    public const int HostApiBufferTooSmall = unchecked((int)0x80008098);
    public const int AppPathFindFailure = unchecked((int)0x8000809a);
    public const int SdkResolveFailure = unchecked((int)0x8000809b);
    public const int FrameworkCompatFailure = unchecked((int)0x8000809c);
    public const int FrameworkCompatRetry = unchecked((int)0x8000809d);
    public const int BundleExtractionFailure = unchecked((int)0x8000809f);
    public const int BundleExtractionIOError = unchecked((int)0x800080a0);
    public const int LibHostDuplicateProperty = unchecked((int)0x800080a1);
    public const int HostApiUnsupportedVersion = unchecked((int)0x800080a2);
    public const int HostInvalidState = unchecked((int)0x800080a3);
    public const int HostPropertyNotFound = unchecked((int)0x800080a4);
    public const int HostIncompatibleConfig = unchecked((int)0x800080a5);
    public const int HostApiUnsupportedScenario = unchecked((int)0x800080a6);
    public const int HostFeatureDisabled = unchecked((int)0x800080a7);

    /// <summary>Returns true when the status code indicates success (non-negative).</summary>
    public static bool IsSuccess(int statusCode) => statusCode >= 0;
}

// ---- High-level result types ----

/// <summary>
/// Managed representation of an installed SDK.
/// </summary>
public sealed record SdkInfo(string Version, string Path);

/// <summary>
/// Managed representation of an installed runtime/framework.
/// </summary>
public sealed record FrameworkInfo(string Name, string Version, string Path);

/// <summary>
/// Managed representation of the complete .NET environment info.
/// </summary>
public sealed class EnvironmentInfo
{
    public int StatusCode { get; internal set; }
    public string HostFxrVersion { get; init; } = "";
    public string HostFxrCommitHash { get; init; } = "";
    public IReadOnlyList<SdkInfo> Sdks { get; init; } = [];
    public IReadOnlyList<FrameworkInfo> Frameworks { get; init; } = [];
}

/// <summary>
/// Result of SDK resolution via <see cref="HostFxr.ResolveSdk2"/>.
/// </summary>
public sealed class SdkResolutionResult
{
    internal SdkResolutionResult() { }
    public int StatusCode { get; internal set; }
    public string? ResolvedSdkDir { get; internal set; }
    public string? GlobalJsonPath { get; internal set; }
    public string? RequestedVersion { get; internal set; }
    public GlobalJsonState GlobalJsonState { get; internal set; }
}

/// <summary>
/// Result of framework resolution.
/// </summary>
public sealed class FrameworkResolutionResult
{
    internal FrameworkResolutionResult() { }
    public int StatusCode { get; internal set; }
    public IReadOnlyList<FrameworkEntry> Resolved { get; init; } = [];
    public IReadOnlyList<FrameworkEntry> Unresolved { get; init; } = [];
}

/// <summary>
/// A single resolved or unresolved framework entry.
/// </summary>
public sealed record FrameworkEntry(string Name, string RequestedVersion, string ResolvedVersion, string ResolvedPath);

/// <summary>
/// Result of querying available SDKs via <see cref="HostFxr.GetAvailableSdkDirs"/>.
/// </summary>
public sealed class AvailableSdksResult
{
    internal AvailableSdksResult() { }
    public int StatusCode { get; internal set; }
    public IReadOnlyList<string> SdkDirs { get; init; } = [];
}

/// <summary>
/// Disposable wrapper for a hostfxr host context handle.
/// Calls <see cref="HostFxr.Close"/> on dispose.
/// </summary>
public sealed class HostContextHandle : IDisposable
{
    private nint _handle;

    internal HostContextHandle(nint handle, int statusCode)
    {
        _handle = handle;
        StatusCode = statusCode;
    }

    /// <summary>The status code from the initialization call.</summary>
    public int StatusCode { get; }

    /// <summary>The raw handle value, or zero if disposed.</summary>
    public nint Value => _handle;

    /// <summary>Whether the handle is valid (non-zero and not disposed).</summary>
    public bool IsValid => _handle != 0;

    /// <summary>Run the application associated with this host context.</summary>
    public int RunApp()
    {
        ObjectDisposedException.ThrowIf(_handle == 0, this);
        return HostFxr.RunApp(_handle);
    }

    /// <summary>Get a runtime delegate of the specified type.</summary>
    public unsafe int GetRuntimeDelegate(HostFxrDelegateType type, out nint @delegate)
    {
        ObjectDisposedException.ThrowIf(_handle == 0, this);
        nint del = 0;
        int rc = HostFxr.GetRuntimeDelegate(_handle, (int)type, &del);
        @delegate = del;
        return rc;
    }

    /// <summary>Get the value of a runtime property.</summary>
    public unsafe int GetRuntimePropertyValue(string name, out string? value)
    {
        ObjectDisposedException.ThrowIf(_handle == 0, this);
        nint namePtr = PlatformStringMarshaller.ConvertToUnmanaged(name);
        try
        {
            nint val = 0;
            int rc = HostFxr.GetRuntimePropertyValue(_handle, namePtr, &val);
            value = HostFxrStatus.IsSuccess(rc) ? PlatformStringMarshaller.ConvertToManaged(val) : null;
            return rc;
        }
        finally
        {
            PlatformStringMarshaller.Free(namePtr);
        }
    }

    /// <summary>Set the value of a runtime property. Pass null to remove it.</summary>
    public int SetRuntimePropertyValue(string name, string? value)
    {
        ObjectDisposedException.ThrowIf(_handle == 0, this);
        nint namePtr = PlatformStringMarshaller.ConvertToUnmanaged(name);
        nint valuePtr = value is not null ? PlatformStringMarshaller.ConvertToUnmanaged(value) : 0;
        try
        {
            return HostFxr.SetRuntimePropertyValue(_handle, namePtr, valuePtr);
        }
        finally
        {
            PlatformStringMarshaller.Free(namePtr);
            PlatformStringMarshaller.Free(valuePtr);
        }
    }

    /// <summary>Get all runtime properties for this host context.</summary>
    public unsafe int GetRuntimeProperties(out IReadOnlyDictionary<string, string> properties)
    {
        ObjectDisposedException.ThrowIf(_handle == 0, this);

        nuint count = 0;
        int rc = HostFxr.GetRuntimeProperties(_handle, &count, null, null);

        if (rc == HostFxrStatus.HostApiBufferTooSmall && count > 0)
        {
            nint[] keyArr = new nint[(int)count];
            nint[] valArr = new nint[(int)count];
            fixed (nint* keys = keyArr)
            fixed (nint* values = valArr)
            {
                rc = HostFxr.GetRuntimeProperties(_handle, &count, keys, values);
            }

            if (HostFxrStatus.IsSuccess(rc))
            {
                var dict = new Dictionary<string, string>((int)count);
                for (int i = 0; i < (int)count; i++)
                {
                    string key = PlatformStringMarshaller.ConvertToManaged(keyArr[i]) ?? "";
                    string val = PlatformStringMarshaller.ConvertToManaged(valArr[i]) ?? "";
                    dict[key] = val;
                }
                properties = dict;
                return rc;
            }
        }

        properties = new Dictionary<string, string>();
        return rc;
    }

    ~HostContextHandle() => Dispose();

    public void Dispose()
    {
        nint h = Interlocked.Exchange(ref _handle, 0);
        if (h != 0 && HostFxr.IsLoaded)
            HostFxr.Close(h);
        GC.SuppressFinalize(this);
    }
}
