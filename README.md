to successfully launch this application, ensure that your appsettings.json file includes the following configuration structure:

{
  "ConnectionStrings": {
    "DefaultConnection": "<your-database-connection-string>"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}

why I didn't separate into different projects:
- simplicity
- less dependencies, cause I don't really like to deal with them
- all the logic is in one project, so I don't need to separate it
