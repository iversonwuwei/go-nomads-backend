namespace DocumentService.Configuration;

public class ServiceConfiguration
{
    public ServiceInfo? Gateway { get; set; }
    public ServiceInfo? ProductService { get; set; }
    public ServiceInfo? UserService { get; set; }
}

public class ServiceInfo
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string OpenApiUrl { get; set; } = string.Empty;
}
