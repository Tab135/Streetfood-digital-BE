using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BO.Entities;

public class UserVoucher
{
    [Key]
    public int UserVoucherId { get; set; }

    [ForeignKey("User")]
    public int UserId { get; set; }

    [ForeignKey("Voucher")]
    public int VoucherId { get; set; }

    public int Quantity { get; set; }
    public bool IsAvailable { get; set; } = true;
    [JsonIgnore]
    public virtual User User { get; set; }
    public virtual Voucher Voucher { get; set; }
}
