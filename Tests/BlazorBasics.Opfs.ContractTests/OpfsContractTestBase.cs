using System.Text;

namespace BlazorBasics.Opfs.ContractTests;

/// <summary>The target under test and the small helpers the battery shares.</summary>
public abstract class OpfsContractTestBase<TTarget>
    where TTarget : IOpfsContractTarget, new()
{
    protected TTarget Target { get; } = new();

    protected IOpfsFileSystem FileSystem => Target.FileSystem;

    protected OpfsPath In(string relativePath) => OpfsPath.Parse($"{Target.Folder}/{relativePath}");

    protected static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    protected static string Text(byte[] bytes) => Encoding.UTF8.GetString(bytes);

    protected Task WriteAsync(string relativePath, string text) => FileSystem.WriteTextAsync(In(relativePath), text);

    protected Task<string> ReadAsync(string relativePath) => FileSystem.ReadTextAsync(In(relativePath));

    protected async Task<bool> ExistsAsync(string relativePath) => await FileSystem.GetInfoOrNullAsync(In(relativePath)) is not null;
}
