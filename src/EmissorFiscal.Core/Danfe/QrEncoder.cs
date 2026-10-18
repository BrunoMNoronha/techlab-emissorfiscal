using System.Text;

namespace EmissorFiscal.Core.Danfe;

// Matriz de módulos de um símbolo QR Code (ISO/IEC 18004). Linha/coluna 0 no
// canto superior esquerdo; true = módulo escuro. Não inclui a zona de silêncio.
public sealed class QrMatrix
{
    private readonly bool[,] _modules;

    internal QrMatrix(int version, bool[,] modules)
    {
        Version = version;
        _modules = modules;
    }

    public int Version { get; }
    public int Size => _modules.GetLength(0);
    public bool this[int row, int column] => _modules[row, column];

    // SVG mínimo: um quadrado por módulo escuro, agrupado por corridas horizontais,
    // com zona de silêncio de quatro módulos (mínimo do padrão) e fundo branco.
    public string ToSvg(int modulePixels = 4, int quietZone = 4)
    {
        if (modulePixels is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(modulePixels));
        if (quietZone is < 4 or > 16) throw new ArgumentOutOfRangeException(nameof(quietZone));
        var total = Size + 2 * quietZone;
        var path = new StringBuilder();
        for (var row = 0; row < Size; row++)
        {
            var column = 0;
            while (column < Size)
            {
                if (!_modules[row, column]) { column++; continue; }
                var start = column;
                while (column < Size && _modules[row, column]) column++;
                path.Append('M').Append(start + quietZone).Append(' ').Append(row + quietZone)
                    .Append('h').Append(column - start).Append("v1h-").Append(column - start).Append('z');
            }
        }
        var pixels = total * modulePixels;
        return "<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" " +
            $"width=\"{pixels}\" height=\"{pixels}\" viewBox=\"0 0 {total} {total}\" shape-rendering=\"crispEdges\">" +
            $"<rect width=\"{total}\" height=\"{total}\" fill=\"#ffffff\"/>" +
            $"<path d=\"{path}\" fill=\"#000000\"/></svg>";
    }
}

// Codificador QR Code próprio: modo byte, nível de correção M, versão mínima
// automática (1 a 40), máscara escolhida pelas penalidades do padrão. Sem
// dependências externas; produz apenas a matriz de módulos.
public static class QrEncoder
{
    public const int MaxVersion = 40;
    // Nível M: indicador 00 nos bits de formato.
    private const int ErrorCorrectionLevelBits = 0b00;

    // Codewords de correção por bloco e quantidade de blocos, nível M, versões 1..40
    // (ISO/IEC 18004, tabela 9). Índice 0 não utilizado.
    private static readonly byte[] EccCodewordsPerBlock =
    [
        0, 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26,
        26, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28
    ];

    private static readonly byte[] ErrorCorrectionBlocks =
    [
        0, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10, 10, 11, 13, 14, 16,
        17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49
    ];

    public static QrMatrix Encode(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return EncodeBytes(Encoding.UTF8.GetBytes(text));
    }

    public static QrMatrix EncodeBytes(ReadOnlySpan<byte> data)
    {
        var version = 1;
        while (DataCapacityBytes(version) < data.Length)
        {
            if (version == MaxVersion) throw new InvalidDataException("Conteúdo excede a capacidade do QR Code versão 40-M.");
            version++;
        }
        var size = SizeOf(version);
        var modules = new bool[size, size];
        var function = new bool[size, size];
        DrawFunctionPatterns(version, modules, function);
        DrawCodewords(AddEccAndInterleave(BuildCodewords(data, version), version), modules, function);
        var mask = ChooseMask(modules, function);
        ApplyMask(modules, function, mask);
        DrawFormatBits(modules, function, mask);
        return new QrMatrix(version, modules);
    }

    public static int SizeOf(int version) => RequireVersion(version) * 4 + 17;

    public static int RawCodewords(int version)
    {
        RequireVersion(version);
        var modules = (16 * version + 128) * version + 64;
        if (version >= 2)
        {
            var alignments = version / 7 + 2;
            modules -= (25 * alignments - 10) * alignments - 55;
            if (version >= 7) modules -= 36;
        }
        return modules / 8;
    }

    public static int DataCodewords(int version) =>
        RawCodewords(version) - EccCodewordsPerBlock[version] * ErrorCorrectionBlocks[version];

