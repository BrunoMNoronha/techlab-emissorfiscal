using System.Text;
using EmissorFiscal.Core.Danfe;
using EmissorFiscal.Core.Domain;
using EmissorFiscal.Core.QrCode;
using Xunit;

namespace EmissorFiscal.Core.Tests;

public sealed class QrEncoderTests
{
    // Vetor conhecido (ISO/IEC 18004, exemplo 1-M amplamente reproduzido): 16 codewords de
    // dados e os 10 codewords Reed-Solomon correspondentes.
    [Fact]
    public void ReedSolomonMatchesKnownVector()
    {
        byte[] data = [32, 91, 11, 120, 209, 114, 220, 77, 67, 64, 236, 17, 236, 17, 236, 17];
        byte[] expected = [196, 35, 39, 119, 235, 215, 231, 226, 93, 23];
        Assert.Equal(expected, QrEncoder.ReedSolomon(data, 10));
    }

    // Tabela de bits de formato para nível M (ISO/IEC 18004, Tabela C.1), máscaras 0..7.
    [Theory]
    [InlineData(0, "101010000010010")]
    [InlineData(1, "101000100100101")]
    [InlineData(2, "101111001111100")]
    [InlineData(3, "101101101001011")]
    [InlineData(4, "100010111111001")]
    [InlineData(5, "100000011001110")]
    [InlineData(6, "100111110010111")]
    [InlineData(7, "100101010100000")]
    public void FormatBitsMatchStandardTableForLevelM(int mask, string expected)
    {
        Assert.Equal(expected, Convert.ToString(QrEncoder.FormatBits(mask), 2).PadLeft(15, '0'));
        Assert.Equal(QrTestDecoder.FormatBits(0b00, mask), QrEncoder.FormatBits(mask));
    }

    [Theory]
    [InlineData(7, "000111110010010100")]
    [InlineData(8, "001000010110111100")]
    [InlineData(40, "101000110001101001")]
    public void VersionBitsMatchStandardTable(int version, string expected) =>
        Assert.Equal(expected, Convert.ToString(QrEncoder.VersionBits(version), 2).PadLeft(18, '0'));

    [Fact]
    public void CodewordTablesAgreeWithSymbolGeometryForEveryVersion()
    {
        int[] totals =
        [
            26, 44, 70, 100, 134, 172, 196, 242, 292, 346, 404, 466, 532, 581, 655, 733, 815, 901, 991, 1085,
            1156, 1258, 1364, 1474, 1588, 1706, 1828, 1921, 2051, 2185, 2323, 2465, 2611, 2761, 2876, 3034, 3196,
            3362, 3532, 3706
        ];
        for (var version = 1; version <= 40; version++)
        {
            Assert.Equal(totals[version - 1], QrEncoder.RawCodewords(version));
            Assert.Equal(version * 4 + 17, QrEncoder.SizeOf(version));
            Assert.InRange(QrEncoder.DataCodewords(version), 1, QrEncoder.RawCodewords(version) - 1);
            Assert.True(QrEncoder.DataCapacityBytes(version) < QrEncoder.DataCapacityBytes(Math.Min(40, version + 1)) ||
                        version == 40);
        }
        // Capacidades de dados nível M conhecidas: 1-M 16, 6-M 108, 10-M 216, 40-M 2334 codewords.
        Assert.Equal(16, QrEncoder.DataCodewords(1));
        Assert.Equal(108, QrEncoder.DataCodewords(6));
        Assert.Equal(216, QrEncoder.DataCodewords(10));
        Assert.Equal(2334, QrEncoder.DataCodewords(40));
        Assert.Equal(2331, QrEncoder.DataCapacityBytes(40));
    }

    [Theory]
    [InlineData(1, new int[0])]
    [InlineData(2, new[] { 6, 18 })]
    [InlineData(6, new[] { 6, 34 })]
    [InlineData(7, new[] { 6, 22, 38 })]
    [InlineData(14, new[] { 6, 26, 46, 66 })]
    [InlineData(32, new[] { 6, 34, 60, 86, 112, 138 })]
    [InlineData(40, new[] { 6, 30, 58, 86, 114, 142, 170 })]
    public void AlignmentPositionsMatchStandardTable(int version, int[] expected) =>
        Assert.Equal(expected, QrEncoder.AlignmentPositions(version));

