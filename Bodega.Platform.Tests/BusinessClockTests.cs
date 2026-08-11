using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;
using Bodega.Platform.Shared.Infrastructure.Time;

namespace Bodega.Platform.Tests;

/// <summary>
///     The bodega runs in UTC-5, so between 19:00 local and midnight the UTC
///     clock has already moved to the next day. Every test here freezes time
///     inside that window — the exact gap where deriving calendar dates from
///     UTC produced wrong answers.
/// </summary>
public class BusinessClockTests
{
    /// <summary>2026-08-11 02:00 UTC is still 2026-08-10 21:00 in Lima.</summary>
    private static readonly DateTimeOffset LateEveningInLima = new(2026, 8, 11, 2, 0, 0, TimeSpan.Zero);

    private static BusinessClock CreateClock(DateTimeOffset now)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Business:TimeZone"] = "America/Lima" })
            .Build();

        return new BusinessClock(configuration, new FakeTimeProvider(now));
    }

    [Fact]
    public void Today_LateInTheEvening_IsStillTheLocalDayNotTheUtcOne()
    {
        var clock = CreateClock(LateEveningInLima);

        Assert.Equal(new DateOnly(2026, 8, 10), clock.Today);
    }

    [Fact]
    public void StartOfDay_IsMidnightLocalExpressedInUtc()
    {
        var clock = CreateClock(LateEveningInLima);

        // Lima midnight on the 10th is 05:00 UTC the same day.
        Assert.Equal(new DateTimeOffset(2026, 8, 10, 5, 0, 0, TimeSpan.Zero), clock.StartOfDay(new DateOnly(2026, 8, 10)));
    }

    [Fact]
    public void EndOfDay_IsJustBeforeLocalMidnightExpressedInUtc()
    {
        var clock = CreateClock(LateEveningInLima);

        var endOfDay = clock.EndOfDay(new DateOnly(2026, 8, 10));

        // The local day ends an instant before 00:00 on the 11th, which is
        // 05:00 UTC on the 11th.
        Assert.True(endOfDay < new DateTimeOffset(2026, 8, 11, 5, 0, 0, TimeSpan.Zero));
        Assert.True(endOfDay > new DateTimeOffset(2026, 8, 11, 4, 59, 0, TimeSpan.Zero));
    }

    /// <summary>
    ///     A sale rung up at 21:00 on the 10th belongs to the 10th's takings,
    ///     even though UTC already calls it the 11th. This is what made the
    ///     evening's sales land in the next day's report and the till never
    ///     match.
    /// </summary>
    [Fact]
    public void ToLocalDate_ForAnEveningSale_KeepsItOnTheDayItWasRungUp()
    {
        var clock = CreateClock(LateEveningInLima);

        Assert.Equal(new DateOnly(2026, 8, 10), clock.ToLocalDate(LateEveningInLima));
    }

    /// <summary>A day's range must contain that day's late-evening instants.</summary>
    [Fact]
    public void DayRange_ContainsAnInstantFromLateThatEvening()
    {
        var clock = CreateClock(LateEveningInLima);
        var day = new DateOnly(2026, 8, 10);

        Assert.InRange(LateEveningInLima, clock.StartOfDay(day), clock.EndOfDay(day));
    }
}
