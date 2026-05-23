using Civitas.Id.Sweden.Core;
using Civitas.Id.Sweden.Format;

// Minimum viable exercise of the public API. TrimmerRootAssembly in the csproj
// forces full ILC analysis of Civitas.Id.Sweden regardless of what is called
// here, so this is a smoke check rather than a coverage check.

if (!PersonalId.TryParse("189001019802", out var pid))
{
    Console.Error.WriteLine("PersonalId.TryParse smoke test failed");
    return 1;
}
Console.WriteLine($"PersonalId: {pid.Format(PnrFormat.LongFormat)} BirthDate={pid.BirthDate} IsAdult={pid.IsAdult(new DateOnly(2026, 5, 20))}");

if (!CoordinationId.TryParse("189001610006", out var cid))
{
    Console.Error.WriteLine("CoordinationId.TryParse smoke test failed");
    return 1;
}
Console.WriteLine($"CoordinationId: {cid.Format(PnrFormat.LongFormat)} BirthDate={cid.BirthDate}");

if (!OrganisationId.TryParse("5560360793", out var oid))
{
    Console.Error.WriteLine("OrganisationId.TryParse smoke test failed");
    return 1;
}
Console.WriteLine($"OrganisationId: {oid.LongFormat()} Form={oid.Form}");

// Round-trip
var roundTrip = PersonalId.Parse(pid.ToString());
if (roundTrip != pid)
{
    Console.Error.WriteLine("PersonalId round-trip failed");
    return 1;
}

Console.WriteLine("AOT smoke test OK");
return 0;
