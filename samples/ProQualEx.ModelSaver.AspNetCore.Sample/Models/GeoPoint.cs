namespace ProQualEx.ModelSaver.AspNetCore.Sample.Models;

[PersistOnPostback]
public class GeoPoint
{
    public double Latitude { get; set; }

    public double Longitude { get; set; }
}
