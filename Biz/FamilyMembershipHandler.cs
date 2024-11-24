using Microsoft.AspNetCore.Authorization;
using PolarExpress3.Data;
using System.Linq;
using System.Threading.Tasks;

namespace PolarExpress3.Biz
{
    public class FamilyMembershipHandler : IAuthorizationHandler
    {
        IGiftRegistry _registry;

        public FamilyMembershipHandler(IGiftRegistry reg)
        {
            _registry = reg;        
        }

        public async Task HandleAsync(AuthorizationHandlerContext context)
        {
            bool foundRequirement = false;
            if (context != null)
            {
                var pendingRequirements = context.PendingRequirements.ToList();

                foreach (var requirement in pendingRequirements)
                {
                    if (requirement is FamilyMemberRequirement)
                    {
                        string userId = context.User.Identity.Name;
                        foundRequirement = true;
                        if (!string.IsNullOrEmpty(userId))
                        {
                            if (await _registry.HasFamily(userId))
                            {
                                context.Succeed(requirement);
                            }
                        }
                    }
                }
            }
            
            return;      
        }
    }
}
