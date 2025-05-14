namespace SGHA.DTO
{
    public class ControlOptionDto
    {
        public int ControlID { get; set; }
        public int HouseID { get; set; }
        public int FanStatus { get; set; }
        public int LightStatus { get; set; }
        public int WaterStatus { get; set; }
        public string? Note { get; set; }
        public int IsAutomated { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
