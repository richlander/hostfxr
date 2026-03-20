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
        Size = (nuint)Marshal.SizeOf<HostFxrInitializeParameters>(),
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
    public IReadOnlyList<ResolvedFramework> Resolved { get; init; } = [];
    public IReadOnlyList<ResolvedFramework> Unresolved { get; init; } = [];
}

/// <summary>
/// A single resolved or unresolved framework entry.
/// </summary>
public sealed record ResolvedFramework(string Name, string RequestedVersion, string ResolvedVersion, string ResolvedPath);
