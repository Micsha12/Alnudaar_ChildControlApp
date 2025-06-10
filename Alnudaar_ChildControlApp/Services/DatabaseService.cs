using Microsoft.Data.Sqlite;
using Alnudaar_ChildControlApp.Models;

namespace Alnudaar_ChildControlApp
{
    public class DatabaseService
    {
        private const string DbFilePath = "child_data.db";
        private readonly ILogger<DatabaseService> _logger;
        private readonly string _connectionString = $"Data Source={DbFilePath}";
        public DatabaseService(ILogger<DatabaseService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            if (!File.Exists(DbFilePath))
            {
                using var connection = new SqliteConnection($"Data Source={DbFilePath}");
                connection.Open();

                string createTablesQuery = @"
                    CREATE TABLE Users (
                    UserID INTEGER PRIMARY KEY,
                    UserName TEXT NOT NULL,
                    Email TEXT
                    );

                    CREATE TABLE Devices (
                        DeviceID INTEGER PRIMARY KEY,
                        Name TEXT NOT NULL,
                        UserID INTEGER NOT NULL,
                        FOREIGN KEY (UserID) REFERENCES Users(UserID)
                    );

                    CREATE TABLE Device (
                        DeviceID INTEGER PRIMARY KEY,
                        Name TEXT,
                        UserID INTEGER
                    );

                    CREATE TABLE ScreenTimeSchedules (
                    ScreenTimeScheduleID INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserID INTEGER NOT NULL,
                    DeviceID INTEGER NULL,
                    DayOfWeek TEXT NULL,
                    StartTime TEXT NULL,
                    EndTime TEXT NULL,
                    FOREIGN KEY (UserID) REFERENCES Users(UserID),
                    FOREIGN KEY (DeviceID) REFERENCES Devices(DeviceID)
                    );

                    CREATE TABLE Geofencing (
                        GeofencingID INTEGER PRIMARY KEY,
                        UserID INTEGER,
                        SafeZoneName TEXT,
                        Latitude REAL,
                        Longitude REAL,
                        Radius INTEGER
                    );

                    CREATE TABLE BlockRule (
                        BlockRuleID INTEGER PRIMARY KEY,
                        UserID INTEGER,
                        Type TEXT,
                        Value TEXT,
                        TimeRange TEXT,
                        DeviceID INTEGER
                    );

                    CREATE TABLE AppUsageReport (
                        AppUsageReportID INTEGER PRIMARY KEY,
                        UserID INTEGER,
                        DeviceID INTEGER,
                        Timestamp DATETIME,
                        AppName TEXT,
                        UsageDuration INTEGER
                    );

                    CREATE TABLE ActivityLog (
                        ActivityLogID INTEGER PRIMARY KEY,
                        UserID INTEGER,
                        DeviceID INTEGER,
                        Timestamp DATETIME,
                        Activity TEXT
                    );

                    CREATE TABLE Alert (
                        AlertID INTEGER PRIMARY KEY,
                        Message TEXT,
                        CreatedAt DATETIME
                    );
                ";

                using var command = new SqliteCommand(createTablesQuery, connection);
                command.ExecuteNonQuery();
            }
        }

        public void SaveDeviceInfo(Device device)
        {
            using var connection = new SqliteConnection($"Data Source={DbFilePath}");
            connection.Open();

            string insertQuery = @"
                INSERT OR REPLACE INTO Device (DeviceID, Name, UserID)
                VALUES (@deviceID, @name, @userID);
            ";

            using var command = new SqliteCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@deviceID", device.DeviceID);
            command.Parameters.AddWithValue("@name", device.Name ?? "Unknown"); // Handle null Name
            command.Parameters.AddWithValue("@userID", device.UserID);

            command.ExecuteNonQuery();

            _logger.LogInformation("Device saved: DeviceID={DeviceID}, Name={Name}, UserID={UserID}", device.DeviceID, device.Name, device.UserID);
        }

