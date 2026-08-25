using System;

namespace EquipmentDechargeManager.Data.Entities;

public class DechargeItem
{
    public int Id { get; set; }
    public int DechargeId { get; set; }
    public int EquipmentId { get; set; }
    public string ConditionAtAssignment { get; set; } = string.Empty;

    public Decharge Decharge { get; set; } = null!;
    public Equipment Equipment { get; set; } = null!;
    public EquipmentReturn? ReturnRecord { get; set; }

    public bool IsReturned => ReturnRecord != null;
}
