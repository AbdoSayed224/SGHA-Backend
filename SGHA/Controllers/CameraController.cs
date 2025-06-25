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

        private async Task NotifyClients(List<MediaItemDto> data)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveImages", data);
        }

        [HttpGet("image/{id}")]
        public async Task<IActionResult> GetImage(int id)
        {
            string query = "SELECT FileName, ContentType, ImageData FROM Sys_CameraImages WHERE Id = @Id";

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

        [HttpGet("video/{id}")]
        public async Task<IActionResult> GetVideo(int id)
        {
            string query = "SELECT FileName, ContentType, ImageData FROM Sys_CameraImages WHERE Id = @Id";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                string fileName = reader.GetString(0);
                string contentType = reader.GetString(1);
                byte[] videoData = (byte[])reader["ImageData"];

                return File(videoData, contentType ?? "video/mp4", fileName);
            }

            return NotFound("Video not found.");
        }

        [HttpGet("{houseId}/media")]
        public async Task<IActionResult> GetMediaUrls(int houseId)
        {
            var mediaItems = await FetchMediaItems(houseId);
            return Ok(mediaItems);
        }

        private async Task<List<MediaItemDto>> FetchMediaItems(int houseId)
        {
            var mediaItems = new List<MediaItemDto>();

            string mediaQuery = @"
            SELECT Id, FileName, ContentType
            FROM Sys_CameraImages
            WHERE HouseID = @HouseID
            ORDER BY UploadedAt DESC";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(mediaQuery, connection);
            command.Parameters.AddWithValue("@HouseID", houseId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int id = reader.GetInt32(0);
                string fileName = reader.GetString(1);
                string contentType = reader.IsDBNull(2) ? "" : reader.GetString(2);

                string type = "unknown";

                if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    type = "image";
                else if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ||
                         contentType.Contains("mp4") || contentType.Contains("ogg") || contentType.Contains("webm") ||
                         fileName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                         fileName.EndsWith(".webm", StringComparison.OrdinalIgnoreCase) ||
                         fileName.EndsWith(".mov", StringComparison.OrdinalIgnoreCase))
                    type = "video";

                string url = type == "image"
                    ? Url.Action("GetImage", "Camera", new { id }, Request.Scheme)
                    : type == "video"
                        ? Url.Action("GetVideo", "Camera", new { id }, Request.Scheme)
                        : null;

                if (url != null)
                {
                    mediaItems.Add(new MediaItemDto
                    {
                        Id = id,
                        FileName = fileName,
                        Type = type,
                        ContentType = contentType,
                        Url = url
                    });
                }
            }

            return mediaItems;
        }

        [HttpGet("{houseId}/livestream")]
        public async Task<IActionResult> GetLatestLiveStream(int houseId)
        {
            string query = @"
        SELECT TOP 1 ID, StreamUrl, CameraName, Description, CreatedAt
        FROM Sys_LiveStreams
        WHERE HouseID = @HouseID
        ORDER BY CreatedAt DESC";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@HouseID", houseId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (!reader.HasRows)
                return NotFound("No live stream found for this house.");

            await reader.ReadAsync();

            var result = new
            {
                id = reader["ID"],
                streamUrl = reader["StreamUrl"],
                cameraName = reader["CameraName"],
                description = reader["Description"],
                createdAt = Convert.ToDateTime(reader["CreatedAt"]).ToString("o")
            };

            return Ok(result);
        }

        [HttpPost("{houseId}/upload")]
        public async Task<IActionResult> UploadImage(int houseId)
        {
            try
            {
                var formFile = Request.Form.Files.FirstOrDefault();

                if (formFile == null || formFile.Length == 0)
                    return BadRequest("No file uploaded (file is null or empty).");

                byte[] imageData;
                using (var ms = new MemoryStream())
                {
                    await formFile.CopyToAsync(ms);
                    imageData = ms.ToArray();
                }

                string query = @"
            INSERT INTO Sys_CameraImages (HouseID, FileName, ContentType, ImageData, UploadedAt)
            VALUES (@HouseID, @FileName, @ContentType, @ImageData, @UploadedAt);
            SELECT SCOPE_IDENTITY();";

                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@HouseID", houseId);
                command.Parameters.AddWithValue("@FileName", formFile.FileName);
                command.Parameters.AddWithValue("@ContentType", formFile.ContentType);
                command.Parameters.AddWithValue("@ImageData", imageData);
                command.Parameters.AddWithValue("@UploadedAt", DateTime.UtcNow);

                await connection.OpenAsync();
                var insertedId = Convert.ToInt32(await command.ExecuteScalarAsync());

                var updatedMedia = await FetchMediaItems(houseId);
                if(updatedMedia != null)
                {
                    NotifyClients(updatedMedia);
                }

                return Ok(new
                {
                    id = insertedId,
                    message = "Image uploaded successfully.",
                    fileName = formFile.FileName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Server error: " + ex.Message);
            }
        }

        [HttpPost("{houseId}/livestream")]
        public async Task<IActionResult> AddLiveStream(int houseId, [FromForm] LiveStreamUploadDto dto)
        {
            if (dto.Stream == null || dto.Stream.Length == 0)
                return BadRequest("No stream file provided.");

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.Stream.FileName)}";
            var savePath = Path.Combine("wwwroot", "uploads", "livestreams", fileName);
            var directory = Path.GetDirectoryName(savePath);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            await using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await dto.Stream.CopyToAsync(stream);
            }

            string relativePath = $"/uploads/livestreams/{fileName}";

            string query = @"
            INSERT INTO Sys_LiveStreams (HouseID, StreamUrl, CameraName, Description, CreatedAt)
            VALUES (@HouseID, @StreamUrl, @CameraName, @Description, @CreatedAt);
            SELECT SCOPE_IDENTITY();";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@HouseID", houseId);
            command.Parameters.AddWithValue("@StreamUrl", relativePath);
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
                streamUrl = relativePath,
                cameraName = dto.CameraName,
                createdAt = DateTime.UtcNow.ToString("o")
            });

            return Ok(new { id = insertedId, message = "Livestream added and stored successfully." });
        }

        public class LiveStreamUploadDto
        {
            public IFormFile Stream { get; set; }
            public string CameraName { get; set; }
            public string Description { get; set; }
        }

    }
}