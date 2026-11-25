using Civitas.Id.Sweden.Errors;
using Civitas.Id.Sweden.Tests.Helpers;

namespace Civitas.Id.Sweden.Tests.Errors;

public class InvalidIdNumberExceptionTests
{
    private static readonly string WrongChecksum = OfficialNumbers.Modern.WithWrongCheckDigit;

    public class Construction
    {
        [Test]
        public async Task DerivesFromFormatException()
        {
            var ex = new InvalidIdNumberException();
            await Assert.That(ex).IsAssignableTo<FormatException>();
        }

        [Test]
        public async Task ParameterlessCtor_HasDefaultMessage()
        {
            var ex = new InvalidIdNumberException();
            await Assert.That(ex.Message).Contains("Swedish ID");
            await Assert.That(ex.RedactedInput).IsNull();
            await Assert.That(ex.Reason).IsEqualTo(InvalidIdNumberReason.Unknown);
        }

        [Test]
        public async Task FullCtor_PreservesRedactedInputAndReason()
        {
            var ex = new InvalidIdNumberException(WrongChecksum, InvalidIdNumberReason.InvalidChecksum);
            await Assert.That(ex.Message).Contains("checksum");
            await Assert.That(ex.RedactedInput).IsEqualTo($"20******{WrongChecksum[^4..]}");
            await Assert.That(ex.Reason).IsEqualTo(InvalidIdNumberReason.InvalidChecksum);
        }

        [Test]
        public async Task InnerExceptionCtor_PreservesInner()
        {
            var inner = new InvalidOperationException("root cause");
            var ex = new InvalidIdNumberException(inner);
            await Assert.That(ex.InnerException).IsEqualTo(inner);
        }

        [Test]
        public async Task FullCtorWithInner_PreservesAllContext()
        {
            var inner = new InvalidOperationException("root");
            var ex = new InvalidIdNumberException(
                WrongChecksum, InvalidIdNumberReason.InvalidChecksum, inner);
            await Assert.That(ex.Message).Contains("checksum");
            await Assert.That(ex.RedactedInput).IsEqualTo($"20******{WrongChecksum[^4..]}");
            await Assert.That(ex.Reason).IsEqualTo(InvalidIdNumberReason.InvalidChecksum);
            await Assert.That(ex.InnerException).IsEqualTo(inner);
        }
    }

    public class MessageDoesNotLeakInput
    {
        [Test]
        public async Task Message_DoesNotContainRawInput()
        {
            var ex = new InvalidIdNumberException(WrongChecksum, InvalidIdNumberReason.InvalidChecksum);

            await Assert.That(ex.Message).DoesNotContain(WrongChecksum);
        }

        [Test]
        public async Task RedactedInput_PreservesPrefixAndSuffix_MasksMiddle()
        {
            var ex = new InvalidIdNumberException(WrongChecksum, InvalidIdNumberReason.InvalidChecksum);

            await Assert.That(ex.RedactedInput).IsEqualTo($"20******{WrongChecksum[^4..]}");
        }

        [Test]
        public async Task RedactedInput_NullInput_IsNull()
        {
            var ex = new InvalidIdNumberException(null, InvalidIdNumberReason.Empty);
            await Assert.That(ex.RedactedInput).IsNull();
        }

        [Test]
        public async Task RedactedInput_EmptyInput_IsEmptyPlaceholder()
        {
            var ex = new InvalidIdNumberException(string.Empty, InvalidIdNumberReason.Empty);
            await Assert.That(ex.RedactedInput).IsEqualTo("[empty]");
        }

        [Test]
        public async Task RedactedInput_VeryShortInput_AllAsterisks()
        {
            var ex = new InvalidIdNumberException("abc", InvalidIdNumberReason.InvalidLength);
            await Assert.That(ex.RedactedInput).IsEqualTo("***");
        }

        [Test]
        public async Task Message_DescribesReason()
        {
            var ex = new InvalidIdNumberException(WrongChecksum, InvalidIdNumberReason.InvalidChecksum);
            await Assert.That(ex.Message).Contains("checksum");
        }
    }

    public class BclConventionConstructors
    {
        [Test]
        public async Task ConstructorWithMessage_ExposesMessage()
        {
            var ex = new InvalidIdNumberException("test message");

            await Assert.That(ex.Message).IsEqualTo("test message");
            await Assert.That(ex.RedactedInput).IsNull();
            await Assert.That(ex.Reason).IsEqualTo(InvalidIdNumberReason.Unknown);
        }

        [Test]
        public async Task ConstructorWithMessageAndInner_ExposesBoth()
        {
            var inner = new InvalidOperationException("root cause");
            var ex = new InvalidIdNumberException("test", inner);

            await Assert.That(ex.Message).IsEqualTo("test");
            await Assert.That(ex.InnerException).IsSameReferenceAs(inner);
            await Assert.That(ex.RedactedInput).IsNull();
            await Assert.That(ex.Reason).IsEqualTo(InvalidIdNumberReason.Unknown);
        }
    }

    public class BuildMessageArmCoverage
    {
        [Test]
        public async Task UnknownOrganisationForm_BuildMessage_IncludesOrganisationFormPhrase()
        {
            var ex = new InvalidIdNumberException(
                input: "1699999999",
                reason: InvalidIdNumberReason.UnknownOrganisationForm);

            await Assert.That(ex.Message).Contains("organisation form");
        }
    }
}
