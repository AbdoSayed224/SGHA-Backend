using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using SGHA.DTO;
using SGHA.Hubs;

namespace SGHA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CameraController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly IHubContext<ControlStatusHub> _hubContext;

        public CameraController(IConfiguration configuration, IHubContext<ControlStatusHub> hubContext)
        {
            _connectionString = configuration.GetConnectionString("Default");
            _hubContext = hubContext;
        }

        [HttpGet("image/{id}")]
        public async Task<IActionResult> GetImage(int id)
        {
            string query = "SELECT FileName, ContentType, ImageData FROM CameraImages WHERE Id = @Id";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                string fileName = reader.GetString(0);
                string contentType = reader.GetString(1);
                byte[] imageData = (byte[])reader["ImageData"];
                return File(imageData, contentType, fileName);
            }

            return NotFound("Image not found.");
        }


        [HttpGet("{houseId}/livestreams")]
        public async Task<IActionResult> GetLiveStreams(int houseId)
        {
            string query = @"
        SELECT Id, StreamUrl, CameraName, Description, CreatedAt
        FROM LiveStreams
        WHERE HouseID = @HouseID
        ORDER BY CreatedAt DESC";

            var result = new List<object>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@HouseID", houseId);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        Id = reader.GetInt32(0),
                        StreamUrl = reader["StreamUrl"].ToString(),
                        CameraName = reader["CameraName"].ToString(),
                        Description = reader["Description"].ToString(),
                        CreatedAt = ((DateTime)reader["CreatedAt"]).ToString("o")
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }


        [HttpPost("{houseId}/upload")]
        public async Task<IActionResult> UploadImage(int houseId, [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            byte[] imageData;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                imageData = ms.ToArray();
            }

            string query = @"
                INSERT INTO CameraImages (HouseID, FileName, ContentType, ImageData, UploadedAt)
                VALUES (@HouseID, @FileName, @ContentType, @ImageData, @UploadedAt);
                SELECT SCOPE_IDENTITY();";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@HouseID", houseId);
            command.Parameters.AddWithValue("@FileName", file.FileName);
            command.Parameters.AddWithValue("@ContentType", file.ContentType);
            command.Parameters.AddWithValue("@ImageData", imageData);
            command.Parameters.AddWithValue("@UploadedAt", DateTime.UtcNow);

            await connection.OpenAsync();
            var insertedId = Convert.ToInt32(await command.ExecuteScalarAsync());

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                type = "image",
                houseId = houseId,
                id = insertedId,
                fileName = file.FileName,
                uploadedAt = DateTime.UtcNow.ToString("o")
            });

            return Ok(new { id = insertedId, message = "Image uploaded successfully." });
        }
        
        [HttpPost("{houseId}/livestream")]
        public async Task<IActionResult> AddLiveStream(int houseId, [FromBody] LiveStreamDto dto)
        {
            string query = @"
                INSERT INTO LiveStreams (HouseID, StreamUrl, CameraName, Description, CreatedAt)
                VALUES (@HouseID, @StreamUrl, @CameraName, @Description, @CreatedAt);
                SELECT SCOPE_IDENTITY();";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@HouseID", houseId);
            command.Parameters.AddWithValue("@StreamUrl", dto.StreamUrl ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CameraName", dto.CameraName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Description", dto.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

            await connection.OpenAsync();
            var insertedId = Convert.ToInt32(await command.ExecuteScalarAsync());

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                type = "livestream",
                houseId = houseId,
                id = insertedId,
                streamUrl = dto.StreamUrl,
                cameraName = dto.CameraName,
                createdAt = DateTime.UtcNow.ToString("o")
            });

            return Ok(new { id = insertedId, message = "Livestream added successfully." });
        }

    }
}