namespace ClearMeasure.Bootcamp.Core.Model
{
    public class Role(string name, bool canCreate, bool canFulfill) : EntityBase<Role>
    {
        public Role() : this(null!, false, false)
        {
        }

        public string Name { get; set; } = name;

        public bool CanCreateWorkOrder { get; set; } = canCreate;

        public bool CanFulfillWorkOrder { get; set; } = canFulfill;
        public override Guid Id { get; set; }
    }
}