namespace SGHA.DTO
{
    public class UpdateUserDto
    {
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public int RoleID { get; set; }
        public bool IsActive { get; set; }
        public int? HouseID { get; set; }
        public string Status { get; set; }
    }
}
