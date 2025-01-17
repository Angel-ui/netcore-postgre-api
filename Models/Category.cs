﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PostgreApi.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Description { get; set; }
        public IList<Course> Courses { get; set; }
    }
}
