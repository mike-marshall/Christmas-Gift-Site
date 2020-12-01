using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PolarExpress3.Data
{
    public class AppUser : Mobsites.Cosmos.Identity.IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

    }
}
