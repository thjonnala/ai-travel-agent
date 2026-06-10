using System.Diagnostics.CodeAnalysis;

// EF Core migrations and the model snapshot are generated code; excluding them
// here (via the partial declarations) keeps coverage numbers honest without
// touching the generated files themselves.
namespace TravelAgent.Infrastructure.Persistence.Migrations;

[ExcludeFromCodeCoverage]
public partial class InitialCreate;

[ExcludeFromCodeCoverage]
public partial class TravelAgentDbContextModelSnapshot;