        public bool ScreenTimeScheduleExists(int screenTimeScheduleID)
        {
            using var connection = new SqliteConnection($"Data Source={DbFilePath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM ScreenTimeSchedules WHERE ScreenTimeScheduleID = $screenTimeScheduleID";
            command.Parameters.AddWithValue("$screenTimeScheduleID", screenTimeScheduleID);

            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        public void SaveScreenTimeSchedules(IEnumerable<ScreenTimeSchedule> schedules)
        {
            using var connection = new SqliteConnection($"Data Source={DbFilePath}");
            connection.Open();

            foreach (var schedule in schedules)
            {
                // Check if the schedule already exists
                if (ScreenTimeScheduleExists(schedule.ScreenTimeScheduleID))
                {
                    // Update the schedule if it exists
                    var updateCommand = connection.CreateCommand();
                    updateCommand.CommandText = @"
                        UPDATE ScreenTimeSchedules
                        SET UserID = $userID,
                            DeviceID = $deviceID,
                            DayOfWeek = $dayOfWeek,
                            StartTime = $startTime,
                            EndTime = $endTime
                        WHERE ScreenTimeScheduleID = $screenTimeScheduleID";
                    updateCommand.Parameters.AddWithValue("$screenTimeScheduleID", schedule.ScreenTimeScheduleID);
                    updateCommand.Parameters.AddWithValue("$userID", schedule.UserID);
                    updateCommand.Parameters.AddWithValue("$deviceID", schedule.DeviceID);
                    updateCommand.Parameters.AddWithValue("$dayOfWeek", schedule.DayOfWeek);
                    updateCommand.Parameters.AddWithValue("$startTime", schedule.StartTime);
                    updateCommand.Parameters.AddWithValue("$endTime", schedule.EndTime);

                    updateCommand.ExecuteNonQuery();
                    _logger.LogInformation("Updated ScreenTimeSchedule: ScreenTimeScheduleID={ScreenTimeScheduleID}", schedule.ScreenTimeScheduleID);
                }
                else
                {
                    // Insert the schedule if it doesn't exist
                    var insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = @"
                        INSERT INTO ScreenTimeSchedules (ScreenTimeScheduleID, UserID, DeviceID, DayOfWeek, StartTime, EndTime)
                        VALUES ($screenTimeScheduleID, $userID, $deviceID, $dayOfWeek, $startTime, $endTime)";
                    insertCommand.Parameters.AddWithValue("$screenTimeScheduleID", schedule.ScreenTimeScheduleID);
                    insertCommand.Parameters.AddWithValue("$userID", schedule.UserID);
                    insertCommand.Parameters.AddWithValue("$deviceID", schedule.DeviceID);
                    insertCommand.Parameters.AddWithValue("$dayOfWeek", schedule.DayOfWeek);
                    insertCommand.Parameters.AddWithValue("$startTime", schedule.StartTime);
                    insertCommand.Parameters.AddWithValue("$endTime", schedule.EndTime);

                    insertCommand.ExecuteNonQuery();
                    _logger.LogInformation("Inserted ScreenTimeSchedule: ScreenTimeScheduleID={ScreenTimeScheduleID}", schedule.ScreenTimeScheduleID);
                }
            }
        }
        public void SaveGeofencingData(IEnumerable<Geofencing> geofencingData)
        {
            using var connection = new SqliteConnection($"Data Source={DbFilePath}");
            connection.Open();

            string insertQuery = @"
                INSERT INTO Geofencing (GeofencingID, UserID, SafeZoneName, Latitude, Longitude, Radius)
                VALUES (@geofencingID, @userID, @safeZoneName, @latitude, @longitude, @radius);
            ";

            foreach (var geofence in geofencingData)
            {
                using var command = new SqliteCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@geofencingID", geofence.GeofencingID);
                command.Parameters.AddWithValue("@userID", geofence.UserID);
                command.Parameters.AddWithValue("@safeZoneName", geofence.SafeZoneName);
                command.Parameters.AddWithValue("@latitude", geofence.Latitude);
                command.Parameters.AddWithValue("@longitude", geofence.Longitude);
                command.Parameters.AddWithValue("@radius", geofence.Radius);
                command.ExecuteNonQuery();
            }
        }

        public bool BlockRuleExists(int blockRuleID)
        {
            using var connection = new SqliteConnection($"Data Source={DbFilePath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM BlockRule WHERE BlockRuleID = $blockRuleID";
            command.Parameters.AddWithValue("$blockRuleID", blockRuleID);

            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        public void SaveBlockRules(IEnumerable<BlockRule> blockRules)
        {
            using var connection = new SqliteConnection($"Data Source={DbFilePath}");
            connection.Open();

            foreach (var blockRule in blockRules)
            {
                if (BlockRuleExists(blockRule.BlockRuleID))
                {
                    // Update the block rule if it exists
                    var updateCommand = connection.CreateCommand();
                    updateCommand.CommandText = @"
                        UPDATE BlockRule
                        SET UserID = $userID,
                            Type = $type,
                            Value = $value,
                            TimeRange = $timeRange,
                            DeviceID = $deviceID
                        WHERE BlockRuleID = $blockRuleID";
                    updateCommand.Parameters.AddWithValue("$blockRuleID", blockRule.BlockRuleID);
                    updateCommand.Parameters.AddWithValue("$userID", blockRule.UserID);
                    updateCommand.Parameters.AddWithValue("$type", blockRule.Type);
                    updateCommand.Parameters.AddWithValue("$value", blockRule.Value);
                    updateCommand.Parameters.AddWithValue("$timeRange", blockRule.TimeRange);
                    updateCommand.Parameters.AddWithValue("$deviceID", blockRule.DeviceID);

                    updateCommand.ExecuteNonQuery();
                    _logger.LogInformation("Updated BlockRule: BlockRuleID={BlockRuleID}", blockRule.BlockRuleID);
                }
                else
                {
                    // Insert the block rule if it doesn't exist
                    var insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = @"
                        INSERT INTO BlockRule (BlockRuleID, UserID, Type, Value, TimeRange, DeviceID)
                        VALUES ($blockRuleID, $userID, $type, $value, $timeRange, $deviceID)";
                    insertCommand.Parameters.AddWithValue("$blockRuleID", blockRule.BlockRuleID);
                    insertCommand.Parameters.AddWithValue("$userID", blockRule.UserID);
                    insertCommand.Parameters.AddWithValue("$type", blockRule.Type);
                    insertCommand.Parameters.AddWithValue("$value", blockRule.Value);
                    insertCommand.Parameters.AddWithValue("$timeRange", blockRule.TimeRange);
                    insertCommand.Parameters.AddWithValue("$deviceID", blockRule.DeviceID);

                    insertCommand.ExecuteNonQuery();
                    _logger.LogInformation("Inserted BlockRule: BlockRuleID={BlockRuleID}", blockRule.BlockRuleID);
                }
            }
        }

        public bool UserExists(int userId)
        {
            using var connection = new SqliteConnection($"Data Source={DbFilePath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM Users WHERE UserID = $userID";
            command.Parameters.AddWithValue("$userID", userId);

            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        public void SaveUser(User user)
        {
            using var connection = new SqliteConnection($"Data Source={DbFilePath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Users (UserID, UserName, Email)
                VALUES ($userID, $userName, $email)
                ON CONFLICT(UserID) DO UPDATE SET
                    UserName = excluded.UserName,
                    Email = excluded.Email;";
            command.Parameters.AddWithValue("$userID", user.UserID);
            command.Parameters.AddWithValue("$userName", user.UserName);
            command.Parameters.AddWithValue("$email", user.Email);

            command.ExecuteNonQuery();
        }

        public void SaveDevicesInfo(Device device)
        {
            using var connection = new SqliteConnection($"Data Source={DbFilePath}");
            connection.Open();

            string insertQuery = @"
                INSERT OR REPLACE INTO Devices (DeviceID, Name, UserID)
                VALUES (@deviceID, @name, @userID);
            ";

            using var command = new SqliteCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@deviceID", device.DeviceID);
            command.Parameters.AddWithValue("@name", device.Name ?? "Unknown"); // Handle null Name
            command.Parameters.AddWithValue("@userID", device.UserID);

            command.ExecuteNonQuery();

            _logger.LogInformation("Device saved to Devices table: DeviceID={DeviceID}, Name={Name}, UserID={UserID}", device.DeviceID, device.Name, device.UserID);
        }

        public IEnumerable<ScreenTimeSchedule> GetScreenTimeSchedules()
        {
            using var connection = new SqliteConnection($"Data Source={DbFilePath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM ScreenTimeSchedules";

            using var reader = command.ExecuteReader();
            var schedules = new List<ScreenTimeSchedule>();

            while (reader.Read())
            {
                schedules.Add(new ScreenTimeSchedule
                {
                    ScreenTimeScheduleID = reader.GetInt32(0),
                    UserID = reader.GetInt32(1),
                    DeviceID = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    DayOfWeek = reader.GetString(3),
                    StartTime = reader.GetString(4),
                    EndTime = reader.GetString(5)
                });
            }

            return schedules;
        }

        public IEnumerable<BlockRule> GetBlockRules()
        {
            using var connection = new SqliteConnection($"Data Source={DbFilePath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM BlockRule";

            using var reader = command.ExecuteReader();
            var blockRules = new List<BlockRule>();

            while (reader.Read())
            {
                blockRules.Add(new BlockRule
                {
                    BlockRuleID = reader.GetInt32(0),
                    UserID = reader.GetInt32(1),
                    Type = reader.GetString(2),
                    Value = reader.GetString(3),
                    TimeRange = reader.GetString(4),
                    DeviceID = reader.GetInt32(5)
                });
            }

            return blockRules;
        }

        public void UpsertAppUsageReport(int userId, int deviceId, DateTime timestamp, string appName, int usageDuration)
        {
            var dateOnly = timestamp.Date;
            using var connection = new SqliteConnection($"Data Source={DbFilePath}");
            connection.Open();

            using (var updateCmd = connection.CreateCommand())
            {
                updateCmd.CommandText = @"
                    UPDATE AppUsageReport
                    SET UsageDuration = UsageDuration + @UsageDuration
                    WHERE UserID = @UserID AND DeviceID = @DeviceID AND AppName = @AppName AND date(Timestamp) = @DateOnly";
                updateCmd.Parameters.AddWithValue("@UsageDuration", usageDuration);
                updateCmd.Parameters.AddWithValue("@UserID", userId);
                updateCmd.Parameters.AddWithValue("@DeviceID", deviceId);
                updateCmd.Parameters.AddWithValue("@AppName", appName);
                updateCmd.Parameters.AddWithValue("@DateOnly", dateOnly);

                int rows = updateCmd.ExecuteNonQuery();

                if (rows == 0)
                {
                    using (var insertCmd = connection.CreateCommand())
                    {
                        insertCmd.CommandText = @"
                            INSERT INTO AppUsageReport (UserID, DeviceID, Timestamp, AppName, UsageDuration)
                            VALUES (@UserID, @DeviceID, @Timestamp, @AppName, @UsageDuration)
                            ON CONFLICT(UserID, DeviceID, AppName, Timestamp)
                            DO UPDATE SET UsageDuration = UsageDuration + excluded.UsageDuration;";
                        insertCmd.Parameters.AddWithValue("@UserID", userId);
                        insertCmd.Parameters.AddWithValue("@DeviceID", deviceId);
                        insertCmd.Parameters.AddWithValue("@Timestamp", dateOnly);
                        insertCmd.Parameters.AddWithValue("@AppName", appName);
                        insertCmd.Parameters.AddWithValue("@UsageDuration", usageDuration);
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public Device? GetDeviceById(int deviceId)
        {
            using var connection = new SqliteConnection($"Data Source={DbFilePath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT DeviceID, Name, UserID FROM Devices WHERE DeviceID = @DeviceID";
            command.Parameters.AddWithValue("@DeviceID", deviceId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Device
                {
                    DeviceID = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    UserID = reader.GetInt32(2)
                };
            }
            return null;
        }

        public List<AppUsageReport> GetAppUsageReportsForDate(DateTime date)
        {
            var reports = new List<AppUsageReport>();
            using var connection = new SqliteConnection($"Data Source={DbFilePath}");
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT UserID, DeviceID, Timestamp, AppName, UsageDuration
                FROM AppUsageReport
                WHERE Timestamp >= @StartOfDay AND Timestamp < @StartOfNextDay";
            cmd.Parameters.AddWithValue("@StartOfDay", date.Date);
            cmd.Parameters.AddWithValue("@StartOfNextDay", date.Date.AddDays(1));

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                reports.Add(new AppUsageReport
                {
                    UserID = reader.GetInt32(0),
                    DeviceID = reader.GetInt32(1),
                    Timestamp = reader.GetDateTime(2),
                    AppName = reader.GetString(3),
                    UsageDuration = TimeSpan.FromSeconds(reader.GetInt32(4))
                });
            }
            return reports;
        }
        // Add similar methods for other models like Geofencing, BlockRule, etc.
    }
}