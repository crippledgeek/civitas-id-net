using Civitas.Id.Sweden.AspNetCore.Options;
using Civitas.Id.Sweden.Format;

namespace Civitas.Id.Sweden.AspNetCore.Tests.Options;

public class CivitasIdSwedenAspNetCoreOptionsTests
{
    public class Defaults
    {
        [Test]
        public async Task JsonFormat_DefaultsTo_LongFormat()
        {
            var opts = new CivitasIdSwedenAspNetCoreOptions();
            await Assert.That(opts.JsonFormat).IsEqualTo(PnrFormat.LongFormat);
        }

        [Test]
        public async Task RedactInput_DefaultsTo_Null()
        {
            var opts = new CivitasIdSwedenAspNetCoreOptions();
            await Assert.That(opts.RedactInput).IsNull();
        }
    }

    public class Validator
    {
        [Test]
        public async Task Accepts_LongFormat()
        {
            var v = new ValidateCivitasIdSwedenAspNetCoreOptions();
            var result = v.Validate(name: null,
                options: new CivitasIdSwedenAspNetCoreOptions { JsonFormat = PnrFormat.LongFormat });
            await Assert.That(result.Succeeded).IsTrue();
        }

        [Test]
        public async Task Accepts_ShortFormat()
        {
            var v = new ValidateCivitasIdSwedenAspNetCoreOptions();
            var result = v.Validate(name: null,
                options: new CivitasIdSwedenAspNetCoreOptions { JsonFormat = PnrFormat.ShortFormat });
            await Assert.That(result.Succeeded).IsTrue();
        }

        [Test]
        public async Task Accepts_ShortFormatWithSeparator()
        {
            var v = new ValidateCivitasIdSwedenAspNetCoreOptions();
            var result = v.Validate(name: null,
                options: new CivitasIdSwedenAspNetCoreOptions
                {
                    JsonFormat = PnrFormat.ShortFormatWithSeparator
                });
            await Assert.That(result.Succeeded).IsTrue();
        }

        [Test]
        public async Task Rejects_OutOfRangeEnumValue()
        {
            var v = new ValidateCivitasIdSwedenAspNetCoreOptions();
            var result = v.Validate(name: null,
                options: new CivitasIdSwedenAspNetCoreOptions { JsonFormat = (PnrFormat)999 });
            await Assert.That(result.Failed).IsTrue();
        }

        [Test]
        public async Task Rejects_EmptyProblemDetailsTypeBaseUri()
        {
            // [Required(AllowEmptyStrings = false)] on ProblemDetailsTypeBaseUri must fire.
            var v = new ValidateCivitasIdSwedenAspNetCoreOptions();
            var result = v.Validate(name: null,
                options: new CivitasIdSwedenAspNetCoreOptions { ProblemDetailsTypeBaseUri = "" });
            await Assert.That(result.Failed).IsTrue();
        }

        [Test]
        public async Task Accepts_DefaultProblemDetailsTypeBaseUri()
        {
            // The default value "https://civitas-id.dev/errors/" satisfies [Required].
            var v = new ValidateCivitasIdSwedenAspNetCoreOptions();
            var result = v.Validate(name: null, options: new CivitasIdSwedenAspNetCoreOptions());
            await Assert.That(result.Succeeded).IsTrue();
        }
    }
}
