using System.Collections.Generic;

namespace EquipmentDechargeManager.Data.Entities;

public class Employee
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Matricule { get; set; } = string.Empty;
    public string Function { get; set; } = string.Empty;
    public string Structure { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;

    // User-friendly display for lists and dropdowns
    public string DisplayName => $"{Matricule} — {FullName} — {Function}";

    public override string ToString() => DisplayName;

    public ICollection<Decharge> Decharges { get; set; } = new List<Decharge>();
}
