using System;

namespace EquipmentDechargeManager.Data.Entities;

public class DechargeItem
{
    public int Id { get; set; }
    public int DechargeId { get; set; }
    public int? EquipmentId { get; set; }
    public string ConditionAtAssignment { get; set; } = string.Empty;
    public DateOnly AssignmentDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? ReturnDate { get; set; }
    public string? ConditionReturned { get; set; }

    public Decharge Decharge { get; set; } = null!;
    public Equipment? Equipment { get; set; }

    public bool IsReturned => ReturnDate.HasValue;
}
