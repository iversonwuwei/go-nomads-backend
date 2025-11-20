using CityService.Domain.Entities;

namespace CityService.Domain.Repositories;

public interface IUserCityProsConsRepository
{
    Task<CityProsCons> AddAsync(CityProsCons prosCons);
    Task<List<CityProsCons>> GetByCityIdAsync(string cityId, bool? isPro = null);
    Task<CityProsCons?> GetByIdAsync(Guid id);
    Task<CityProsCons> UpdateAsync(CityProsCons prosCons);
    Task<bool> DeleteAsync(Guid id, Guid userId);
    
    // 投票相关方法
    Task<CityProsConsVote?> GetUserVoteAsync(Guid prosConsId, Guid userId);
    Task<CityProsConsVote> AddVoteAsync(CityProsConsVote vote);
    Task<bool> DeleteVoteAsync(Guid voteId);
    Task<CityProsConsVote> UpdateVoteAsync(CityProsConsVote vote);
}