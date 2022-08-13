namespace JsonReader.Models;

public class Car
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string? Model { get; set; }
    public string? Vendor { get; set; }
    public int Year { get; set; }
    public string? ImagePath { get; set; }
}
