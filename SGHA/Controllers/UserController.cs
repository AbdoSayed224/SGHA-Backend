using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SGHA.DTO;

namespace SGHA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly string _connectionString;

        public UserController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default");
        }



        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            if (string.IsNullOrEmpty(createUserDto.EmailAddress) || string.IsNullOrEmpty(createUserDto.AccountPassword))
                return BadRequest("Email address and password are required.");

            if (string.IsNullOrEmpty(createUserDto.UserName) || string.IsNullOrEmpty(createUserDto.PhoneNumber))
                return BadRequest("User name and phone number are required.");

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    // Insert into Sys_Account
                    var insertAccountQuery = @"
                INSERT INTO Sys_Account (EmailAddress, AccountPassword, CreatedAt, UpdatedAt)
                VALUES (@EmailAddress, @AccountPassword, @CreatedAt, @UpdatedAt);
                SELECT SCOPE_IDENTITY();";

                    var accountCommand = new SqlCommand(insertAccountQuery, connection, transaction);
                    accountCommand.Parameters.AddWithValue("@EmailAddress", createUserDto.EmailAddress);
                    accountCommand.Parameters.AddWithValue("@AccountPassword", createUserDto.AccountPassword);
                    accountCommand.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    accountCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);

                    int accountId = Convert.ToInt32(await accountCommand.ExecuteScalarAsync());

                    // Insert into Sys_User
                    var insertUserQuery = @"
                INSERT INTO Sys_User (UserName, PhoneNumber, AccountID, RoleID, CreatedAt, UpdatedAt, IsActive)
                VALUES (@UserName, @PhoneNumber, @AccountID, @RoleID, @CreatedAt, @UpdatedAt, @IsActive);";

                    var userCommand = new SqlCommand(insertUserQuery, connection, transaction);
                    userCommand.Parameters.AddWithValue("@UserName", createUserDto.UserName);
                    userCommand.Parameters.AddWithValue("@PhoneNumber", createUserDto.PhoneNumber);
                    userCommand.Parameters.AddWithValue("@AccountID", accountId);
                    userCommand.Parameters.AddWithValue("@RoleID", createUserDto.RoleId);
                    userCommand.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    userCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                    userCommand.Parameters.AddWithValue("@IsActive", true);

                    await userCommand.ExecuteNonQueryAsync();

                    // Commit transaction
                    await transaction.CommitAsync();

                    return Ok(new { Message = "User created successfully", AccountID = accountId });
                }
            }
        }


    }
}