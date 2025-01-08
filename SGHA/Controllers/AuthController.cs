using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SGHA.DTO;

namespace SGHA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly string _connectionString;

        public AuthController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (loginDto == null || string.IsNullOrEmpty(loginDto.EmailAddress) || string.IsNullOrEmpty(loginDto.AccountPassword))
                return BadRequest("Email address and password are required.");

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
            SELECT u.UserID, u.UserName, u.PhoneNumber, r.RoleName, u.IsActive
            FROM Sys_User u
            INNER JOIN Sys_Account a ON u.AccountID = a.AccountID
            INNER JOIN Sys_Role r ON u.RoleID = r.RoleID
            WHERE a.EmailAddress = @EmailAddress AND a.AccountPassword = @AccountPassword and u.IsActive = 'true'";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@EmailAddress", loginDto.EmailAddress);
                    command.Parameters.AddWithValue("@AccountPassword", loginDto.AccountPassword);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // Create a response object
                            var response = new
                            {
                                UserID = reader["UserID"],
                                UserName = reader["UserName"],
                                RoleName = reader["RoleName"] 
                            };

                            return Ok(response); // Return the user details
                        }
                        else
                        {
                            return Unauthorized("Invalid email or password.");
                        }
                    }
                }
            }
        }

    }
}
