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
            case WorkOrder order:
                order.Number = new WorkOrderNumberGenerator().GenerateNumber();
                order.Title = order.Title.ClampLength(1, 200);        // HasMaxLength(200)
                order.Description = order.Description.ClampLength(1, 4000); // HasMaxLength(4000)
                order.RoomNumber = order.RoomNumber.ClampLength(1, 50);     // HasMaxLength(50)
                break;
            case WorkOrderStatus:
                context.Instance = context.Faker.PickRandom<WorkOrderStatus>(WorkOrderStatus.GetAllItems());
                break;
            case WorkOrderSpecificationQuery query:
                query.StatusKey = context.Faker.PickRandom<WorkOrderStatus>(WorkOrderStatus.GetAllItems()).Key;
                break;
        }
    }
}