using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PostgreApi.Models
{
    public partial class Role
    {
        public Role()
        {
            Users = new HashSet<User>();
        }
        [Key]
        public int Id { get; set; }
        [Required]
        public string RoleDesc { get; set; }

        public virtual ICollection<User> Users { get; set; }
    }
}
