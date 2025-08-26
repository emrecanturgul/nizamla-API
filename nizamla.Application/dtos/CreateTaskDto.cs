using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nizamla.Application.dtos
{
    public class CreateTaskDto
    {
        [Required(ErrorMessage = "Başlık zorunludur")]
        [MaxLength(100, ErrorMessage = "Başlık en fazla 100 karakter olabilir")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir")]
        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

     
    }
}
