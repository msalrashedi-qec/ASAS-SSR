
namespace Shared.Dtos.General;

public class BasicDto
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string NameAr { get; set; }
    public bool IsSelected { get; set; }
    public bool IsMandatory { get; set; } = false;
}
