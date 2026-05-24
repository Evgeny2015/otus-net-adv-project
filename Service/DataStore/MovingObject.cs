using SourceGenerator;

namespace DataStore;

/// <summary>
/// Represents a real-time moving object with geospatial and motion attributes.
/// </summary>
[GenerateBinarySerializer]
public partial class MovingObject
{
    /// <summary>
    /// Geographic coordinates (longitude, latitude)
    /// </summary>
    public (double Longitude, double Latitude) Coordinates { get; set; }

    /// <summary>
    /// Altitude in meters above sea level
    /// </summary>
    public double Altitude { get; set; }

    /// <summary>
    /// Horizontal accuracy in meters
    /// </summary>
    public double Accuracy { get; set; }

    /// <summary>
    /// Vertical accuracy in meters
    /// </summary>
    public double VerticalAccuracy { get; set; }

    /// <summary>
    /// Speed in meters per second
    /// </summary>
    public double Speed { get; set; }

    /// <summary>
    /// Direction in degrees (0-360)
    /// </summary>
    public double Direction { get; set; }

    /// <summary>
    /// Magnetic variation in degrees
    /// </summary>
    public double MagneticVariation { get; set; }

    /// <summary>
    /// Acceleration components in meters per second^2
    /// </summary>
    public Acceleration Acceleration { get; set; } = new Acceleration();

    /// <summary>
    /// Timestamp of the last position update
    /// </summary>
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Default constructor
    /// </summary>
    public MovingObject() { }

    /// <summary>
    /// Constructor with initial coordinates
    /// </summary>
    public MovingObject(double longitude, double latitude)
    {
        Coordinates = (longitude, latitude);
    }

    /// <summary>
    /// Updates the position with new coordinates and timestamp
    /// </summary>
    public void UpdatePosition(double longitude, double latitude, double altitude = 0)
    {
        Coordinates = (longitude, latitude);
        Altitude = altitude;
        LastUpdate = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns a string representation of the moving object
    /// </summary>
    public override string ToString()
    {
        return $"MovingObject at ({Coordinates.Longitude:F6}, {Coordinates.Latitude:F6}), Alt: {Altitude:F1}m, Speed: {Speed:F1}m/s, Dir: {Direction:F1}°";
    }
}

/// <summary>
/// Represents acceleration components in three dimensions
/// </summary>
public class Acceleration
{
    /// <summary>
    /// Linear acceleration (forward/backward) in m/s²
    /// </summary>
    public double Linear { get; set; }

    /// <summary>
    /// Lateral acceleration (left/right) in m/s²
    /// </summary>
    public double Lateral { get; set; }

    /// <summary>
    /// Vertical acceleration (up/down) in m/s²
    /// </summary>
    public double Vertical { get; set; }

    /// <summary>
    /// Calculates the total acceleration magnitude
    /// </summary>
    public double Magnitude => Math.Sqrt(Linear * Linear + Lateral * Lateral + Vertical * Vertical);

    /// <summary>
    /// Returns a string representation of acceleration
    /// </summary>
    public override string ToString()
    {
        return $"Acceleration(L: {Linear:F2}, Lat: {Lateral:F2}, V: {Vertical:F2}) m/s²";
    }
}