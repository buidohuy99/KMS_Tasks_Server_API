﻿using Core.Domain.DbEntities;
using Infrastructure.Persistence.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Application.Models.Project
{
    public class ProjectResponseModel
    {
        public ProjectResponseModel(Domain.DbEntities.Project project, Domain.DbEntities.ProjectRole role)
        {
            if (project == null) return;
            Id = project.Id;
            Name = project.Name;
            Description = project.Description;
            CreatedDate = project.CreatedDate;
            CreatedBy = project.CreatedByUser == null ? null : new UserDTO(project.CreatedByUser);
            UpdatedDate = project.UpdatedDate;
            UpdatedBy = project.UpdatedByUser == null ? null : new UserDTO(project.UpdatedByUser);
            ProjectRole = role;
            IsDeleted = project.Deleted;
            if (project.Parent == null) return;
            Parent = new ProjectResponseModel(project.Parent, null);
        }

        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedDate { get; set; }
        public UserDTO CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public UserDTO UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public ProjectResponseModel Parent { get; set; }
        public ProjectRole ProjectRole { get; set; }
    }
}