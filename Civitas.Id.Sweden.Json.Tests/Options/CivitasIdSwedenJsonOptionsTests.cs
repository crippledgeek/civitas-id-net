using Civitas.Id.Sweden.Format;
using Civitas.Id.Sweden.Json.Options;

namespace Civitas.Id.Sweden.Json.Tests.Options;

public class CivitasIdSwedenJsonOptionsTests
{
    public class Defaults
    {
        [Test]
        public async Task PersonnummerFormat_DefaultsTo_LongFormat()
        {
            var opts = new CivitasIdSwedenJsonOptions();
            await Assert.That(opts.PersonnummerFormat).IsEqualTo(PnrFormat.LongFormat);
        }

        [Test]
        public async Task OrganisationIdLongFormat_DefaultsTo_True()
        {
            var opts = new CivitasIdSwedenJsonOptions();
            await Assert.That(opts.OrganisationIdLongFormat).IsTrue();
        }
    }

    public class Validator
    {
        [Test]
        public async Task Accepts_LongFormat()
        {
            var v = new ValidateCivitasIdSwedenJsonOptions();
            var result = v.Validate(name: null,
                options: new CivitasIdSwedenJsonOptions { PersonnummerFormat = PnrFormat.LongFormat });
            await Assert.That(result.Succeeded).IsTrue();
        }

        [Test]
        public async Task Accepts_ShortFormat()
        {
            var v = new ValidateCivitasIdSwedenJsonOptions();
            var result = v.Validate(name: null,
                options: new CivitasIdSwedenJsonOptions { PersonnummerFormat = PnrFormat.ShortFormat });
            await Assert.That(result.Succeeded).IsTrue();
        }

        [Test]
        public async Task Rejects_OutOfRangeEnumValue()
        {
            var v = new ValidateCivitasIdSwedenJsonOptions();
            var result = v.Validate(name: null,
                options: new CivitasIdSwedenJsonOptions { PersonnummerFormat = (PnrFormat)999 });
            await Assert.That(result.Failed).IsTrue();
        }
    }
}
