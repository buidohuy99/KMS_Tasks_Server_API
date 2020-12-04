﻿using Microsoft.AspNetCore.Identity;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Core.Domain.DbEntities
{
    public class ApplicationUser : IdentityUser
    {
        public long UserId { get; set; }
        public string LoginProvider { get; set; }
        public string ProviderKey { get; set; }
        public string FirstName { get; set; }
        public string MidName { get; set; }
        public string LastName { get; set; }
        public string AvatarUrl { get; set; }
        public string Address { get; set; }
        public string Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public byte Status { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        public virtual ICollection<Project> ProjectsCreated { get; set; }
        public virtual ICollection<Project> ProjectsUpdated { get; set; }
        public virtual ICollection<Tasks> AssignedTasks { get; set; }
        public virtual ICollection<Tasks> TasksAssigned { get; set; }
        public virtual ICollection<Tasks> TasksCreated { get; set; }
    }
}
