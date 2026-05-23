namespace Civitas.Id.Sweden.Dapper.Tests.Fixtures;

using Civitas.Id.Sweden.Core;

public sealed class Customer
{
    public Guid Id { get; init; }
    public PersonalId TaxpayerId { get; init; } = null!;
    public PersonalId? OptionalSecondaryId { get; init; }
}

public sealed class CoordinationHolder
{
    public Guid Id { get; init; }
    public CoordinationId CoordId { get; init; } = null!;
    public CoordinationId? OptionalCoord { get; init; }
}

public sealed class Company
{
    public Guid Id { get; init; }
    public OrganisationId OrgId { get; init; } = null!;
    public OrganisationId? OptionalSubsidiaryOrgId { get; init; }
}
