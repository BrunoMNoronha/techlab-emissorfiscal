using EmissorFiscal.Core.Signature;
using Xunit;

namespace EmissorFiscal.Core.Tests;

public sealed class OfflineBoundaryTests
{
    [Fact]
    public void CoreDoesNotReferenceHttpOrSocketTransportAssemblies()
    {
        var references = typeof(A1Signer).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();
        Assert.DoesNotContain("System.Net.Http", references);
        Assert.DoesNotContain("System.Net.Sockets", references);
    }

    [Fact]
    public void Pkcs12ImportHasOnlyMemoryInputs()
    {
        var method = typeof(A1Signer).GetMethod(nameof(A1Signer.ImportPkcs12))!;
        Assert.Equal(new[] { typeof(ReadOnlySpan<byte>), typeof(ReadOnlySpan<char>) },
            method.GetParameters().Select(p => p.ParameterType).ToArray());
    }
}
