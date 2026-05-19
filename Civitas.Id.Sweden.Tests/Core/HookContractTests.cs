using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Internal;

namespace Civitas.Id.Sweden.Tests.Core;

/// <summary>
///     Pins the explicit-interface <see cref="ISwedishPersonIdHooks{TSelf}"/>
///     implementations on the sealed subtypes. Explicit static-abstract members
///     are NOT accessible by concrete type name (C# 11+ spec) — tests dispatch
///     through a constrained generic helper.
/// </summary>
public class HookContractTests
{
    // Generic helpers — only way to invoke explicit-interface static abstract members.
    private static bool IsDayValid<TSelf>(int d)
        where TSelf : PhysicalPersonId, ISwedishPersonIdHooks<TSelf>
        => TSelf.IsDayValid(d);

    private static int CalendarDay<TSelf>(int d)
        where TSelf : PhysicalPersonId, ISwedishPersonIdHooks<TSelf>
        => TSelf.CalendarDay(d);

    private static TSelf FromValidated<TSelf>(string n)
        where TSelf : PhysicalPersonId, ISwedishPersonIdHooks<TSelf>
        => TSelf.FromValidated(n);

    public class PersonalIdHooks
    {
        [Test]
        [Arguments(1, true)]
        [Arguments(15, true)]
        [Arguments(31, true)]
        [Arguments(0, false)]
        [Arguments(32, false)]
        [Arguments(61, false)]
        public async Task IsDayValid_AcceptsCalendarRange(int day, bool expected)
        {
            await Assert.That(IsDayValid<PersonalId>(day)).IsEqualTo(expected);
        }

        [Test]
        public async Task CalendarDay_IsIdentity()
        {
            await Assert.That(CalendarDay<PersonalId>(1)).IsEqualTo(1);
            await Assert.That(CalendarDay<PersonalId>(15)).IsEqualTo(15);
            await Assert.That(CalendarDay<PersonalId>(31)).IsEqualTo(31);
        }

        [Test]
        public async Task FromValidated_RoundTripsThroughLongFormat()
        {
            var p = FromValidated<PersonalId>("198901010101");
            await Assert.That(p.LongFormat()).IsEqualTo("198901010101");
        }
    }

    public class CoordinationIdHooks
    {
        [Test]
        [Arguments(61, true)]
        [Arguments(75, true)]
        [Arguments(91, true)]
        [Arguments(60, false)]
        [Arguments(92, false)]
        [Arguments(15, false)]
        public async Task IsDayValid_AcceptsCoordinationRange(int day, bool expected)
        {
            await Assert.That(IsDayValid<CoordinationId>(day)).IsEqualTo(expected);
        }

        [Test]
        public async Task CalendarDay_OffsetsByMinus60()
        {
            await Assert.That(CalendarDay<CoordinationId>(61)).IsEqualTo(1);
            await Assert.That(CalendarDay<CoordinationId>(75)).IsEqualTo(15);
            await Assert.That(CalendarDay<CoordinationId>(91)).IsEqualTo(31);
        }
    }
}
