namespace SGHA.DTO
{
    public class HouseDto
    {
        public int HouseID { get; set; }
        public string HouseName { get; set; }
        public string Location { get; set; }
        public float SizeSquareMeters { get; set; }
        public string Status { get; set; }
        public int OwnerID { get; set; }
        public DateTime LastMaintenance { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
