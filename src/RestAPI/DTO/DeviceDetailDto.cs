namespace RestAPI.DTO;

public class DeviceDetailDto
{
    public string Name { get; set; } = null!;
    public string? DeviceTypeName { get; set; }
    public bool IsEnabled { get; set; }
    public object AdditionalProperties { get; set; } = new(); // serialized JSON object
    public DeviceEmployeeDto? CurrentEmployee { get; set; }
}