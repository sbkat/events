using Microsoft.EntityFrameworkCore;

namespace event_scheduler.Models
{
    public class MyContext : DbContext
    {
        public MyContext(DbContextOptions options) : base(options) { }
    }
}