    [Fact]
    public void QrV3UrlUsesSmallestVersionAndDecodesBackToTheSameText()
    {
        var key = AccessKey.Create("2610", "93847261000122", "017", "000000123", "87654321");
        var text = QrCode3.Online(key);
        Assert.Equal(91, Encoding.ASCII.GetByteCount(text));
        var qr = QrEncoder.Encode(text);
        // 5-M comporta 84 bytes; 6-M comporta 106.
        Assert.Equal(6, qr.Version);
        Assert.Equal(41, qr.Size);
        Assert.Equal(84, QrEncoder.DataCapacityBytes(5));
        Assert.Equal(106, QrEncoder.DataCapacityBytes(6));
        QrTestDecoder.AssertFunctionPatterns(qr);
        Assert.Equal(text, QrTestDecoder.Decode(qr));
    }

    [Theory]
    [InlineData(1, 1)]      // 1-M, contagem de 8 bits
    [InlineData(84, 5)]     // limite exato de 5-M
    [InlineData(85, 6)]
    [InlineData(150, 8)]    // >= 7: bits de versão
    [InlineData(300, 13)]   // >= 10: contagem de 16 bits
    [InlineData(2331, 40)]  // capacidade máxima
    public void ArbitraryPayloadsRoundTripThroughIndependentDecoder(int length, int expectedVersion)
    {
        var payload = new byte[length];
        for (var i = 0; i < length; i++) payload[i] = (byte)(i * 131 + 17);
        var qr = QrEncoder.EncodeBytes(payload);
        Assert.Equal(expectedVersion, qr.Version);
        QrTestDecoder.AssertFunctionPatterns(qr);
        Assert.Equal(payload, QrTestDecoder.DecodeBytes(qr));
    }

    [Fact]
    public void PayloadBeyondVersion40IsRefused() =>
        Assert.Throws<InvalidDataException>(() => QrEncoder.EncodeBytes(new byte[2332]));

    [Fact]
    public void SvgHasQuietZoneWhiteBackgroundAndOneRectanglePerDarkRun()
    {
        var qr = QrEncoder.Encode("A");
        var svg = qr.ToSvg();
        Assert.StartsWith("<svg xmlns=\"http://www.w3.org/2000/svg\"", svg, StringComparison.Ordinal);
        Assert.Contains("viewBox=\"0 0 29 29\"", svg, StringComparison.Ordinal);
        Assert.Contains("width=\"116\" height=\"116\"", svg, StringComparison.Ordinal);
        Assert.Contains("<rect width=\"29\" height=\"29\" fill=\"#ffffff\"/>", svg, StringComparison.Ordinal);
        // O localizador superior esquerdo começa em (4,4) e tem 7 módulos de largura.
        Assert.Contains("M4 4h7v1h-7z", svg, StringComparison.Ordinal);
        var runs = 0;
        for (var row = 0; row < qr.Size; row++)
            for (var column = 0; column < qr.Size; column++)
                if (qr[row, column] && (column == 0 || !qr[row, column - 1])) runs++;
        Assert.Equal(runs, svg.Split("zM").Length);
        Assert.EndsWith("</svg>", svg, StringComparison.Ordinal);
        Assert.Throws<ArgumentOutOfRangeException>(() => qr.ToSvg(quietZone: 3));
    }
}

// Decodificador mínimo, independente do codificador: lê formato, desmascara,
// percorre o zigue-zague do padrão, desintercala, confere as síndromes
// Reed-Solomon de cada bloco e extrai o conteúdo em modo byte. Não corrige
// erros: qualquer síndrome diferente de zero falha o teste.
internal static class QrTestDecoder
{
    private static readonly byte[] Exp = new byte[512];
    private static readonly byte[] Log = new byte[256];

    static QrTestDecoder()
    {
        var x = 1;
        for (var i = 0; i < 255; i++)
        {
            Exp[i] = (byte)x;
            Log[x] = (byte)i;
            x <<= 1;
            if (x >= 256) x ^= 0x11D;
        }
        for (var i = 255; i < 512; i++) Exp[i] = Exp[i - 255];
    }

