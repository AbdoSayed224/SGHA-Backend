namespace SGHA.DTO
{
    public class StatusHouseDto
    {
        public int HouseID { get; set; } // The ID of the house to update
        public string Status { get; set; } // The new status: "Active" or "Inactive"
        public string LastMaintenance { get; set; }
    }
}
