using System.Globalization;

namespace Core.Dto;

public sealed record ImportStats(int Total, int Accepted, int Skipped)
{
    public double ErrorPercent => Total == 0 ? 0 : 100.0 * Skipped / Total;

    public override string ToString() =>
        $"Усього: {Total}, прийнято: {Accepted}, пропущено: {Skipped} " +
        $"({ErrorPercent.ToString("F1", CultureInfo.InvariantCulture)}% помилок)";
}