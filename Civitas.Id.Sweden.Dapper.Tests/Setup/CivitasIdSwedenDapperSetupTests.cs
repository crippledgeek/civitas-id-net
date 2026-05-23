namespace Civitas.Id.Sweden.Dapper.Tests.Setup;

public class CivitasIdSwedenDapperSetupTests
{
    public class RegisterBehavior
    {
        [Test]
        public void Register_DoesNotThrow()
        {
            CivitasIdSwedenDapperSetup.Register();
        }

        // Calling Register twice in a row is safe — Dapper's AddTypeHandler
        // is idempotent for the same (type, handler) pair (overwrites if a
        // different handler were registered).
        [Test]
        public void Register_IsIdempotent()
        {
            CivitasIdSwedenDapperSetup.Register();
            CivitasIdSwedenDapperSetup.Register();
        }
    }
}
