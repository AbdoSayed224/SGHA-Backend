using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using SGHA.DTO;
using SGHA.Hubs;

namespace SGHA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SensorController : ControllerBase
    {
        private readonly IHubContext<ControlStatusHub> _hubContext;
        private readonly string _connectionString;

        public SensorController(IConfiguration configuration, IHubContext<ControlStatusHub> hubContext)
        {
            _hubContext = hubContext;
            _connectionString = configuration.GetConnectionString("Default");
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
        private async Task NotifyClients(KeySensorReadingsDto updatedControl)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveSensorsUpdate", updatedControl);
        }

        private async Task<KeySensorReadingsDto> GetKeySensorReadingsByHouseId(int houseId)
        {
            double? temperature = null;
            double? humidity = null;
            double? moisture = null;
            double? light = null;
            double? airquality = null;

            string query = @"
        SELECT SensorType, SensorValue
        FROM Sys_Sensor
        WHERE HouseID = @HouseID
          AND SensorType IN ('Temperature', 'Humidity', 'Soil Moisture', 'Light', 'AirQuality')";

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@HouseID", houseId);
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string sensorType = reader.GetString(reader.GetOrdinal("SensorType"));
                            double value = reader.GetDouble(reader.GetOrdinal("SensorValue"));

                            switch (sensorType)
                            {
                                case "Temperature":
                                    temperature = value;
                                    break;
                                case "Humidity":
                                    humidity = value;
                                    break;
                                case "Soil Moisture":
                                    moisture = value;
                                    break;
                                case "Light":
                                    light = value;
                                    break;
                                case "AirQuality":
                                    airquality = value;
                                    break;
                            }
                        }
                    }
                }

                var result = new KeySensorReadingsDto
                {
                    Temperature = temperature,
                    Humidity = humidity,
                    Moisture = moisture,
                    Light = light,
                    AirQuality = airquality
                };

                return result;
            }
            catch (SqlException ex)
            {
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        //get all sensors in house by house id
        [HttpGet("house/{houseId}")]
            public async Task<IActionResult> GetSensorsByHouseId(int houseId)
            {
                var sensors = new List<SensorDto>();

                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();
                    var cmd = new SqlCommand("SELECT * FROM sys_sensor WHERE HouseID = @houseId", conn);
                    cmd.Parameters.AddWithValue("@houseId", houseId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var sensor = new SensorDto
                            {
                                SensorID = reader.GetInt32(0),
                                HouseID = reader.GetInt32(1),
                                SensorType = reader.GetString(2),
                                SensorName = reader.IsDBNull(3) ? null : reader.GetString(3),
                                SensorLocation = reader.IsDBNull(4) ? null : reader.GetString(4),
                                SensorValue = reader.IsDBNull(5) ? (double?)null : reader.GetDouble(5),
                                Unit = reader.IsDBNull(6) ? null : reader.GetString(6),
                                CreatedAt = reader.GetDateTime(7),
                                UpdatedAt = reader.GetDateTime(8)
                            };
                            sensors.Add(sensor);
                        }
                    }
                }

                if (sensors.Count == 0) return NotFound();

                return Ok(sensors);
            }

            [HttpGet("key-sensors/by-house-object/{houseId}")]
            public async Task<IActionResult> GetKeySensorsByHouseAsObject(int houseId)
            {
                double? temperature = null;
                double? humidity = null;
                double? moisture = null;
                double? light = null;
                double? airquality = null;

                string query = @"
        SELECT SensorType, SensorValue
        FROM Sys_Sensor
        WHERE HouseID = @HouseID
          AND SensorType IN ('Temperature', 'Humidity', 'Soil Moisture', 'Light', 'AirQuality')";

                try
                {
                    using (var connection = new SqlConnection(_connectionString))
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@HouseID", houseId);
                        await connection.OpenAsync();

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string sensorType = reader.GetString(reader.GetOrdinal("SensorType"));
                                double value = reader.GetDouble(reader.GetOrdinal("SensorValue"));

                                switch (sensorType)
                                {
                                    case "Temperature":
                                        temperature = value;
                                        break;
                                    case "Humidity":
                                        humidity = value;
                                        break;
                                    case "Soil Moisture":
                                        moisture = value;
                                        break;
                                    case "Light":
                                        light = value;
                                        break;
                                    case "AirQuality":
                                        airquality = value;
                                        break;
                                }
                            }
                        }
                    }

                    var result = new KeySensorReadingsDto
                    {
                        Temperature = temperature,
                        Humidity = humidity,
                        Moisture = moisture,
                        Light = light,
                        AirQuality = airquality,
                    };

                    return Ok(result);
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

            [HttpGet("{id}")]
            public async Task<IActionResult> GetSensor(int id)
            {
                SensorDto sensor = null;

                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();
                    var cmd = new SqlCommand("SELECT * FROM sys_sensor WHERE SensorID = @id", conn);
                    cmd.Parameters.AddWithValue("@id", id);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            sensor = new SensorDto
                            {
                                SensorID = reader.GetInt32(0),
                                HouseID = reader.GetInt32(1),
                                SensorType = reader.GetString(2),
                                SensorName = reader.IsDBNull(3) ? null : reader.GetString(3),
                                SensorLocation = reader.IsDBNull(4) ? null : reader.GetString(4),
                                SensorValue = reader.IsDBNull(5) ? (double?)null : reader.GetDouble(5),
                                Unit = reader.IsDBNull(6) ? null : reader.GetString(6),
                                CreatedAt = reader.GetDateTime(7),
                                UpdatedAt = reader.GetDateTime(8)
                            };
                        }
                    }
                }

                if (sensor == null) return NotFound();

                return Ok(sensor);
            }

            [HttpPatch("{id}")]
            public async Task<IActionResult> UpdateSensor(int id, [FromBody] SensorDto sensorDto)
            {
                if (sensorDto == null) return BadRequest();

                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();
                    var cmd = new SqlCommand(
                        "UPDATE sys_sensor SET HouseID = @HouseID, SensorType = @SensorType, SensorName = @SensorName, " +
                        "SensorLocation = @SensorLocation, SensorValue = @SensorValue, Unit = @Unit, UpdatedAt = @UpdatedAt " +
                        "WHERE SensorID = @id", conn);

                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@HouseID", sensorDto.HouseID);
                    cmd.Parameters.AddWithValue("@SensorType", sensorDto.SensorType);
                    cmd.Parameters.AddWithValue("@SensorName", (object?)sensorDto.SensorName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SensorLocation", (object?)sensorDto.SensorLocation ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SensorValue", (object?)sensorDto.SensorValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Unit", (object?)sensorDto.Unit ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

                    var rowsAffected = await cmd.ExecuteNonQueryAsync();
                    if (rowsAffected == 0) return NotFound();

                    return NoContent();
                }
            }

        private class patchSensorValue
        {
            public double? SensorValue { get; set; }
        }

        [HttpPatch("{id}/value/{sensorValue}")]
        public async Task<IActionResult> UpdateSensorValue(int id, double sensorValue)
        {
            if (sensorValue == null) return BadRequest();

            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(
                    "UPDATE sys_sensor SET SensorValue = @SensorValue, UpdatedAt = @UpdatedAt WHERE SensorID = @id", conn);

                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@SensorValue", (object?)sensorValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    return NotFound();
                }
                else
                {
                    var updatedReadings = await GetKeySensorReadingsByHouseId(1);
                    if (updatedReadings != null)
                    {
                        await NotifyClients(updatedReadings);
                    }

                    return Ok("Value Patched Success.");
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddSensor([FromBody] SensorDto sensorDto)
        {
            if (sensorDto == null) return BadRequest();

            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(
                    "INSERT INTO sys_sensor (HouseID, SensorType, SensorName, SensorLocation, SensorValue, Unit, CreatedAt, UpdatedAt) " +
                    "VALUES (@HouseID, @SensorType, @SensorName, @SensorLocation, @SensorValue, @Unit, @CreatedAt, @UpdatedAt); " +
                    "SELECT SCOPE_IDENTITY();", conn);

                cmd.Parameters.AddWithValue("@HouseID", sensorDto.HouseID);
                cmd.Parameters.AddWithValue("@SensorType", sensorDto.SensorType);
                cmd.Parameters.AddWithValue("@SensorName", (object?)sensorDto.SensorName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SensorLocation", (object?)sensorDto.SensorLocation ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SensorValue", (object?)sensorDto.SensorValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Unit", (object?)sensorDto.Unit ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedAt", sensorDto.CreatedAt);
                cmd.Parameters.AddWithValue("@UpdatedAt", sensorDto.UpdatedAt);

                var newId = await cmd.ExecuteScalarAsync();
                return Ok(new { SensorID = Convert.ToInt32(newId) });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSensor(int id)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("DELETE FROM sys_sensor WHERE SensorID = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0) return NotFound();

                return NoContent();
            }
        }
    }
}