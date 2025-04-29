namespace SGHA.DTO
{
    public class CreateUserDto
    {
            public string? EmailAddress { get; set; }
            public string? AccountPassword { get; set; }
            public string?  HouseID { get; set; }
            public string? UserName { get; set; }
            public string? PhoneNumber { get; set; }

            public int RoleId { get; set; }  
    }
}
