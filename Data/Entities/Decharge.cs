using System;
using System.Collections.Generic;

namespace EquipmentDechargeManager.Data.Entities;

public class Decharge
{
    public int Id { get; set; }
    public string DechargeNumber { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }

    public Employee Employee { get; set; } = null!;
    public ICollection<DechargeItem> Items { get; set; } = new List<DechargeItem>();
}
