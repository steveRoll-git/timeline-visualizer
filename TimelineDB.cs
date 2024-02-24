using SQLite;
using System.IO;
using System.Text.Json;

namespace TimelineVisualizer
{
    [Table("timeline")]
    public class TimelineRecord
    {
        [PrimaryKey]
        [Column("timestamp")]
        public DateTime Timestamp { get; set; }

        [Column("latitude")]
        public double Latitude { get; set; }

        [Column("longitude")]
        public double Longitude { get; set; }
    }

    /// <summary>
    /// Responsible for the timeline SQLite database, including parsing the JSON data and storing it.
    /// </summary>
    internal static class TimelineDB
    {
        public static string DBLocation = "db.sqlite3";

        public delegate void LoadFinishedEventHandler(int numRecords, DateTime from, DateTime to);
        public static event LoadFinishedEventHandler? LoadFinished;

        static TimelineDB()
        {
            using var db = GetConnection();
            db.CreateTable<TimelineRecord>();
        }

        private static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(DBLocation);
        }

        private static double ConvertCoordinate(int value)
        {
            return value / 1e7;
        }

        public static void LoadFromJSON(string filename, IProgress<double> progress, CancellationToken cancellationToken)
        {
            using var db = GetConnection();

            using var stream = new FileStream(filename, FileMode.Open);

            long lastReport = 0;
            int count = 0;
            DateTime minDate = DateTime.MaxValue;
            DateTime maxDate = DateTime.MinValue;

            var buffer = new byte[16];
            stream.Read(buffer);

            bool Read(ref Utf8JsonReader reader)
            {
                while (!reader.Read())
                {
                    GetMoreBytesFromStream(stream, ref buffer, ref reader);
                }
                return reader.CurrentDepth > 0 || reader.TokenType == JsonTokenType.StartObject;
            }

            var reader = new Utf8JsonReader(buffer, isFinalBlock: false, state: default);

            {
                double? latitudeP = null;
                double? longitudeP = null;
                DateTime? timestampP = null;
                while (Read(ref reader))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    if (reader.CurrentDepth == 3)
                    {
                        if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "latitudeE7")
                        {
                            Read(ref reader);
                            latitudeP = ConvertCoordinate(reader.GetInt32());
                        }
                        if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "longitudeE7")
                        {
                            Read(ref reader);
                            longitudeP = ConvertCoordinate(reader.GetInt32());
                        }
                        if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "timestamp")
                        {
                            Read(ref reader);
                            timestampP = reader.GetDateTime();
                        }
                        if (latitudeP is double latitude && longitudeP is double longitude && timestampP is DateTime timestamp)
                        {
                            db.InsertOrReplace(new TimelineRecord() { Latitude = latitude, Longitude = longitude, Timestamp = timestamp });
                            count++;
                            minDate = timestamp < minDate ? timestamp : minDate;
                            maxDate = timestamp > maxDate ? timestamp : maxDate;

                            latitudeP = null;
                            longitudeP = null;
                            timestampP = null;
                        }
                    }
                    if (stream.Position - lastReport >= stream.Length / 1000)
                    {
                        progress.Report((double)stream.Position / stream.Length);
                        lastReport = stream.Position;
                    }
                }
            }

            progress.Report(1);
            LoadFinished?.Invoke(count, minDate, maxDate);
        }

        private static void GetMoreBytesFromStream(Stream stream, ref byte[] buffer, ref Utf8JsonReader reader)
        {
            int bytesRead;
            if (reader.BytesConsumed < buffer.Length)
            {
                ReadOnlySpan<byte> leftover = buffer.AsSpan((int)reader.BytesConsumed);

                if (leftover.Length == buffer.Length)
                {
                    Array.Resize(ref buffer, buffer.Length * 2);
                }

                leftover.CopyTo(buffer);
                bytesRead = stream.Read(buffer.AsSpan(leftover.Length));
            }
            else
            {
                bytesRead = stream.Read(buffer);
            }
            reader = new Utf8JsonReader(buffer, isFinalBlock: bytesRead == 0, reader.CurrentState);
        }
    }
}