    private static byte Mul(byte a, byte b) => a == 0 || b == 0 ? (byte)0 : Exp[Log[a] + Log[b]];

    public static int FormatBits(int level, int mask)
    {
        var data = level << 3 | mask;
        var value = data << 10;
        for (var i = 14; i >= 10; i--)
            if ((value >> i & 1) != 0) value ^= 0b10100110111 << (i - 10);
        return (data << 10 | value) ^ 0x5412;
    }

    public static string Decode(QrMatrix qr) => Encoding.UTF8.GetString(DecodeBytes(qr));

    public static void AssertFunctionPatterns(QrMatrix qr)
    {
        var size = qr.Size;
        var version = (size - 17) / 4;
        Assert.Equal(size, version * 4 + 17);
        foreach (var (row, column) in new[] { (0, 0), (0, size - 7), (size - 7, 0) })
            for (var r = 0; r < 7; r++)
                for (var c = 0; c < 7; c++)
                {
                    var ring = Math.Max(Math.Abs(r - 3), Math.Abs(c - 3));
                    Assert.Equal(ring != 2, qr[row + r, column + c]);
                }
        for (var i = 8; i < size - 8; i++)
        {
            Assert.Equal(i % 2 == 0, qr[6, i]);
            Assert.Equal(i % 2 == 0, qr[i, 6]);
        }
        Assert.True(qr[size - 8, 8]);
        Assert.False(qr[7, 7]);
        Assert.False(qr[7, size - 8]);
        Assert.False(qr[size - 8, 7]);
        var format = ReadFormat(qr, out var mask);
        Assert.Equal(0, format >> 13); // nível M = 00
        Assert.InRange(mask, 0, 7);
        if (version >= 7)
        {
            var bits = 0;
            for (var i = 0; i < 18; i++)
            {
                var a = size - 11 + i % 3;
                var b = i / 3;
                Assert.Equal(qr[b, a], qr[a, b]);
                if (qr[b, a]) bits |= 1 << i;
            }
            Assert.Equal(version, bits >> 12);
        }
    }

    private static int ReadFormat(QrMatrix qr, out int mask)
    {
        var size = qr.Size;
        var first = 0;
        for (var i = 0; i <= 5; i++) if (qr[i, 8]) first |= 1 << i;
        if (qr[7, 8]) first |= 1 << 6;
        if (qr[8, 8]) first |= 1 << 7;
        if (qr[8, 7]) first |= 1 << 8;
        for (var i = 9; i < 15; i++) if (qr[8, 14 - i]) first |= 1 << i;
        var second = 0;
        for (var i = 0; i < 8; i++) if (qr[8, size - 1 - i]) second |= 1 << i;
        for (var i = 8; i < 15; i++) if (qr[size - 15 + i, 8]) second |= 1 << i;
        Assert.Equal(first, second);
        var data = (first ^ 0x5412) >> 10;
        Assert.Equal(first, FormatBits(data >> 3, data & 7));
        mask = data & 7;
        return data;
    }

