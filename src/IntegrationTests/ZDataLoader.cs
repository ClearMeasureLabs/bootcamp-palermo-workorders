using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.IntegrationTests.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.IntegrationTests;

[TestFixture]
public class ZDataLoader
{
    [Test]
    public void LoadData()
    {
        new DatabaseTests().Clean();
        var lead = new Role("Facility Lead", true, false);
        var fulfillment = new Role("Fulfillment", false, true);
        var db = TestHost.GetRequiredService<DbContext>();
        db.Add(lead);
        db.Add(fulfillment);
        db.SaveChanges();

        //Trainer1
        var jpalermo = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffreypalermo@yahoo.com");
        jpalermo.AddRole(lead);
        jpalermo.AddRole(fulfillment);
        db.Add(jpalermo);

        //Person 1

        //Person 2

        //Person 3

        //Person 4

        //Person 5

        //Person 6

        //Person 7

        //Person 8

        //Person 9

        //Person 10

        //Person 11

        //Person 12

        //Person 13

        var hsimpson = new Employee("hsimpson", "Homer", "Simpson", "homer@simpson.com");
        hsimpson.AddRole(lead);
        hsimpson.AddRole(fulfillment);
        db.Add(hsimpson);

        db.SaveChanges();
        db.Dispose();

        LoadSimpsonsChurchData();
    }

