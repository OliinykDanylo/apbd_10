namespace RestAPI.DTO;

public class DeviceCreateDto
{
    public string Name { get; set; } = null!;
    public string DeviceTypeName { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public object AdditionalProperties { get; set; } = new(); // object will be stored as JSON
}