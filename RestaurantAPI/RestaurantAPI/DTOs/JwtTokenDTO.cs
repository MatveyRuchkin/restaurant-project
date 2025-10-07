namespace RestaurantAPI.DTOs
{
    public class JwtTokenReadDto
    {
        public int Id { get; set; }
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public bool Revoked { get; set; }
    }

}