    public static int DataCapacityBytes(int version) =>
        (DataCodewords(version) * 8 - 4 - CharacterCountBits(version)) / 8;

    public static int CharacterCountBits(int version) => RequireVersion(version) < 10 ? 8 : 16;

    public static IReadOnlyList<int> AlignmentPositions(int version)
    {
        RequireVersion(version);
        if (version == 1) return [];
        var count = version / 7 + 2;
        var size = SizeOf(version);
        var step = version == 32 ? 26 : (version * 4 + count * 2 + 1) / (count * 2 - 2) * 2;
        var result = new int[count];
        result[0] = 6;
        for (int i = 1, position = size - 7; i < count; i++, position -= step) result[count - i] = position;
        return result;
    }

    // Bits de formato (15): nível + máscara, BCH(15,5) e máscara fixa 0x5412.
    public static int FormatBits(int mask)
    {
        if (mask is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(mask));
        var data = ErrorCorrectionLevelBits << 3 | mask;
        var remainder = data;
        for (var i = 0; i < 10; i++) remainder = (remainder << 1) ^ ((remainder >> 9) * 0x537);
        return (data << 10 | remainder) ^ 0x5412;
    }

    // Bits de versão (18), presentes a partir da versão 7: Golay (18,6).
    public static int VersionBits(int version)
    {
        if (RequireVersion(version) < 7) throw new ArgumentOutOfRangeException(nameof(version));
        var remainder = version;
        for (var i = 0; i < 12; i++) remainder = (remainder << 1) ^ ((remainder >> 11) * 0x1F25);
        return version << 12 | remainder;
    }

    // Reed-Solomon sobre GF(2^8) com polinômio primitivo 0x11D: devolve os
    // codewords de correção do bloco informado.
    public static byte[] ReedSolomon(ReadOnlySpan<byte> data, int degree)
    {
        if (degree is < 1 or > 255) throw new ArgumentOutOfRangeException(nameof(degree));
        var divisor = new byte[degree];
        divisor[^1] = 1;
        var root = 1;
        for (var i = 0; i < degree; i++)
        {
            for (var j = 0; j < degree; j++)
            {
                divisor[j] = Multiply(divisor[j], root);
                if (j + 1 < degree) divisor[j] ^= divisor[j + 1];
            }
            root = Multiply(root, 0x02);
        }
        var result = new byte[degree];
        foreach (var value in data)
        {
            var factor = value ^ result[0];
            Array.Copy(result, 1, result, 0, degree - 1);
            result[^1] = 0;
            for (var j = 0; j < degree; j++) result[j] ^= Multiply(divisor[j], factor);
        }
        return result;
    }

    public static byte Multiply(int x, int y)
    {
        var z = 0;
        for (var i = 7; i >= 0; i--)
        {
            z = (z << 1) ^ ((z >> 7) * 0x11D);
            z ^= (y >> i & 1) * x;
        }
        return (byte)z;
    }

    public static bool MaskBit(int mask, int row, int column) => mask switch
    {
        0 => (row + column) % 2 == 0,
        1 => row % 2 == 0,
        2 => column % 3 == 0,
        3 => (row + column) % 3 == 0,
        4 => (row / 2 + column / 3) % 2 == 0,
        5 => row * column % 2 + row * column % 3 == 0,
        6 => (row * column % 2 + row * column % 3) % 2 == 0,
        7 => ((row + column) % 2 + row * column % 3) % 2 == 0,
        _ => throw new ArgumentOutOfRangeException(nameof(mask))
    };

    private static int RequireVersion(int version) =>
        version is >= 1 and <= MaxVersion ? version : throw new ArgumentOutOfRangeException(nameof(version));

    private static byte[] BuildCodewords(ReadOnlySpan<byte> data, int version)
    {
        var bits = new BitWriter(DataCodewords(version));
        bits.Append(0b0100, 4);
        bits.Append(data.Length, CharacterCountBits(version));
        foreach (var value in data) bits.Append(value, 8);
        var capacity = DataCodewords(version) * 8;
        bits.Append(0, Math.Min(4, capacity - bits.Length));
        bits.Append(0, (8 - bits.Length % 8) % 8);
        for (var pad = 0xEC; bits.Length < capacity; pad ^= 0xEC ^ 0x11) bits.Append(pad, 8);
        return bits.ToArray();
    }

