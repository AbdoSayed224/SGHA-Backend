namespace SGHA.DTO
{
    public class SensorDto
    {
        public int? SensorID { get; set; }
        public int HouseID { get; set; }
        public string SensorType { get; set; }
        public string SensorName { get; set; }
        public string SensorLocation { get; set; }
        public double? SensorValue { get; set; }
        public string Unit { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
