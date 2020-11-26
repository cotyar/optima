using System.Threading.Tasks;
using Dapr.Actors;
using Optima.Domain.Core;
using Optima.Domain.Security;

namespace Optima.Interfaces
{
    public interface ISecurityRegistry : IActor
    {
        Task<PrincipalPermissions> AddPrincipal(Principal principal);
        Task<PrincipalPermissions> AddPrincipalPermissions(PrincipalPermissions principalPermissions);
        Task<PrincipalPermissions> RevokePrincipalPermissions(PrincipalPermissions principalPermissions);
        
        Task<PrincipalPermissions> GetPrincipalPermissions(UUID principalId);
        Task<Permissions> GetPermissions();
        Task<Principal> GetPrincipal(UUID principalId);
        Task<Principal[]> GetPrincipals();
    }
}
