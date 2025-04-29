namespace SGHA.DTO
{
    public class UpdateHouseDto
    {
        public string HouseName { get; set; }
        public string Location { get; set; }
        public double SizeSquareMeters { get; set; }
        public string Status { get; set; }
        public int OwnerID { get; set; }
        public DateTime? LastMaintenance { get; set; }
    }
}
