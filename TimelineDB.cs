using SQLite;
using System.Drawing;
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

    [Table("places")]
    public class Place
    {
        [PrimaryKey]
        [Column("name")]
        public string Name { get; set; }

        [Column("color")]
        public KnownColor Color { get; set; }

        [Column("latitude")]
        public double Latitude { get; set; }

        [Column("longitude")]
        public double Longitude { get; set; }

        [Column("radius")]
        public double Radius { get; set; }
    }

    public class PlaceDaySection
    {
        public Place? Place;
        public double StartTime;
        public double EndTime;
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
            db.CreateTable<Place>();
        }

        public static SQLiteConnection GetConnection()
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

        public static List<Place> GetPlaces(SQLiteConnection db)
        {
            return [.. db.Table<Place>()];
        }

        public static List<Place> GetPlaces()
        {
            var db = GetConnection();
            return GetPlaces(db);
        }

        private static double ToRadians(double degree)
        {
            return Math.PI / 180 * degree;
        }

        private static double GetDistance(double lat1, double long1, double lat2, double long2)
        {
            // Convert the latitudes 
            // and longitudes
            // from degree to radians.
            lat1 = ToRadians(lat1);
            long1 = ToRadians(long1);
            lat2 = ToRadians(lat2);
            long2 = ToRadians(long2);

            // Haversine Formula
            double dlong = long2 - long1;
            double dlat = lat2 - lat1;

            double ans = Math.Pow(Math.Sin(dlat / 2), 2) +
                                  Math.Cos(lat1) * Math.Cos(lat2) *
                                  Math.Pow(Math.Sin(dlong / 2), 2);

            ans = 2 * Math.Asin(Math.Sqrt(ans));

            double R = 6371000;

            ans *= R;

            return ans;
        }

        private static Place? GetClosestPlace(this List<Place> places, double latitude, double longitude)
        {
            return places.FirstOrDefault(p =>
            GetDistance(p.Latitude, p.Longitude, latitude, longitude) <= p.Radius);
        }

        private static double TimeToFactor(DateTime time)
        {
            return time.TimeOfDay.TotalSeconds / (24 * 60 * 60);
        }

        public static List<PlaceDaySection> GetPlaceDaySections(DateTime date)
        {
            using var db = GetConnection();
            var places = GetPlaces(db);
            var upperDate = date.AddDays(1);
            var records = db.Table<TimelineRecord>().Where(r => r.Timestamp >= date && r.Timestamp < upperDate).ToList();
            var sections = new List<PlaceDaySection>();
            foreach (var record in records)
            {
                var place = places.GetClosestPlace(record.Latitude, record.Longitude);
                if (sections.Count == 0)
                {
                    sections.Add(new PlaceDaySection { Place = place, StartTime = TimeToFactor(record.Timestamp) });
                }
                else
                {
                    sections.Last().EndTime = TimeToFactor(record.Timestamp);
                    if (sections.Last().Place != place)
                    {
                        sections.Add(new PlaceDaySection
                        {
                            Place = place,
                            StartTime = TimeToFactor(record.Timestamp)
                        });
                    }
                }
            }
            if (sections.Count > 0)
            {
                var lastSection = sections.Last();
                lastSection.EndTime = 1;
            }
            else
            {
                sections.Add(new PlaceDaySection
                {
                    Place = null,
                    StartTime = 0,
                    EndTime = 1
                });
            }
            if (sections[0].StartTime > 1/24/60)
            {
                sections.Insert(0, new PlaceDaySection { StartTime = 0, EndTime = sections[0].StartTime });
            }
            return sections;
        }
    }
}
