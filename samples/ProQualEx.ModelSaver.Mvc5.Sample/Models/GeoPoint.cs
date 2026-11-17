namespace ProQualEx.ModelSaver.Mvc5.Sample.Models;

/// <summary>
/// A geographic coordinate pair, persisted as nested hidden fields.
/// </summary>
[PersistOnPostback]
public class GeoPoint
{
    /// <summary>
    /// Gets or sets the latitude in decimal degrees, or <see langword="null"/> when unknown.
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Gets or sets the longitude in decimal degrees, or <see langword="null"/> when unknown.
    /// </summary>
    public double? Longitude { get; set; }
}
