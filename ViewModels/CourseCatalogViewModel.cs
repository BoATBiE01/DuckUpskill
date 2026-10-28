using DuckUpskill.Models;

namespace DuckUpskill.ViewModels;

public class CourseCatalogViewModel
{
    public IEnumerable<Course> Courses { get; set; } = Enumerable.Empty<Course>();
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public string? PriceType { get; set; }
    public IEnumerable<string> Categories { get; set; } = Enumerable.Empty<string>();
}
