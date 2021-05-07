using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PostgreApi.Models
{
    public partial class RefreshToken
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; }
        public DateTime ExpiryDate { get; set; }

        public virtual User User { get; set; }
    }
}
