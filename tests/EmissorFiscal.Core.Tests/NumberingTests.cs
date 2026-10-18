using EmissorFiscal.Core.Domain;
using EmissorFiscal.Core.Idempotency;
using EmissorFiscal.Core.Numbering;
using EmissorFiscal.Core.Preparation;
using Xunit;

namespace EmissorFiscal.Core.Tests;

public sealed class NumberingTests
{
    private static readonly NumberPoolKey Key = NumberPoolKey.Create("cnpj:12345678000195/est:0001", "1");
    private static readonly RequestHash HashA = RequestHash.Compute("{\"a\":\"1\"}", "{\"e\":\"1\"}");
    private static readonly RequestHash HashB = RequestHash.Compute("{\"a\":\"2\"}", "{\"e\":\"1\"}");

    private static InMemoryNumberPool Pool(long next = 1)
    {
        var pool = new InMemoryNumberPool();
        pool.Initialize(Key, next);
        return pool;
    }

    [Fact]
    public void ReserveStartsAtApprovedNumberAndIsSequential()
    {
        var pool = Pool(123);
        Assert.Equal(123, pool.Reserve(Key, "op-1", HashA).Number);
        Assert.Equal(124, pool.Reserve(Key, "op-2", HashA).Number);
        Assert.Equal(125, pool.Reserve(Key, "op-3", HashB).Number);
    }

    [Fact]
    public void ReserveWithoutApprovedNextNumberFails()
    {
        var pool = new InMemoryNumberPool();
        Assert.Throws<NumberPoolNotInitializedException>(() => pool.Reserve(Key, "op-1", HashA));
        Assert.Null(pool.Find(Key, "op-1"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1_000_000_000)]
    [InlineData(-5)]
    public void ApprovedNextNumberOutsideNnfRangeFails(long next) =>
        Assert.Throws<InvalidDataException>(() => new InMemoryNumberPool().Initialize(Key, next));

    [Fact]
    public void ReinitializingAPoolFailsBecauseItWouldAllowReuse()
    {
        var pool = Pool(10);
        pool.Reserve(Key, "op-1", HashA);
        Assert.Throws<NumberPoolAlreadyInitializedException>(() => pool.Initialize(Key, 1));
        Assert.Equal(11, pool.Reserve(Key, "op-2", HashA).Number);
    }

    [Fact]
    public void PoolExhaustsAtMaximumNnf()
    {
        var pool = Pool(NumberReservation.MaxNumber);
        Assert.Equal("999999999", pool.Reserve(Key, "op-1", HashA).PaddedNumber);
        Assert.Throws<NumberPoolExhaustedException>(() => pool.Reserve(Key, "op-2", HashA));
    }

    [Fact]
    public async Task ConcurrentReservationsAreUniqueAndGapless()
    {
        const int count = 500;
        var pool = Pool(1_000);
        var numbers = await Task.WhenAll(Enumerable.Range(0, count).Select(index =>
            Task.Run(() => pool.Reserve(Key, "op-" + index, HashA).Number)));
        Assert.Equal(Enumerable.Range(1_000, count).Select(n => (long)n), numbers.Order());
    }

    [Fact]
    public async Task SameOperationAndContentReplaysTheSameReservationEvenUnderConcurrency()
    {
        var pool = Pool(50);
        var replays = await Task.WhenAll(Enumerable.Range(0, 64).Select(_ =>
            Task.Run(() => pool.Reserve(Key, "op-same", HashA))));
        Assert.Single(replays.Distinct());
        Assert.Equal(50, replays[0].Number);
        Assert.Same(replays[0].Key, Key);
        Assert.Equal(replays[0], pool.Find(Key, "op-same"));
        // Nenhum número foi consumido pelas repetições.
        Assert.Equal(51, pool.Reserve(Key, "op-next", HashA).Number);
    }

    [Fact]
    public void SameOperationWithDifferentContentConflictsWithoutConsumingANumber()
    {
        var pool = Pool(7);
        var first = pool.Reserve(Key, "op-1", HashA);
        Assert.Throws<IdempotencyConflictException>(() => pool.Reserve(Key, "op-1", HashB));
        Assert.Equal(first, pool.Find(Key, "op-1"));
        Assert.Equal(8, pool.Reserve(Key, "op-2", HashB).Number);
    }

    [Fact]
    public void DifferentSeriesOrIdentitiesNeverSharePools()
    {
        var pool = Pool(1);
        var otherSeries = NumberPoolKey.Create(Key.FiscalIdentity, "2");
        var otherIdentity = NumberPoolKey.Create("cnpj:12345678000195/est:0002", "1");
        pool.Initialize(otherSeries, 500);
        pool.Initialize(otherIdentity, 900);
        Assert.Equal(1, pool.Reserve(Key, "op", HashA).Number);
        Assert.Equal(500, pool.Reserve(otherSeries, "op", HashA).Number);
        Assert.Equal(900, pool.Reserve(otherIdentity, "op", HashA).Number);
    }

    [Fact]
    public void SeriesIsNormalizedSoTheSameCanonicalIdentitySharesOnePool()
    {
        var pool = Pool(30);
        var alias = NumberPoolKey.Create(Key.FiscalIdentity, "001");
        Assert.Equal(Key, alias);
        Assert.Equal("001", alias.Series);
        Assert.Equal(30, pool.Reserve(alias, "op-1", HashA).Number);
        Assert.Equal(31, pool.Reserve(Key, "op-2", HashA).Number);
        Assert.Equal("cnpj:12345678000195/est:0001/65/2/001", Key.ToString());
    }

    [Theory]
    [InlineData("", "1")]
    [InlineData(" cnpj:1", "1")]
    [InlineData("cnpj:1", "01")]
    [InlineData("cnpj:1", "1000")]
    [InlineData("cnpj:1", "")]
    [InlineData("çnpj", "1")]
    public void InvalidKeysAreRejected(string identity, string series) =>
        Assert.Throws<InvalidDataException>(() => NumberPoolKey.Create(identity, series));

    [Theory]
    [InlineData("")]
    [InlineData("op 1")]
    [InlineData(" op-1")]
    public void InvalidOperationIdsAreRejectedBeforeTouchingThePool(string operationId)
    {
        var pool = Pool(1);
        Assert.Throws<InvalidDataException>(() => pool.Reserve(Key, operationId, HashA));
        Assert.Equal(1, pool.Reserve(Key, "op-ok", HashA).Number);
    }

    [Fact]
    public void ThePortHasNoWayToReleaseOrReuseNumbers()
    {
        // H13: rejeição não devolve número; a interface só inicializa, reserva e localiza.
        var methods = typeof(INumberPool).GetMethods().Select(m => m.Name).Order().ToArray();
        Assert.Equal(new[] { "Find", "Initialize", "Reserve" }, methods);
    }

    [Fact]
    public void ReservationFeedsTheExistingPreparationContextAndAccessKey()
    {
        var reservation = Pool(123).Reserve(Key, "op-1", HashA);
        var context = PreparationJson.ParseContext(
            $"{{\"version\":\"{PreparationVersion.Current}\",\"syntheticOnly\":true,\"number\":\"{reservation.Nnf}\",\"numericCode\":\"87654321\"}}");
        Assert.Equal("123", context.Number);
        var key = AccessKey.Create("2610", "12345678000195", Key.Series, reservation.PaddedNumber, context.NumericCode);
        Assert.Equal("000000123", key.Value.Substring(25, 9));
    }
}
