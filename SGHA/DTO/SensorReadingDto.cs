namespace SGHA.DTO
{
    public class SensorReadingDto
    {
        public string SensorType { get; set; }
        public float SensorValue { get; set; }
        public string Unit { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
