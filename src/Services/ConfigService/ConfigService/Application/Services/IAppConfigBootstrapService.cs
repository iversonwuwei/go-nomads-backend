namespace ConfigService.Application.Services;

public interface IAppConfigBootstrapService
{
    Task BootstrapAsync(CancellationToken cancellationToken = default);
}