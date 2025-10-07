using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Models
{
    public class PaymentTransaction
    {
        public int Id { get; set; }

        public int ClubId { get; set; }
        public Club Club { get; set; } = null!;

        [Required, MaxLength(50)]
        public string Type { get; set; } = "Income"; // Income / Expense / Fund

        public decimal Amount { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string Method { get; set; } = "Cash"; // Cash / Online

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        public int? CreatedById { get; set; }
        public User? CreatedBy { get; set; }
    }
}

