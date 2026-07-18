using AutoBogus;
using Bogus.Extensions;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services.Impl;

namespace ClearMeasure.Bootcamp.UnitTests;

internal class BogusOverrides : AutoGeneratorOverride
{
    public override bool CanOverride(AutoGenerateContext context)
    {
        return true;
    }

    public override void Generate(AutoGenerateOverrideContext context)
    {
        switch (context.Instance)
        {
            case WorkRequest order:
                order.Number = new WorkRequestNumberGenerator().GenerateNumber();
                order.Title = order.Title.ClampLength(1, 200);        // HasMaxLength(200)
                order.Description = order.Description.ClampLength(1, 4000); // HasMaxLength(4000)
                order.RoomNumber = order.RoomNumber.ClampLength(1, 50);     // HasMaxLength(50)
                break;
            case WorkRequestStatus:
                context.Instance = context.Faker.PickRandom<WorkRequestStatus>(WorkRequestStatus.GetAllItems());
                break;
            case WorkRequestSpecificationQuery query:
                query.StatusKey = context.Faker.PickRandom<WorkRequestStatus>(WorkRequestStatus.GetAllItems()).Key;
                break;
            case Employee employee:
                employee.PreferredLanguage = context.Faker.PickRandom("en-US", "es-ES", "fr-FR", "de-DE", "pt-BR");
                break;
        }
    }
}