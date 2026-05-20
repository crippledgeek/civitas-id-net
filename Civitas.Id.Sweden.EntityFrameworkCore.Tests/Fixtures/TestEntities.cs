using Civitas.Id.Sweden.Core;

namespace Civitas.Id.Sweden.EntityFrameworkCore.Tests.Fixtures;

/// <summary>Customer entity used in SQLite roundtrip integration tests.</summary>
public sealed class Customer
{
    /// <summary>Gets or inits the primary key.</summary>
    public int Id { get; init; }

    /// <summary>Gets or inits the taxpayer identity (personnummer).</summary>
    public PersonalId TaxpayerId { get; init; } = null!;

    /// <summary>Gets or inits an optional coordination number.</summary>
    public CoordinationId? OptionalCoordinationId { get; init; }
}

/// <summary>Company entity used in SQLite roundtrip integration tests.</summary>
public sealed class Company
{
    /// <summary>Gets or inits the primary key.</summary>
    public int Id { get; init; }

    /// <summary>Gets or inits the organisation number.</summary>
    public OrganisationId OrgId { get; init; } = null!;
}
