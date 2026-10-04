using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.Constants;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.IntegrationTests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// ReSharper disable once CheckNamespace -- outside ClearMeasure.Bootcamp.IntegrationTests on purpose: that namespace's
// SetUpFixture starts the test host (an NServiceBus endpoint that installs its queues), which must never run against
// the database of a deployed environment.
namespace ClearMeasure.Bootcamp.DemoData;

/// <summary>
/// The demo employees and their roles, kept in one list: <see cref="ZDataLoader"/> loads them into an emptied test
/// database, and <see cref="SeedDemoEmployees"/> adds them to the database of any other environment, for example from a
/// deployment step. Seeding only inserts: a role whose name exists, an employee whose user name exists and an existing
/// employee-role link are left as they are, and nothing is updated or deleted.
/// </summary>
[TestFixture]
public class DemoEmployeeSeeder
{
    private const string FacilityLead = "Facility Lead";
    private const string Fulfillment = "Fulfillment";
    private const string Minister = "Minister";
    private const string Deacon = "Deacon";
    private const string ChoirMember = "Choir Member";
    private const string ChurchOrganist = "Church Organist";
    private const string Parishioner = "Parishioner";
    private const string Groundskeeper = "Groundskeeper";

    private static readonly DemoRole[] DemoRoles =
    [
        new(FacilityLead, true, false),
        new(Fulfillment, false, true),
        new(Roles.Bot, false, true),
        new(Minister, true, true),
        new(Deacon, false, true),
        new(ChoirMember, false, false),
        new(ChurchOrganist, false, true),
        new(Parishioner, false, false),
        new(Groundskeeper, false, true)
    ];

    private static readonly DemoEmployee[] DemoEmployees =
    [
        new("jpalermo", "Jeffrey", "Palermo", "jeffreypalermo@yahoo.com", null, [FacilityLead, Fulfillment]),
        new("sspaniel", "Sean", "Spaniel", "sean.spaniel@clear-measure.com", null, [FacilityLead, Fulfillment]),
        new("aibot", "AI", Roles.Bot, "aibot@system.local", null, [Roles.Bot]),
        new("jcuevas", "Joe", "Cuevas", "joecuevasjr@gmail.com", "es-ES", [Fulfillment]),
        new("bsides", "Bart", "Sides", "bartsides@gmail.com", null, [FacilityLead, Fulfillment]),
        new("cklaips", "Casey", "Klaips", "cklaips@gmail.com", null, [FacilityLead, Fulfillment]),
        new("csullivan", "Cole", "Sullivan", "cole.sullivan@biberk.com", null, [FacilityLead, Fulfillment]),
        new("nlarsen", "Nick", "Larsen", "nick@larsen.com", "de-DE", [FacilityLead, Fulfillment]),
        new("pludecker", "Paige", "Ludecker", "pludecker@gmail.com", "de-DE", [FacilityLead, Fulfillment]),
        new("hsimpson", "Homer", "Simpson", "homer@simpson.com", null, [FacilityLead, Fulfillment]),
        new("ndoughton", "Noah", "Doughton", "noah.doughton@biberk.com", null, [FacilityLead, Fulfillment]),
        new("will", "Will", "Perea", "wperea@setworks.com", "es-ES", [FacilityLead, Fulfillment]),
        new("tlovejoy", "Timothy", "Lovejoy Jr", "reverend@firstchurchspringfield.org", null, [Minister]),
        new("hlovejoy", "Helen", "Lovejoy", "helen@firstchurchspringfield.org", null, [Parishioner]),
        new("jlovejoy", "Jessica", "Lovejoy", "jessica@springfield.edu", null, [Parishioner]),
        new("nflanders", "Ned", "Flanders", "neddy@okily.dokily.com", null, [Deacon, Parishioner]),
        new("mflanders", "Maude", "Flanders", "maude@okily.dokily.com", null, [ChoirMember, Parishioner]),
        new("rflanders", "Rod", "Flanders", "rod@okily.dokily.com", null, [Parishioner]),
        new("tflanders", "Todd", "Flanders", "todd@okily.dokily.com", null, [Parishioner]),
        new("msimpson", "Marge", "Simpson", "marge@simpson.com", null, [Parishioner]),
        new("lsimpson", "Lisa", "Simpson", "lisa@simpson.com", null, [Parishioner]),
        new("gwillie", "Groundskeeper Willie", "MacDougal", "willie@springfieldelementary.edu", null, [Groundskeeper]),
        new("gfeesh", "Gertie", "Feesh", "gertie@firstchurchspringfield.org", null, [ChurchOrganist]),
        new("anahasapeemapetilon", "Apu", "Nahasapeemapetilon", "apu@kwikmart.com", null, [Parishioner]),
        new("mszyslak", "Moe", "Szyslak", "moe@moestab.com", null, [Parishioner]),
        new("lleonard", "Lenny", "Leonard", "lenny@powerplant.com", null, [Parishioner]),
        new("malbright", "Ms.", "Albright", "albright@firstchurchspringfield.com", null, [ChoirMember])
    ];

