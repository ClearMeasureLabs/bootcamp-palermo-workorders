using AutoBogus;
using Bogus.Extensions;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;

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
                order.Description = order.Description.ClampLength(1, 2000);
                order.Number = order.Number.ClampLength(1, 5);
                // order.Status = context.Faker.PickRandom<WorkOrderStatus>(WorkOrderStatus.GetAllItems());
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