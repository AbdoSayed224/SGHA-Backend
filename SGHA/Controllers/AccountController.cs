using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SGHA.DTO;

namespace SGHA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly string _connectionString;

        public AccountController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default");
        }
        // Patch Sys_Account
        [HttpPatch("patch-account/{accountId}")]
        public async Task<IActionResult> PatchAccount(int accountId, [FromBody] UpdateAccountDto accountDto)
        {
            if (accountDto == null) return BadRequest("Invalid data.");

            var query = @"
                UPDATE Sys_Account
                SET 
                    EmailAddress = CASE WHEN @EmailAddress IS NOT NULL THEN @EmailAddress ELSE EmailAddress END,
                    AccountPassword = CASE WHEN @AccountPassword IS NOT NULL THEN @AccountPassword ELSE AccountPassword END,
                    UpdatedAt = @UpdatedAt
                WHERE AccountID = @AccountID";

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AccountID", accountId);
                    command.Parameters.AddWithValue("@EmailAddress", (object?)accountDto.EmailAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("@AccountPassword", (object?)accountDto.AccountPassword ?? DBNull.Value);
                    command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

                    await connection.OpenAsync();
                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    return rowsAffected > 0 ? Ok("Account updated successfully.") : NotFound("Account not found.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        // Delete Sys_Account
        [HttpDelete("delete-account/{accountId}")]
        public async Task<IActionResult> DeleteAccount(int accountId)
        {
            var query = "DELETE FROM Sys_Account WHERE AccountID = @AccountID";

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AccountID", accountId);

                    await connection.OpenAsync();
                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    return rowsAffected > 0 ? Ok("Account deleted successfully.") : NotFound("Account not found.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}
