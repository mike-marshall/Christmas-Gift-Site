using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace PolarExpress3.Utils
{
    public static class Extensions
    { 
        public static async Task<string> GetUserNameAsync(this AuthenticationStateProvider prov)
        {
            AuthenticationState state = await prov.GetAuthenticationStateAsync();

            var user = state.User;

            return user.Identity.Name;
        }

        public static async Task<bool> IsLoggedInAsync(this AuthenticationStateProvider prov)
        {
            AuthenticationState state = await prov.GetAuthenticationStateAsync();

            return state.User.Identity.IsAuthenticated;
        }
    }
}
