namespace PracticeMonitoring.Api.Dtos;

public class PushDeviceRegistrationRequest
{
    public string? Token { get; set; }

    public string? Platform { get; set; }

    public string? DeviceName { get; set; }
}

public class PushSettingsResponse
{
    public bool IsConfigured { get; set; }

    public bool HasRegisteredDevice { get; set; }
}
