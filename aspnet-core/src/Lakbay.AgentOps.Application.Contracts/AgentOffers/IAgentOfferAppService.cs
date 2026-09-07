using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Lakbay.AgentOps.AgentOffers;

public interface IAgentOfferAppService : IApplicationService
{
    Task<AgentOfferDto> GetOfferAsync(string destinationCode);
}
