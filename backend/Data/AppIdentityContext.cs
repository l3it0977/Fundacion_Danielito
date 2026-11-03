using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SISTEMADANIELITO.Data;

public class AppIdentityContext : IdentityDbContext
{
    public AppIdentityContext(DbContextOptions<AppIdentityContext> options) : base(options)
    {
    }
}
