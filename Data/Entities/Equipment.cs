using System.Collections.Generic;

namespace EquipmentDechargeManager.Data.Entities;

public class Equipment
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? InventoryNumber { get; set; }
    public string? ShCode { get; set; }
    public EquipmentStatus Status { get; set; } = EquipmentStatus.Available;

    public string DisplaySerialNumber => string.IsNullOrWhiteSpace(SerialNumber) ? "—" : SerialNumber.Trim();
    public string DisplayInventoryNumber => string.IsNullOrWhiteSpace(InventoryNumber) ? "—" : InventoryNumber.Trim();
    public string DisplayShCode => string.IsNullOrWhiteSpace(ShCode) ? "—" : ShCode.Trim();

    public ICollection<DechargeItem> DechargeItems { get; set; } = new List<DechargeItem>();
}
