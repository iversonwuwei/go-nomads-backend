namespace UserService.Configuration;

public class SupabaseSettings
{
    public string Url { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Schema { get; set; } = "public";
}
