using System;
using System.Collections.Generic;

namespace EquipmentDechargeManager.Data.Entities;

public class Decharge
{
    public int Id { get; set; }
    public string DechargeNumber { get; set; } = string.Empty;
    public int? EmployeeId { get; set; }
    public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }

    public Employee? Employee { get; set; }
    public ICollection<DechargeItem> Items { get; set; } = new List<DechargeItem>();
}
