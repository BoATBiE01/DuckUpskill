using DuckUpskill.Models;

namespace DuckUpskill.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalCourses { get; set; }
    public int TotalFreeCourses { get; set; }
    public int TotalPaidCourses { get; set; }
    public int TotalMembers { get; set; }
    public int TotalEnrollments { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<Order> RecentOrders { get; set; } = new();
    public List<Enrollment> RecentEnrollments { get; set; } = new();
    public List<Course> PopularCourses { get; set; } = new();
}
