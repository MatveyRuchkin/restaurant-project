namespace RestaurantAPI.DTOs
{
    public class LogReadDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Action { get; set; } = null!;
        public string TableName { get; set; } = null!;
        public int? RecordId { get; set; }
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
