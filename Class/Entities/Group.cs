namespace Entities
{
    public class Group
    {
        public required string Id { get; set; }
        public required List<Entities.User> Members { get; set; }
    }
}