    private void LoadSimpsonsChurchData()
    {
        var db = TestHost.GetRequiredService<DbContext>();

        // Create church-related roles
        var minister = new Role("Minister", true, true);
        var deacon = new Role("Deacon", false, true);
        var choir = new Role("Choir Member", false, false);
        var organist = new Role("Church Organist", false, true);
        var parishioner = new Role("Parishioner", false, false);
        var groundskeeper = new Role("Groundskeeper", false, true);

        db.Add(minister);
        db.Add(deacon);
        db.Add(choir);
        db.Add(organist);
        db.Add(parishioner);
        db.Add(groundskeeper);

        // Reverend Timothy Lovejoy Jr - Main focus character
        var revLovejoy = new Employee("tlovejoy", "Timothy", "Lovejoy Jr", "reverend@firstchurchspringfield.org");
        revLovejoy.AddRole(minister);
        db.Add(revLovejoy);

        // Helen Lovejoy - Minister's wife
        var helenLovejoy = new Employee("hlovejoy", "Helen", "Lovejoy", "helen@firstchurchspringfield.org");
        helenLovejoy.AddRole(parishioner);
        db.Add(helenLovejoy);

        // Jessica Lovejoy - Minister's daughter
        var jessicaLovejoy = new Employee("jlovejoy", "Jessica", "Lovejoy", "jessica@springfield.edu");
        jessicaLovejoy.AddRole(parishioner);
        db.Add(jessicaLovejoy);

        // Ned Flanders - Deacon and faithful parishioner
        var nedFlanders = new Employee("nflanders", "Ned", "Flanders", "neddy@okily.dokily.com");
        nedFlanders.AddRole(deacon);
        nedFlanders.AddRole(parishioner);
        db.Add(nedFlanders);

        // Maude Flanders - Choir member (though deceased in later seasons, including for completeness)
        var maudeFlanders = new Employee("mflanders", "Maude", "Flanders", "maude@okily.dokily.com");
        maudeFlanders.AddRole(choir);
        maudeFlanders.AddRole(parishioner);
        db.Add(maudeFlanders);

        // Rod Flanders - Young parishioner
        var rodFlanders = new Employee("rflanders", "Rod", "Flanders", "rod@okily.dokily.com");
        rodFlanders.AddRole(parishioner);
        db.Add(rodFlanders);

        // Todd Flanders - Young parishioner
        var toddFlanders = new Employee("tflanders", "Todd", "Flanders", "todd@okily.dokily.com");
        toddFlanders.AddRole(parishioner);
        db.Add(toddFlanders);

        // Marge Simpson - Occasional church attendee
        var margeSimpson = new Employee("msimpson", "Marge", "Simpson", "marge@simpson.com");
        margeSimpson.AddRole(parishioner);
        db.Add(margeSimpson);

        // Lisa Simpson - Thoughtful young parishioner
        var lisaSimpson = new Employee("lsimpson", "Lisa", "Simpson", "lisa@simpson.com");
        lisaSimpson.AddRole(parishioner);
        db.Add(lisaSimpson);

        // Groundskeeper Willie - Church groundskeeper and maintenance
        var groundskeeperWillie = new Employee("gwillie", "Groundskeeper Willie", "MacDougal",
            "willie@springfieldelementary.edu");
        groundskeeperWillie.AddRole(groundskeeper);
        db.Add(groundskeeperWillie);

        // Church organist (generic character for church services)
        var organistEmployee = new Employee("gfeesh", "Gertie", "Feesh", "gertie@firstchurchspringfield.org");
        organistEmployee.AddRole(organist);
        db.Add(organistEmployee);

        // Apu Nahasapeemapetilon - Represents religious diversity but attends some services
        var apuNahasapeemapetilon =
            new Employee("anahasapeemapetilon", "Apu", "Nahasapeemapetilon", "apu@kwikmart.com");
        apuNahasapeemapetilon.AddRole(parishioner);
        db.Add(apuNahasapeemapetilon);

        // Moe Szyslak - Rarely attends church but included for completeness
        var moeSzyslak = new Employee("mszyslak", "Moe", "Szyslak", "moe@moestab.com");
        moeSzyslak.AddRole(parishioner);
        db.Add(moeSzyslak);

        // Lenny Leonard - Occasional church attendee
        var lennyLeonard = new Employee("lleonard", "Lenny", "Leonard", "lenny@powerplant.com");
        lennyLeonard.AddRole(parishioner);
        db.Add(lennyLeonard);

        // Ms. Albright - Church basement sunday school teacher
        var msAlbright = new Employee("malbright", "Ms.", "Albright", "albright@firstchurchspringfield.com");
        msAlbright.AddRole(choir);
        db.Add(msAlbright);

        db.SaveChanges();

        // Create Christmas Concert Work Orders
        var christmasOrder1 = new WorkOrder();
        christmasOrder1.Number = Guid.NewGuid().ToString().Substring(0, 5).ToUpper();
        christmasOrder1.Creator = revLovejoy;
        christmasOrder1.Assignee = maudeFlanders;
        christmasOrder1.Status = WorkOrderStatus.Draft;
        christmasOrder1.Title = "Organize Christmas Concert Choir Practice Schedule";
        christmasOrder1.Description =
            "Coordinate weekly choir rehearsals for the Christmas concert. Schedule practice sessions for November and December leading up to the Christmas Eve service.";
        christmasOrder1.CreatedDate = new DateTime(2024, 10, 15, 9, 0, 0);
        christmasOrder1.RoomNumber = "Sanctuary";
        db.Add(christmasOrder1);

        var christmasOrder2 = new WorkOrder();
        christmasOrder2.Number = Guid.NewGuid().ToString().Substring(0, 5).ToUpper();
        christmasOrder2.Creator = revLovejoy;
        christmasOrder2.Assignee = groundskeeperWillie;
        christmasOrder2.Status = WorkOrderStatus.Assigned;
        christmasOrder2.Title = "Prepare Church Grounds for Christmas Decorations";
        christmasOrder2.Description =
            "Clean and prepare the church exterior and landscaping for Christmas decorations. Ensure proper lighting infrastructure and safe walkways for concert attendees.";
        christmasOrder2.CreatedDate = new DateTime(2024, 11, 1, 8, 0, 0);
        christmasOrder2.AssignedDate = new DateTime(2024, 11, 2, 10, 0, 0);
        christmasOrder2.RoomNumber = "Exterior Grounds";
        db.Add(christmasOrder2);

        var christmasOrder3 = new WorkOrder();
        christmasOrder3.Number = Guid.NewGuid().ToString().Substring(0, 5).ToUpper();
        christmasOrder3.Creator = nedFlanders;
        christmasOrder3.Assignee = organistEmployee;
        christmasOrder3.Status = WorkOrderStatus.InProgress;
        christmasOrder3.Title = "Tune and Maintain Church Organ for Christmas Concert";
        christmasOrder3.Description =
            "Perform complete maintenance and tuning of the church organ in preparation for Christmas concert performances. Test all stops and ensure optimal sound quality.";
        christmasOrder3.CreatedDate = new DateTime(2024, 11, 5, 14, 0, 0);
        christmasOrder3.AssignedDate = new DateTime(2024, 11, 6, 9, 0, 0);
        christmasOrder3.RoomNumber = "Sanctuary Organ Loft";
        db.Add(christmasOrder3);

        var christmasOrder4 = new WorkOrder();
        christmasOrder4.Number = Guid.NewGuid().ToString().Substring(0, 5).ToUpper();
        christmasOrder4.Creator = revLovejoy;
        christmasOrder4.Assignee = nedFlanders;
        christmasOrder4.Status = WorkOrderStatus.Draft;
        christmasOrder4.Title = "Setup Audio System for Christmas Concert";
        christmasOrder4.Description =
            "Configure and test the sanctuary sound system for the Christmas concert. Ensure microphones, speakers, and recording equipment are functioning properly.";
        christmasOrder4.CreatedDate = new DateTime(2024, 11, 10, 16, 0, 0);
        christmasOrder4.RoomNumber = "Sanctuary";
        db.Add(christmasOrder4);

        var christmasOrder5 = new WorkOrder();
        christmasOrder5.Number = Guid.NewGuid().ToString().Substring(0, 5).ToUpper();
        christmasOrder5.Creator = nedFlanders;
        christmasOrder5.Assignee = groundskeeperWillie;
        christmasOrder5.Status = WorkOrderStatus.Complete;
        christmasOrder5.Title = "Install Christmas Tree in Sanctuary";
        christmasOrder5.Description =
            "Select, transport, and install the Christmas tree in the sanctuary. Ensure proper placement and safety for the Christmas concert and services.";
        christmasOrder5.CreatedDate = new DateTime(2024, 12, 1, 10, 0, 0);
        christmasOrder5.AssignedDate = new DateTime(2024, 12, 1, 11, 0, 0);
        christmasOrder5.CompletedDate = new DateTime(2024, 12, 3, 15, 0, 0);
        christmasOrder5.RoomNumber = "Sanctuary";
        db.Add(christmasOrder5);

        var christmasOrder6 = new WorkOrder();
        christmasOrder6.Number = Guid.NewGuid().ToString().Substring(0, 5).ToUpper();
        christmasOrder6.Creator = revLovejoy;
        christmasOrder6.Assignee = maudeFlanders;
        christmasOrder6.Status = WorkOrderStatus.Assigned;
        christmasOrder6.Title = "Coordinate Christmas Concert Program Design";
        christmasOrder6.Description =
            "Design and prepare printed programs for the Christmas concert including song listings, performer credits, and special acknowledgments.";
        christmasOrder6.CreatedDate = new DateTime(2024, 11, 20, 13, 0, 0);
        christmasOrder6.AssignedDate = new DateTime(2024, 11, 21, 9, 0, 0);
        christmasOrder6.RoomNumber = "Church Office";
        db.Add(christmasOrder6);

        db.SaveChanges();
    }

    public Employee CreateUser()
    {
        using var context = TestHost.GetRequiredService<DbContext>();
        var employee = TestHost.Faker<Employee>();
        employee.UserName = "current" + employee.UserName;
        employee.AddRole(new Role("admin", true, true));
        context.Add(employee);
        context.SaveChanges();
        return employee;
    }
}