    /// <summary>
    /// Adds the demo employees to the database in ConnectionStrings:SqlConnectionString (appsettings.test.json, or the
    /// environment variable ConnectionStrings__SqlConnectionString) and writes the counts to the test output. Explicit:
    /// it runs only when selected by name, for example
    /// <c>dotnet test --filter FullyQualifiedName=ClearMeasure.Bootcamp.DemoData.DemoEmployeeSeeder.SeedDemoEmployees</c>.
    /// </summary>
    [Test]
    [Explicit("Writes to the database in ConnectionStrings:SqlConnectionString; run it by name to seed an environment.")]
    public void SeedDemoEmployees()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.test.json", true)
            .AddEnvironmentVariables()
            .Build();
        using var context = new DataContext(new TestDatabaseConfiguration(configuration));

        var result = Seed(context);

        TestContext.Out.WriteLine(result.ToSummary());
    }

    /// <summary>
    /// Inserts the demo roles, employees and employee-role links that the database does not have yet, in one
    /// transaction. Roles are matched by name and employees by user name; existing rows are never changed.
    /// </summary>
    public static DemoEmployeeSeedResult Seed(DbContext context)
    {
        var roles = new Dictionary<string, Role>();
        var rolesAdded = 0;
        foreach (var demoRole in DemoRoles)
        {
            var role = context.Set<Role>().FirstOrDefault(r => r.Name == demoRole.Name);
            if (role is null)
            {
                role = new Role(demoRole.Name, demoRole.CanCreate, demoRole.CanFulfill);
                context.Add(role);
                rolesAdded++;
            }

            roles.Add(demoRole.Name, role);
        }

        var employeesAdded = 0;
        var linksAdded = 0;
        foreach (var demoEmployee in DemoEmployees)
        {
            var employee = context.Set<Employee>().Include(e => e.Roles)
                .FirstOrDefault(e => e.UserName == demoEmployee.UserName);
            if (employee is null)
            {
                employee = new Employee(demoEmployee.UserName, demoEmployee.FirstName, demoEmployee.LastName,
                    demoEmployee.EmailAddress);
                if (demoEmployee.PreferredLanguage is not null)
                {
                    employee.PreferredLanguage = demoEmployee.PreferredLanguage;
                }

                context.Add(employee);
                employeesAdded++;
            }

            linksAdded += AddMissingRoles(employee, demoEmployee.Roles.Select(name => roles[name]));
        }

        context.SaveChanges();
        return new DemoEmployeeSeedResult(rolesAdded, DemoRoles.Length - rolesAdded, employeesAdded,
            DemoEmployees.Length - employeesAdded, linksAdded);
    }

    private static int AddMissingRoles(Employee employee, IEnumerable<Role> demoRoles)
    {
        var added = 0;
        foreach (var role in demoRoles)
        {
            if (employee.Roles.Any(existing => existing.Id == role.Id
                    || string.Equals(existing.Name, role.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            employee.AddRole(role);
            added++;
        }

        return added;
    }

    private sealed record DemoRole(string Name, bool CanCreate, bool CanFulfill);

    private sealed record DemoEmployee(
        string UserName,
        string FirstName,
        string LastName,
        string EmailAddress,
        string? PreferredLanguage,
        string[] Roles);
}

/// <summary>
/// What <see cref="DemoEmployeeSeeder.Seed"/> inserted, and how many demo roles and employees were already there.
/// </summary>
public sealed record DemoEmployeeSeedResult(
    int RolesAdded,
    int RolesFound,
    int EmployeesAdded,
    int EmployeesFound,
    int RoleLinksAdded)
{
    /// <summary>
    /// One line for the deployment log.
    /// </summary>
    public string ToSummary()
    {
        return $"Demo employees: {EmployeesAdded} added, {EmployeesFound} already there; roles: {RolesAdded} added, "
               + $"{RolesFound} already there; employee-role links: {RoleLinksAdded} added.";
    }
}
