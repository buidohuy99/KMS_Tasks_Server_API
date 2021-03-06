﻿using System.ComponentModel.DataAnnotations;

namespace UM.Core.Application.Models
{
    public class UserRegisterModel
    {
        [Required]
        public string FirstName { get; set; }

        public string MidName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }
    }
}