    private static byte[] AddEccAndInterleave(byte[] data, int version)
    {
        var blocks = ErrorCorrectionBlocks[version];
        var eccLength = EccCodewordsPerBlock[version];
        var raw = RawCodewords(version);
        var shortBlocks = blocks - raw % blocks;
        var shortLength = raw / blocks;
        var blockData = new List<byte[]>(blocks);
        for (int i = 0, offset = 0; i < blocks; i++)
        {
            var length = shortLength - eccLength + (i < shortBlocks ? 0 : 1);
            var block = new byte[shortLength + 1];
            data.AsSpan(offset, length).CopyTo(block);
            ReedSolomon(data.AsSpan(offset, length), eccLength).CopyTo(block, shortLength + 1 - eccLength);
            blockData.Add(block);
            offset += length;
        }
        var result = new byte[raw];
        var k = 0;
        for (var i = 0; i < shortLength + 1; i++)
            for (var j = 0; j < blocks; j++)
                if (i != shortLength - eccLength || j >= shortBlocks) result[k++] = blockData[j][i];
        return result;
    }

    private static void DrawFunctionPatterns(int version, bool[,] modules, bool[,] function)
    {
        var size = modules.GetLength(0);
        for (var i = 0; i < size; i++)
        {
            Set(modules, function, 6, i, i % 2 == 0);
            Set(modules, function, i, 6, i % 2 == 0);
        }
        DrawFinder(modules, function, 3, 3);
        DrawFinder(modules, function, 3, size - 4);
        DrawFinder(modules, function, size - 4, 3);
        var positions = AlignmentPositions(version);
        for (var i = 0; i < positions.Count; i++)
            for (var j = 0; j < positions.Count; j++)
            {
                if ((i == 0 && j == 0) || (i == 0 && j == positions.Count - 1) || (i == positions.Count - 1 && j == 0)) continue;
                DrawAlignment(modules, function, positions[i], positions[j]);
            }
        DrawFormatBits(modules, function, 0);
        DrawVersionBits(version, modules, function);
    }

    private static void DrawFinder(bool[,] modules, bool[,] function, int row, int column)
    {
        for (var dr = -4; dr <= 4; dr++)
            for (var dc = -4; dc <= 4; dc++)
            {
                var r = row + dr;
                var c = column + dc;
                if (r < 0 || c < 0 || r >= modules.GetLength(0) || c >= modules.GetLength(0)) continue;
                var distance = Math.Max(Math.Abs(dr), Math.Abs(dc));
                Set(modules, function, r, c, distance != 2 && distance != 4);
            }
    }

    private static void DrawAlignment(bool[,] modules, bool[,] function, int row, int column)
    {
        for (var dr = -2; dr <= 2; dr++)
            for (var dc = -2; dc <= 2; dc++)
                Set(modules, function, row + dr, column + dc, Math.Max(Math.Abs(dr), Math.Abs(dc)) != 1);
    }

    private static void DrawFormatBits(bool[,] modules, bool[,] function, int mask)
    {
        var bits = FormatBits(mask);
        var size = modules.GetLength(0);
        for (var i = 0; i <= 5; i++) Set(modules, function, i, 8, Bit(bits, i));
        Set(modules, function, 7, 8, Bit(bits, 6));
        Set(modules, function, 8, 8, Bit(bits, 7));
        Set(modules, function, 8, 7, Bit(bits, 8));
        for (var i = 9; i < 15; i++) Set(modules, function, 8, 14 - i, Bit(bits, i));
        for (var i = 0; i < 8; i++) Set(modules, function, 8, size - 1 - i, Bit(bits, i));
        for (var i = 8; i < 15; i++) Set(modules, function, size - 15 + i, 8, Bit(bits, i));
        Set(modules, function, size - 8, 8, true); // módulo escuro fixo
    }

    private static void DrawVersionBits(int version, bool[,] modules, bool[,] function)
    {
        if (version < 7) return;
        var bits = VersionBits(version);
        var size = modules.GetLength(0);
        for (var i = 0; i < 18; i++)
        {
            var a = size - 11 + i % 3;
            var b = i / 3;
            Set(modules, function, b, a, Bit(bits, i));
            Set(modules, function, a, b, Bit(bits, i));
        }
    }