    public static byte[] DecodeBytes(QrMatrix qr)
    {
        var size = qr.Size;
        var version = (size - 17) / 4;
        ReadFormat(qr, out var mask);
        var function = FunctionMap(version);
        var bits = new List<bool>();
        for (var right = size - 1; right >= 1; right -= 2)
        {
            if (right == 6) right = 5;
            var upward = ((right + 1) & 2) == 0;
            for (var vertical = 0; vertical < size; vertical++)
            {
                var row = upward ? size - 1 - vertical : vertical;
                for (var j = 0; j < 2; j++)
                {
                    var column = right - j;
                    if (function[row, column]) continue;
                    bits.Add(qr[row, column] ^ Mask(mask, row, column));
                }
            }
        }
        var total = QrEncoder.RawCodewords(version);
        Assert.True(bits.Count >= total * 8);
        var interleaved = new byte[total];
        for (var i = 0; i < total * 8; i++) if (bits[i]) interleaved[i >> 3] |= (byte)(0x80 >> (i & 7));
        var (blocks, ecc) = Blocks(version);
        var shortBlocks = blocks - total % blocks;
        var shortLength = total / blocks;
        var lengths = Enumerable.Range(0, blocks).Select(i => shortLength + (i < shortBlocks ? 0 : 1)).ToArray();
        var codewords = lengths.Select(length => new byte[length]).ToArray();
        var k = 0;
        for (var i = 0; i < shortLength + 1 - ecc; i++)
            for (var j = 0; j < blocks; j++)
                if (i < lengths[j] - ecc) codewords[j][i] = interleaved[k++];
        for (var i = 0; i < ecc; i++)
            for (var j = 0; j < blocks; j++)
                codewords[j][lengths[j] - ecc + i] = interleaved[k++];
        Assert.Equal(total, k);
        var data = new List<byte>();
        foreach (var block in codewords)
        {
            for (var power = 0; power < ecc; power++)
            {
                byte syndrome = 0;
                foreach (var codeword in block) syndrome = (byte)(Mul(syndrome, Exp[power]) ^ codeword);
                Assert.Equal(0, syndrome);
            }
            data.AddRange(block.Take(block.Length - ecc));
        }
        var reader = new BitReader(data.ToArray());
        Assert.Equal(0b0100, reader.Read(4));
        var count = reader.Read(version < 10 ? 8 : 16);
        var result = new byte[count];
        for (var i = 0; i < count; i++) result[i] = (byte)reader.Read(8);
        return result;
    }

    // Blocos de correção (nível M) das versões exercitadas: ISO/IEC 18004, Tabela 9.
    private static (int Blocks, int Ecc) Blocks(int version) => version switch
    {
        1 => (1, 10), 5 => (2, 24), 6 => (4, 16), 8 => (4, 22), 13 => (9, 22), 40 => (49, 28),
        _ => throw new NotSupportedException($"Versão {version} fora do decodificador de teste.")
    };

    private static bool[,] FunctionMap(int version)
    {
        var size = version * 4 + 17;
        var map = new bool[size, size];
        for (var r = 0; r < 9; r++)
            for (var c = 0; c < 9; c++)
            {
                map[r, c] = true;
                if (c < 8) map[r, size - 1 - c] = true;
                if (r < 8) map[size - 1 - r, c] = true;
            }
        for (var i = 0; i < size; i++) { map[6, i] = true; map[i, 6] = true; }
        var positions = Alignment(version);
        for (var i = 0; i < positions.Length; i++)
            for (var j = 0; j < positions.Length; j++)
            {
                if ((i == 0 && j == 0) || (i == 0 && j == positions.Length - 1) || (i == positions.Length - 1 && j == 0)) continue;
                for (var dr = -2; dr <= 2; dr++)
                    for (var dc = -2; dc <= 2; dc++) map[positions[i] + dr, positions[j] + dc] = true;
            }
        if (version >= 7)
            for (var i = 0; i < 18; i++)
            {
                map[i / 3, size - 11 + i % 3] = true;
                map[size - 11 + i % 3, i / 3] = true;
            }
        return map;
    }

    private static int[] Alignment(int version) => version switch
    {
        1 => [], 5 => [6, 30], 6 => [6, 34], 8 => [6, 24, 42], 13 => [6, 34, 62],
        40 => [6, 30, 58, 86, 114, 142, 170],
        _ => throw new NotSupportedException($"Versão {version} fora do decodificador de teste.")
    };

    private static bool Mask(int mask, int row, int column) => mask switch
    {
        0 => (row + column) % 2 == 0,
        1 => row % 2 == 0,
        2 => column % 3 == 0,
        3 => (row + column) % 3 == 0,
        4 => (row / 2 + column / 3) % 2 == 0,
        5 => row * column % 2 + row * column % 3 == 0,
        6 => (row * column % 2 + row * column % 3) % 2 == 0,
        _ => ((row + column) % 2 + row * column % 3) % 2 == 0
    };

    private sealed class BitReader(byte[] data)
    {
        private int _position;

        public int Read(int count)
        {
            var value = 0;
            for (var i = 0; i < count; i++, _position++)
                value = value << 1 | (data[_position >> 3] >> (7 - (_position & 7)) & 1);
            return value;
        }
    }
}
