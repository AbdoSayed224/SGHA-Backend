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

        [HttpGet("ShowAll")]
        public async Task<ActionResult<IEnumerable<UserDataDto>>> GetAllUsers()
        {
            var users = new List<UserDataDto>();

            // SQL query to get all users along with account email, role name, and house name
            string query = @"
        SELECT u.UserID, u.UserName, u.PhoneNumber, a.EmailAddress AS AccountEmail, r.RoleName, 
               u.CreatedAt, u.UpdatedAt, u.IsActive, h.HouseName
        FROM Sys_User u
        INNER JOIN Sys_Account a ON u.AccountID = a.AccountID
        INNER JOIN Sys_Role r ON u.RoleID = r.RoleID
        LEFT JOIN Sys_House h ON u.UserID = h.OwnerID"; // Assuming UserID is linked to OwnerID in Sys_House

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var user = new UserDataDto
                                {
                                    UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                                    AccountEmail = reader.GetString(reader.GetOrdinal("AccountEmail")),
                                    RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
                                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                    HouseName = reader.IsDBNull(reader.GetOrdinal("HouseName")) ? null : reader.GetString(reader.GetOrdinal("HouseName")) // Getting HouseName
                                };

                                users.Add(user);
                            }
                        }
                    }
                }

                return Ok(users);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PATCH: Set user status to Active by UserID
        [HttpPatch("set-status-active/{userID}")]
        public async Task<IActionResult> SetUserStatusActive(int userID)
        {
            if (userID <= 0)
            {
                return BadRequest("Invalid UserID.");
            }

            string query = @"
                UPDATE Sys_User
                SET 
                    Status = 'Active',
                    UpdatedAt = @UpdatedAt
                WHERE UserID = @UserID";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Add parameters
                        command.Parameters.AddWithValue("@UserID", userID);
                        command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return Ok($"User with ID {userID} status updated to Active.");
                        }

                        return NotFound($"User with ID {userID} not found.");
                    }
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PATCH: Set user status to Inactive by UserID
        [HttpPatch("set-status-inactive/{userID}")]
        public async Task<IActionResult> SetUserStatusInactive(int userID)
        {
            if (userID <= 0)
            {
                return BadRequest("Invalid UserID.");
            }

            string query = @"
                UPDATE Sys_User
                SET 
                    Status = 'Inactive',
                    UpdatedAt = @UpdatedAt
                WHERE UserID = @UserID";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Add parameters
                        command.Parameters.AddWithValue("@UserID", userID);
                        command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return Ok($"User with ID {userID} status updated to Inactive.");
                        }

                        return NotFound($"User with ID {userID} not found.");
                    }
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Create User//
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            if (string.IsNullOrEmpty(createUserDto.EmailAddress) || string.IsNullOrEmpty(createUserDto.AccountPassword))
                return BadRequest("Email address and password are required.");

            if (string.IsNullOrEmpty(createUserDto.UserName) || string.IsNullOrEmpty(createUserDto.PhoneNumber))
                return BadRequest("User name and phone number are required.");

            try
            {
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
            catch (SqlException ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}