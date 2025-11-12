namespace Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;

public interface IGeo
{
    public decimal Longitude { get; set; }
    public decimal Latitude { get; set; }
}