    private static void DrawCodewords(byte[] codewords, bool[,] modules, bool[,] function)
    {
        var size = modules.GetLength(0);
        var i = 0;
        for (var right = size - 1; right >= 1; right -= 2)
        {
            if (right == 6) right = 5;
            for (var vertical = 0; vertical < size; vertical++)
                for (var j = 0; j < 2; j++)
                {
                    var column = right - j;
                    var upward = ((right + 1) & 2) == 0;
                    var row = upward ? size - 1 - vertical : vertical;
                    if (function[row, column] || i >= codewords.Length * 8) continue;
                    modules[row, column] = Bit(codewords[i >> 3], 7 - (i & 7));
                    i++;
                }
        }
    }

    private static int ChooseMask(bool[,] modules, bool[,] function)
    {
        var best = 0;
        var bestPenalty = int.MaxValue;
        for (var mask = 0; mask < 8; mask++)
        {
            var candidate = (bool[,])modules.Clone();
            ApplyMask(candidate, function, mask);
            DrawFormatBits(candidate, function, mask);
            var penalty = Penalty(candidate);
            if (penalty < bestPenalty) { bestPenalty = penalty; best = mask; }
        }
        return best;
    }

    private static void ApplyMask(bool[,] modules, bool[,] function, int mask)
    {
        var size = modules.GetLength(0);
        for (var row = 0; row < size; row++)
            for (var column = 0; column < size; column++)
                if (!function[row, column] && MaskBit(mask, row, column)) modules[row, column] ^= true;
    }

    // Penalidades N1=3, N2=3, N3=40, N4=10 (ISO/IEC 18004, 7.8.3.1). O valor só
    // escolhe a máscara; a decodificação lê a máscara nos bits de formato.
    private static int Penalty(bool[,] modules)
    {
        var size = modules.GetLength(0);
        var penalty = 0;
        var line = new bool[size];
        for (var index = 0; index < size; index++)
        {
            for (var i = 0; i < size; i++) line[i] = modules[index, i];
            penalty += LinePenalty(line);
            for (var i = 0; i < size; i++) line[i] = modules[i, index];
            penalty += LinePenalty(line);
        }
        var dark = 0;
        for (var row = 0; row < size; row++)
            for (var column = 0; column < size; column++)
            {
                if (modules[row, column]) dark++;
                if (row + 1 < size && column + 1 < size &&
                    modules[row, column] == modules[row, column + 1] &&
                    modules[row, column] == modules[row + 1, column] &&
                    modules[row, column] == modules[row + 1, column + 1]) penalty += 3;
            }
        var total = size * size;
        var deviation = (Math.Abs(dark * 20 - total * 10) + total - 1) / total - 1;
        return penalty + Math.Max(deviation, 0) * 10;
    }

    private static readonly bool[] FinderCore = [true, false, true, true, true, false, true];

    private static int LinePenalty(bool[] line)
    {
        var penalty = 0;
        var run = 1;
        for (var i = 1; i <= line.Length; i++)
        {
            if (i < line.Length && line[i] == line[i - 1]) { run++; continue; }
            if (run >= 5) penalty += run - 2;
            run = 1;
        }
        for (var i = 0; i + 7 <= line.Length; i++)
        {
            var match = true;
            for (var j = 0; j < 7 && match; j++) match = line[i + j] == FinderCore[j];
            if (!match) continue;
            if (Light(line, i - 4, i) || Light(line, i + 7, i + 11)) penalty += 40;
        }
        return penalty;
    }

    private static bool Light(bool[] line, int from, int to)
    {
        if (from < 0 || to > line.Length) return false;
        for (var i = from; i < to; i++) if (line[i]) return false;
        return true;
    }

    private static bool Bit(int value, int index) => (value >> index & 1) != 0;

    private static void Set(bool[,] modules, bool[,] function, int row, int column, bool dark)
    {
        modules[row, column] = dark;
        function[row, column] = true;
    }

    private sealed class BitWriter(int capacityBytes)
    {
        private readonly byte[] _buffer = new byte[capacityBytes];
        public int Length { get; private set; }

        public void Append(int value, int count)
        {
            for (var i = count - 1; i >= 0; i--, Length++)
                if ((value >> i & 1) != 0) _buffer[Length >> 3] |= (byte)(0x80 >> (Length & 7));
        }

        public byte[] ToArray() => _buffer;
    }
}
