using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Extensions;

namespace Civitas.Id.Sweden.Tests.Extensions;

/// <summary>Tests for <see cref="NullableExtensions" />.</summary>
public class NullableExtensionsTests
{
    private const string ValidPin = "189001019802";

    public class MapTests
    {
        [Test]
        public async Task NonNull_AppliesFunction()
        {
            var id = PersonalId.Parse(ValidPin);
            var result = id.Map(x => x.LongFormat());
            await Assert.That(result).IsEqualTo("189001019802");
        }

        [Test]
        public async Task Null_ReturnsNull()
        {
            PersonalId? id = null;
            var result = id.Map(x => x.LongFormat());
            await Assert.That(result).IsNull();
        }

        [Test]
        public async Task NullFunction_ThrowsArgumentNullException()
        {
            var id = PersonalId.Parse(ValidPin);
            await Assert.That(() => id.Map<PersonalId, string>(null!))
                .Throws<ArgumentNullException>();
        }
    }

    public class FilterTests
    {
        [Test]
        public async Task NonNull_PredicateTrue_ReturnsValue()
        {
            var id = PersonalId.Parse(ValidPin);
            var filtered = id.Filter(x => x.IsFemale);
            await Assert.That(filtered).IsNotNull();
        }

        [Test]
        public async Task NonNull_PredicateFalse_ReturnsNull()
        {
            var id = PersonalId.Parse(ValidPin);
            var filtered = id.Filter(x => x.IsMale);
            await Assert.That(filtered).IsNull();
        }

        [Test]
        public async Task Null_ReturnsNull()
        {
            PersonalId? id = null;
            var filtered = id.Filter(x => x.IsFemale);
            await Assert.That(filtered).IsNull();
        }

        [Test]
        public async Task NullPredicate_ThrowsArgumentNullException()
        {
            var id = PersonalId.Parse(ValidPin);
            await Assert.That(() => id.Filter(null!))
                .Throws<ArgumentNullException>();
        }
    }

    public class MatchTests
    {
        [Test]
        public async Task NonNull_CallsSome()
        {
            var id = PersonalId.Parse(ValidPin);
            var result = id.Match(_ => "yes", () => "no");
            await Assert.That(result).IsEqualTo("yes");
        }

        [Test]
        public async Task Null_CallsNone()
        {
            PersonalId? id = null;
            var result = id.Match(_ => "yes", () => "no");
            await Assert.That(result).IsEqualTo("no");
        }

        [Test]
        public async Task ValueTypeResult_Works()
        {
            // Match's TResult is unconstrained — supports value types like int.
            var id = PersonalId.Parse(ValidPin);
            var age = id.Match(_ => 42, () => -1);
            await Assert.That(age).IsEqualTo(42);
        }

        [Test]
        public async Task NullSome_ThrowsArgumentNullException()
        {
            var id = PersonalId.Parse(ValidPin);
            await Assert.That(() => id.Match<PersonalId, string>(null!, () => "x"))
                .Throws<ArgumentNullException>();
        }

        [Test]
        public async Task NullNone_ThrowsArgumentNullException()
        {
            PersonalId? id = null;
            await Assert.That(() => id.Match<PersonalId, string>(_ => "y", null!))
                .Throws<ArgumentNullException>();
        }
    }

    public class BindTests
    {
        [Test]
        public async Task NonNull_AppliesChain()
        {
            var id = PersonalId.Parse(ValidPin);
            // Bind to ToOrganisationId — a non-null-returning chain
            var org = id.Bind(OrganisationId? (x) => x.ToOrganisationId());
            await Assert.That(org).IsNotNull();
        }

        [Test]
        public async Task Null_ShortCircuits()
        {
            PersonalId? id = null;
            var org = id.Bind(OrganisationId? (x) => x.ToOrganisationId());
            await Assert.That(org).IsNull();
        }

        [Test]
        public async Task ChainReturnsNull_PropagatesNull()
        {
            var id = PersonalId.Parse(ValidPin);
            // Chain function returns null — Bind preserves it.
            var org = id.Bind<PersonalId, OrganisationId>(_ => null);
            await Assert.That(org).IsNull();
        }

        [Test]
        public async Task NullFunction_ThrowsArgumentNullException()
        {
            var id = PersonalId.Parse(ValidPin);
            await Assert.That(() => id.Bind<PersonalId, OrganisationId>(null!))
                .Throws<ArgumentNullException>();
        }
    }
}
