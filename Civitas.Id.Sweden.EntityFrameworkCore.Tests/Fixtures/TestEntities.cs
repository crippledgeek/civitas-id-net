namespace Civitas.Id.Sweden.EntityFrameworkCore.Tests.Fixtures;

using Civitas.Id.Sweden.Core;

/// <summary>Customer entity used in SQLite roundtrip integration tests.</summary>
public sealed class Customer
{
    /// <summary>Gets or sets the primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the taxpayer identity (personnummer).</summary>
    public PersonalId TaxpayerId { get; set; } = null!;

    /// <summary>Gets or sets an optional coordination number.</summary>
    public CoordinationId? OptionalCoordinationId { get; set; }
}

/// <summary>Company entity used in SQLite roundtrip integration tests.</summary>
public sealed class Company
{
    /// <summary>Gets or sets the primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the organisation number.</summary>
    public OrganisationId OrgId { get; set; } = null!;
}
