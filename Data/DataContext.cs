using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ecode.Data
{
    // [Table("xp_sys_base_class")]
    public class BaseClass2
    {
        public string Id { get; set; }
        public int orgId { get; set; }
    }
    public class DataContext : DbContext
    {
        public DataContext() { }
        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {

        }
        public DbSet<BaseClass2> Blogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseNpgsql(@"Host=10.1.7.61;Username=postgres;Password=postgres;Database=postgres");
        }
    }
}