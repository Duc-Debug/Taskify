using Microsoft.EntityFrameworkCore;
using Taskify.Models;
namespace Taskify.Data
{
    public class AppDbContext:DbContext
    {
     public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

    }
}
