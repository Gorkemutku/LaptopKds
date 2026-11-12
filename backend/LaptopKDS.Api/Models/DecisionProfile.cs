namespace LaptopKDS.Api.Models;

public class DecisionProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconEmoji { get; set; } = "💻";

    public ICollection<ProfileCriterionWeight> CriterionWeights { get; set; } = new List<ProfileCriterionWeight>();
}
