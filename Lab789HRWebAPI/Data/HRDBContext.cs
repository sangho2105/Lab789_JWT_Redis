using Lab789HRWebAPI.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lab789HRWebAPI.Data
{
    public class HRDBContext : DbContext
    {
        public HRDBContext(DbContextOptions<HRDBContext> options) : base(options){ }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<DepartmentTransferRequest> DepartmentTransferRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Employee>().HasData(
                new Employee
                {
                    Id = 1,
                    FullName = "Sang Ho",
                    Email = "sangho@gmail.com",
                    Department = "IT",
                    Position = "Dev",
                    Salary = 1500
                },
                new Employee
                {
                    Id = 2,
                    FullName = "Nhu Nguyen",
                    Email = "nhunhu@gmail.com",
                    Department = "Marketing",
                    Position = "Content Creator",
                    Salary = 2000
                },
                new Employee
                {
                    Id = 3,
                    FullName = "Bo Ho",
                    Email = "boho@gmail.com",
                    Department = "HR",
                    Position = "Admin",
                    Salary = 1000
                },
                new Employee
                {
                    Id = 4,
                    FullName = "Nguyen Van Nhan Vien",
                    Email = "employee@gmail.com",
                    Department = "IT",
                    Position = "Junior Developer",
                    Salary = 1200
                }
                );
        }
    }
}
