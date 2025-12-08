namespace DDACAssignment.Dtos.TokenRequest
{
    public class TokenResponseDto
    {
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
        public required string Role { get; set; }
    }
}
