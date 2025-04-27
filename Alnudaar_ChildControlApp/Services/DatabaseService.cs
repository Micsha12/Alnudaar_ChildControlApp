using Microsoft.Data.Sqlite;
using Alnudaar_ChildControlApp.Models;

namespace Alnudaar_ChildControlApp
{
    public class DatabaseService
    {
        private const string DbFilePath = "child_data.db";

        public DatabaseService()
        {
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            if (!File.Exists(DbFilePath))
            {
                using var connection = new SqliteConnection($"Data Source={DbFilePath}");
                connection.Open();

                string createTablesQuery = @"
                    CREATE TABLE Device (
                        DeviceID INTEGER PRIMARY KEY,
                        Name TEXT,
                        UserID INTEGER
                    );

                    CREATE TABLE ScreenTimeSchedule (
                        ScreenTimeScheduleID INTEGER PRIMARY KEY,
                        UserID INTEGER,
                        DeviceID INTEGER,
                        StartTime TEXT,
                        EndTime TEXT,
                        DayOfWeek TEXT
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
            command.Parameters.AddWithValue("@name", device.Name); // Handle null Name
            command.Parameters.AddWithValue("@userID", device.UserID);

            command.ExecuteNonQuery();
        }

        public void SaveScreenTimeSchedules(IEnumerable<ScreenTimeSchedule> schedules)
        {
            using var connection = new SqliteConnection($"Data Source={DbFilePath}");
            connection.Open();

            string insertQuery = @"
                INSERT INTO ScreenTimeSchedule (ScreenTimeScheduleID, UserID, DeviceID, StartTime, EndTime, DayOfWeek)
                VALUES (@scheduleID, @userID, @deviceID, @startTime, @endTime, @dayOfWeek);
            ";

            foreach (var schedule in schedules)
            {
                using var command = new SqliteCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@scheduleID", schedule.ScreenTimeScheduleID);
                command.Parameters.AddWithValue("@userID", schedule.UserID);
                command.Parameters.AddWithValue("@deviceID", schedule.DeviceID);
                command.Parameters.AddWithValue("@startTime", schedule.StartTime);
                command.Parameters.AddWithValue("@endTime", schedule.EndTime);
                command.Parameters.AddWithValue("@dayOfWeek", schedule.DayOfWeek);
                command.ExecuteNonQuery();
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

        public void SaveBlockRules(IEnumerable<BlockRule> blockRules)
        {
            using var connection = new SqliteConnection($"Data Source={DbFilePath}");
            connection.Open();

            string insertQuery = @"
                INSERT INTO BlockRule (BlockRuleID, UserID, Type, Value, TimeRange, DeviceID)
                VALUES (@blockRuleID, @userID, @type, @value, @timeRange, @deviceID);
            ";

            foreach (var blockRule in blockRules)
            {
                using var command = new SqliteCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@blockRuleID", blockRule.BlockRuleID);
                command.Parameters.AddWithValue("@userID", blockRule.UserID);
                command.Parameters.AddWithValue("@type", blockRule.Type);
                command.Parameters.AddWithValue("@value", blockRule.Value);
                command.Parameters.AddWithValue("@timeRange", blockRule.TimeRange);
                command.Parameters.AddWithValue("@deviceID", blockRule.DeviceID);
                command.ExecuteNonQuery();
            }
        }
        // Add similar methods for other models like Geofencing, BlockRule, etc.
    }
}