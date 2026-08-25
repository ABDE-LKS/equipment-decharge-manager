using System;

namespace EquipmentDechargeManager.Data.Entities;

public class EquipmentReturn
{
    public int Id { get; set; }
    public int DechargeItemId { get; set; }
    public DateTime ReturnDate { get; set; } = DateTime.UtcNow;
    public string ConditionReturned { get; set; } = string.Empty;

    public DechargeItem DechargeItem { get; set; } = null!;
}
