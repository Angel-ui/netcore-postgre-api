using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PostgreApi.Models
{
    public class Teacher
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "El campo Nombre es requerido")]
        [Display(Name = "Nombre*")]
        public string Name { get; set; }
        [Required(ErrorMessage = "El campo Apellido Paterno es requerido")]
        [Display(Name = "Apellido Paterno*")]
        public string LastName { get; set; }
        [Required(ErrorMessage = "El campo Apellido Materno es requerido")]
        [Display(Name = "Apellido Materno*")]
        public string MaidenName { get; set; }
        [Required]
        public string Gender { get; set; }
        public string ImgURL { get; set; }
        public string FullName { get; private set; }
        public string IdPath { get; set; }
        [NotMapped]
        public IFormFile Files { get; set; }

        public IList<Course> Courses { get; set; }

    }
}
