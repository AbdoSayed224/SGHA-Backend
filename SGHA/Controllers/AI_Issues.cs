using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;


//using NETCore.MailKit.Core;
using SGHA.DTO;
using SGHA.Hubs;
using SGHA.Interfaces;
using SGHA.Services;

namespace SGHA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AI_IssuesController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly IHubContext<AiHub> _hubContext;
        private readonly ILogger<AI_IssuesController> _logger;
        private readonly IEmailService _emailService;

        public AI_IssuesController(
                IConfiguration configuration,
                IHubContext<AiHub> hubContext,
                ILogger<AI_IssuesController> logger,
                IEmailService emailService
            )
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("Default");
            _hubContext = hubContext;
            _emailService = emailService;
        }

        private async Task NotifyClients(List<AIIssueDto> data)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveIssues", data);
        }

        private async Task<List<AIIssueDto>> GetIssuesAsync(int houseId)
        {
            var result = new List<AIIssueDto>();
            string query = "SELECT * FROM Sys_AI_Issues WHERE HouseID = @HouseID ORDER BY CreatedAt DESC";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@HouseID", houseId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new AIIssueDto
                {
                    IsAnomaly = reader.GetBoolean(reader.GetOrdinal("IsAnomaly")),
                    Action = reader["Action"].ToString(),
                    Parameter = reader["Parameter"].ToString(),
                    Range = reader["Range"].ToString(),
                    Value = Convert.ToSingle(reader["Value"]),
                    Message = reader["Message"].ToString(),
                });
            }

            return result;
        }

        // GET: api/AI_Issues/{houseId}
        [HttpGet("{houseId}")]
        public async Task<IActionResult> GetIssuesByHouseId(int houseId)
        {
            try
            {
                var issues = await GetIssuesAsync(houseId);
                return Ok(issues);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        // POST: api/AI_Issues/{houseId}
        [HttpPost("{houseId}")]
        public async Task<IActionResult> PostIssue(int houseId, [FromBody] AIIssueDto dto)
        {
            string insertQuery = @"
                INSERT INTO Sys_AI_Issues 
                (HouseID, IsAnomaly, Action, Parameter, Range, Value, Message, CreatedAt)
                VALUES 
                (@HouseID, @IsAnomaly, @Action, @Parameter, @Range, @Value, @Message, @CreatedAt)";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@HouseID", houseId);
                command.Parameters.AddWithValue("@IsAnomaly", dto.IsAnomaly);
                command.Parameters.AddWithValue("@Action", dto.Action ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Parameter", dto.Parameter ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Range", dto.Range ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Value", dto.Value);
                command.Parameters.AddWithValue("@Message", dto.Message ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                await connection.OpenAsync();
                int rows = await command.ExecuteNonQueryAsync();

                if (rows == 0)
                    return BadRequest("Failed to insert issue.");

                // Fetch all email addresses for users in the same house
                string getEmailsQuery = @"
                    SELECT A.EmailAddress
                    FROM Sys_User U
                    LEFT JOIN Sys_Account A ON U.AccountID = A.AccountID
                    WHERE U.HouseID = @HouseID";

                using var getEmailsCommand = new SqlCommand(getEmailsQuery, connection);
                getEmailsCommand.Parameters.AddWithValue("@HouseID", houseId);

                var emailList = new List<string>();
                using var reader = await getEmailsCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string email = reader["EmailAddress"]?.ToString();
                    if (!string.IsNullOrEmpty(email))
                        emailList.Add(email);
                }

                // Send emails in parallel
                if (emailList.Count > 0)
                {
                    string subject = $"🚨 AI Issue in Greenhouse (House #{houseId})";

                    string body = $@"
                    <div style='font-family: Arial, sans-serif;'>
                        <h2 style='color: red;'>AI Alert 🚨</h2>
                        <p>A new issue has been detected by the AI system in your greenhouse.</p>
                        <table border='1' cellpadding='8' cellspacing='0' style='border-collapse: collapse;'>
                            <tr><th>Parameter</th><td>{dto.Parameter}</td></tr>
                            <tr><th>Value</th><td>{dto.Value}</td></tr>
                            <tr><th>Expected Range</th><td>{dto.Range}</td></tr>
                            <tr><th>Recommended Action</th><td>{dto.Action}</td></tr>
                            <tr><th>Message</th><td>{dto.Message}</td></tr>
                            <tr><th>Detected At</th><td>{DateTime.UtcNow:f}</td></tr>
                        </table>
                        <br/>
                        <p>This is an automated message from the <strong>Smart Greenhouse System</strong>.</p>
                    </div>";

                    foreach (var email in emailList) 
                    {
                        _logger.LogInformation("Sending email to: {Email}", email);
                        try
                        {
                            await _emailService.SendEmailAsync(new EmailRequestDto
                            {
                                Subject = subject,
                                Body = body,
                                To = email
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send email to: {Email}", email);
                        }
                    }
                }

                var allIssues = await GetIssuesAsync(houseId);
                if (allIssues != null)
                {
                    await NotifyClients(allIssues);
                }

                return Ok("Issue recorded, emails sent to all users, and notification sent.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}
