namespace SGHA.DTO
{
    public class AddHouseDto
    {
        public string HouseName { get; set; }
        public string Location { get; set; }
        public float SizeSquareMeters { get; set; }
        public string Status { get; set; }
        public int OwnerID { get; set; } // Foreign Key from Sys_User
    }
}
