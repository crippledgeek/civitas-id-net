namespace Civitas.Id.Time.Tests;

public class SwedenTests
{
    public class Stockholm
    {
        [Test]
        public async Task ResolvesToEuropeStockholm()
        {
            await Assert.That(Sweden.Stockholm.TimeZone.Id).IsEqualTo("Europe/Stockholm");
        }

        [Test]
        public async Task HasBaseOffsetOfPlusOne()
        {
            await Assert.That(Sweden.Stockholm.TimeZone.BaseUtcOffset).IsEqualTo(TimeSpan.FromHours(1));
        }

        [Test]
        public async Task ObservesCestInSummer()
        {
            var summer = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Unspecified);
            await Assert.That(Sweden.Stockholm.TimeZone.GetUtcOffset(summer)).IsEqualTo(TimeSpan.FromHours(2));
        }

        [Test]
        public async Task ObservesCetInWinter()
        {
            var winter = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Unspecified);
            await Assert.That(Sweden.Stockholm.TimeZone.GetUtcOffset(winter)).IsEqualTo(TimeSpan.FromHours(1));
        }
    }
}
