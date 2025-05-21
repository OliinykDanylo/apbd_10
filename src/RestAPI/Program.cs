using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RestAPI;
using RestAPI.DTO;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<DeviceHubContext>(options => options.UseSqlServer(connectionString));


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/api/devices", async (DeviceHubContext context) =>
{
    var result = await context.Devices
        .Select(d => new { d.Id, d.Name })
        .ToListAsync();

    return Results.Ok(result);
});

app.MapGet("/api/devices/{id}", async (int id, DeviceHubContext context) =>
{
    var device = await context.Devices
        .Include(d => d.DeviceType)
        .Include(d => d.DeviceEmployees)
        .ThenInclude(de => de.Employee)
        .ThenInclude(e => e.Person)
        .FirstOrDefaultAsync(d => d.Id == id);

    if (device == null)
        return Results.NotFound();
    
    var currentEmployeeRelation = device.DeviceEmployees
        .OrderByDescending(de => de.IssueDate) // optional, if you track it
        .FirstOrDefault();

    var dto = new DeviceDetailDto
    {
        Name = device.Name,
        DeviceTypeName = device.DeviceType?.Name,
        IsEnabled = device.IsEnabled,
        AdditionalProperties = JsonSerializer.Deserialize<object>(device.AdditionalProperties),
        CurrentEmployee = currentEmployeeRelation?.Employee != null
            ? new DeviceEmployeeDto
            {
                Id = currentEmployeeRelation.Employee.Id,
                FullName = currentEmployeeRelation.Employee.Person.FirstName + " " + currentEmployeeRelation.Employee.Person.MiddleName + " "  +   currentEmployeeRelation.Employee.Person.LastName
            }
            : null
    };

    return Results.Ok(dto);
});

app.MapPost("/api/devices", async (DeviceCreateDto dto, DeviceHubContext context) =>
{
    var deviceType = await context.DeviceTypes.FirstOrDefaultAsync(dt => dt.Name == dto.DeviceTypeName);
    if (deviceType == null) return Results.BadRequest("Invalid device type name.");

    var device = new Device
    {
        Name = dto.Name,
        DeviceTypeId = deviceType.Id,
        IsEnabled = dto.IsEnabled,
        AdditionalProperties = JsonSerializer.Serialize(dto.AdditionalProperties)
    };

    context.Devices.Add(device);
    await context.SaveChangesAsync();

    return Results.Created($"/api/devices/{device.Id}", new { device.Id });
});

app.MapPut("/api/devices/{id}", async (int id, DeviceCreateDto dto, DeviceHubContext context) =>
{
    var device = await context.Devices.FirstOrDefaultAsync(d => d.Id == id);
    if (device == null)
        throw new InvalidOperationException($"Device with ID {id} not found.");

    var deviceType = await context.DeviceTypes.FirstOrDefaultAsync(dt => dt.Name == dto.DeviceTypeName);
    if (deviceType == null)
        return Results.BadRequest("Invalid device type name.");

    device.Name = dto.Name;
    device.DeviceTypeId = deviceType.Id;
    device.IsEnabled = dto.IsEnabled;
    device.AdditionalProperties = JsonSerializer.Serialize(dto.AdditionalProperties);

    await context.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/devices/{id}", async (int id, DeviceHubContext context) =>
{
    var device = await context.Devices.FindAsync(id);
    if (device == null)
        throw new InvalidOperationException($"Device with ID {id} not found.");

    context.Devices.Remove(device);
    await context.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/api/employees", async (DeviceHubContext context) =>
{
    var employees = await context.Employees
        .Include(e => e.Person)
        .Select(e => new
        {
            e.Id,
            FullName = e.Person.FirstName + " " + e.Person.MiddleName + " " + e.Person.LastName
        })
        .ToListAsync();

    return Results.Ok(employees);
});

app.MapGet("/api/employees/{id}", async (int id, DeviceHubContext context) =>
{
    var employee = await context.Employees
        .Include(e => e.Person)
        .Include(e => e.Position)
        .FirstOrDefaultAsync(e => e.Id == id);

    if (employee == null) return Results.NotFound();
    
    string fullName = string.Join(" ", new[]
    {
        employee.Person.FirstName,
        employee.Person.MiddleName,
        employee.Person.LastName
    }.Where(name => !string.IsNullOrEmpty(name)));


    return Results.Ok(new
    {
        fullName,
        employee.Salary,
        employee.HireDate,
        Position = new
        {
            employee.Position.Id,
            employee.Position.Name
        }
    });
});

app.UseAuthorization();

app.MapControllers();

app.Run();