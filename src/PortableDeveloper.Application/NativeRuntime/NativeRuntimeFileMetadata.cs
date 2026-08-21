namespace PortableDeveloper.Application.NativeRuntime;

public sealed record NativeRuntimeFileMetadata(
    string FileName,
    string FileVersion,
    string Sha256,
    string Signer,
    DateTimeOffset ImportedAtUtc);
