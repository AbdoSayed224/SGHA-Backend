namespace SGHA.DTO
{
    public class AIResultDto
    {
        public string? Model { get; set; }
        public string? Detection { get; set; }
        public bool? IsRipe { get; set; }
        public bool? IsHealthy { get; set; }
        public bool? IsPest { get; set; }
        public string? ActionNeeded { get; set; }
    }
}
