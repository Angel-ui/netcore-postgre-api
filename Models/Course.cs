using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostgreApi.Models
{
    public class Course
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "El campo Nombre es requerido")]
        [Display(Name = "Nombre del Curso*")]
        public string Name { get; set; }
        [Required]
        [Display(Name ="Cantidad de Clases")]
        public int? ClassesQty { get; set; }
        [Required]
        [Column(TypeName ="decimal(18,4)")]
        public decimal? Price { get; set; }
        public string ImgURL { get; set; }
        [Required]
        public int? TeacherId { get; set; }
        [Required]
        public int? CategoryId { get; set; }
        public string IdPath { get; set; }
        [NotMapped]
        public IFormFile Files { get; set; }
        public Teacher Teacher { get; set; }
        public Category Category { get; set; }
    }
}
