﻿using Core.Application.Helper.Exceptions.Project;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Domain.Constants;
using Core.Domain.DbEntities;
using Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Infrastructure.Persistence.DTOs;
using Core.Application.Models.Project;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Services
{
    public class ProjectService : IProjectService
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected ILogger<ProjectService> _logger;
        protected readonly UserManager<ApplicationUser> _userManager;

        public ProjectService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, ILogger<ProjectService> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        async Task<ProjectResponseModel> IProjectService.AddNewProject(long createdByUserId, NewProjectModel newProject)
        {
            if (newProject.Name == null || newProject.Name.Length <= 0) throw new ProjectServiceException(400, "Cannot create new project without a name");

            await using var t = await _unitOfWork.CreateTransaction();

            try
            {
                // Check if uid is valid or not
                ApplicationUser validUser = _userManager.Users.FirstOrDefault(e => e.UserId == createdByUserId);
                if (validUser == null)
                {
                    throw new ProjectServiceException(404, "Cannot locate a valid user from the claim provided");
                }

                // Check if its parent project is valid
                if (newProject.ParentId != null)
                {
                    var parent = from project in _unitOfWork.Repository<Project>().GetDbset()
                                 where project.Id == newProject.ParentId
                                 select project;
                    if (parent == null || parent.Count() < 1)
                    {
                        throw new ProjectServiceException(404, "Cannot find a single instance of a parent project from the infos you provided");
                    }
                    if (parent.Count() > 1)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("Inconsistency in database. Executing query returns more than one result: ");
                        sb.AppendLine(parent.ToList().ToString());
                        throw new Exception(sb.ToString());
                    }
                }

                // Add project in first
                Project addedProject = new Project()
                {
                    Name = newProject.Name,
                    Description = newProject.Description,
                    CreatedBy = validUser.UserId,
                    UpdatedBy = validUser.UserId,
                    ParentId = newProject.ParentId,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    Deleted = false,
                };
                addedProject = await _unitOfWork.Repository<Project>().InsertAsync(addedProject);
                await _unitOfWork.SaveChangesAsync();

                // Add user project: project's relation to an owner
                UserProjects relationToUser = new UserProjects()
                {
                    UserId = validUser.UserId,
                    ProjectId = addedProject.Id,
                    RoleId = Enums.ProjectRoles.Owner,
                };
                await _unitOfWork.Repository<UserProjects>().InsertAsync(relationToUser);
                await _unitOfWork.SaveChangesAsync();

                List<ProjectRole> roles = new List<ProjectRole>();
                var entry = _unitOfWork.Entry(relationToUser);
                await entry.Reference(e => e.ProjectRole).LoadAsync();
                roles.Add(relationToUser.ProjectRole);

                await t.CommitAsync();

                return new ProjectResponseModel(addedProject, roles);
            }
            catch (Exception ex)
            {
                await t.RollbackAsync();
                _logger.LogError(ex, "An error occurred when using ProjectService");
                throw ex;
            }
        }

        async Task<IEnumerable<ProjectResponseModel>> IProjectService.GetAllProjects(GetAllProjectsModel model)
        {
            if (model.UserID == null) throw new ProjectServiceException(400, "Cannot find projects of this user if you don't provide a UserID");

            await using var t = await _unitOfWork.CreateTransaction();

            try
            {
                // Check if uid is valid or not
                ApplicationUser validUser = _userManager.Users.FirstOrDefault(e => e.UserId == model.UserID);
                if (validUser == null)
                {
                    throw new ProjectServiceException(404, "Cannot locate a valid user from the claim provided");
                }

                // Query for participations in projects with the provided info => roles
                var participation = (from userProject in _unitOfWork.Repository<UserProjects>().GetDbset()
                                     where userProject.UserId == model.UserID
                                     select userProject);
                // If cannot find any participation from the infos provided, return a service exception
                if (participation == null || participation.Count() < 1)
                {
                    throw new ProjectServiceException(404, "Cannot find any project you participated in");
                }
                // Get all the projects participated, then for each of them 
                var resultProjects = _unitOfWork.Repository<Project>().GetDbset()
                    .Where(project => participation.Any(p => p.ProjectId == project.Id && project.Deleted == false)).ToList();

                List<ProjectResponseModel> result = new List<ProjectResponseModel>();
                var projectRoles = _unitOfWork.Repository<ProjectRole>().GetDbset();
                foreach(var project in resultProjects)
                {
                    // get the roles for this project
                    var roles = projectRoles.Where(role => participation.Where(p => p.ProjectId == project.Id)
                                .Any(p => p.RoleId == role.Id));
                    result.Add(new ProjectResponseModel(project, roles));
                }

                await _unitOfWork.SaveChangesAsync();

                return result;
            }
            catch (Exception ex)
            {
                await t.RollbackAsync();
                _logger.LogError(ex, "An error occurred when using ProjectService");
                throw ex;
            }
        }

        async Task<ProjectResponseModel> IProjectService.GetOneProject(GetOneProjectModel model)
        {
            if (model.UserId == null || model.ProjectId == null) throw new ProjectServiceException(400, "Cannot find projects of this user if you don't provide a UserID");

            await using var t = await _unitOfWork.CreateTransaction();

            try
            {
                // Check if uid is valid or not
                ApplicationUser validUser = _userManager.Users.FirstOrDefault(e => e.UserId == model.UserId);
                if (validUser == null)
                {
                    throw new ProjectServiceException(404, "Cannot locate a valid user from the claim provided");
                }

                // Query for participations in projects with the provided info => roles
                var participation = (from userProject in _unitOfWork.Repository<UserProjects>().GetDbset()
                                     where userProject.UserId == model.UserId && userProject.ProjectId == model.ProjectId
                                     select userProject);
                // If cannot find any participation from the infos provided, return a service exception
                if (participation == null || participation.Count() < 1)
                {
                    throw new ProjectServiceException(404, "Cannot find any project you participated in");
                }

                // Get the only one project participated 
                var resultProject = _unitOfWork.Repository<Project>().GetDbset()
                    .Where(project => participation.Any(p => p.ProjectId == project.Id && project.Deleted == false));
                // If cannot find the project from the infos provided, return a service exception
                if (resultProject == null || resultProject.Count() < 1)
                {
                    throw new ProjectServiceException(404, "Cannot find a single instance of a project from the infos you provided");
                }
                // Corrupted Db
                if (resultProject.Count() > 1)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Inconsistency in database. Executing query returns more than one result: ");
                    sb.AppendLine(resultProject.ToList().ToString());
                    throw new Exception(sb.ToString());
                }

                List<ProjectResponseModel> result = new List<ProjectResponseModel>();
                var projectRoles = _unitOfWork.Repository<ProjectRole>().GetDbset();
                foreach(var project in resultProject)
                {
                    // get the roles for this project
                    var roles = projectRoles.Where(role => participation.Where(p => p.ProjectId == project.Id)
                                .Any(p => p.RoleId == role.Id));
                    result.Add(new ProjectResponseModel(project, roles));
                }

                await _unitOfWork.SaveChangesAsync();

                return result[0];
            }
            catch (Exception ex)
            {
                await t.RollbackAsync();
                _logger.LogError(ex, "An error occurred when using ProjectService");
                throw ex;
            }
        }

        public async Task<ProjectResponseModel> UpdateProjectInfo(long projectId, long updatedByUserId, UpdateProjectInfoModel model)
        {
            // Start the update transaction
            await using var t = await _unitOfWork.CreateTransaction();
            try
            {
                // Check if uid is valid or not
                ApplicationUser validUser = _userManager.Users.FirstOrDefault(e => e.UserId == updatedByUserId);
                if (validUser == null)
                {
                    throw new ProjectServiceException(404, "Cannot locate a valid user from the claim provided");
                }

                // Check if project is in db first
                var result = from project in _unitOfWork.Repository<Project>().GetDbset()
                             where project.Id == projectId
                             select project;
                if (result == null || result.Count() < 1)
                {
                    throw new ProjectServiceException(404, "Cannot find a single instance of a project from the infos you provided");
                }
                if (result.Count() > 1)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Inconsistency in database. Executing query returns more than one result: ");
                    sb.AppendLine(result.ToList().ToString());
                    throw new Exception(sb.ToString());
                }

                Project operatedProject = result.ToList()[0];

                // Get if user have the authorization to change project info
                var getUserProject = from userProject in _unitOfWork.Repository<UserProjects>().GetDbset()
                                     where userProject.UserId == validUser.UserId && userProject.ProjectId == operatedProject.Id
                                     select userProject;
                if (getUserProject == null || getUserProject.Count() < 1)
                {
                    throw new ProjectServiceException(404, "Cannot find the project you are looking for");
                }

                // flag to know if any field is going to be changed or not
                bool isUpdated = false;

                // Check if its parent project is valid
                if (model.ParentId != null)
                {
                    var parent = from project in _unitOfWork.Repository<Project>().GetDbset()
                                 where project.Id == model.ParentId
                                 select project;
                    if (parent == null || parent.Count() < 1)
                    {
                        throw new ProjectServiceException(404, "Cannot find a single instance of a project from the infos you provided");
                    }
                    if (parent.Count() > 1)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("Inconsistency in database. Executing query returns more than one result: ");
                        sb.AppendLine(parent.ToList().ToString());
                        throw new Exception(sb.ToString());
                    }

                    Project newParentProject = parent.ToList()[0];

                    if(newParentProject.Id == operatedProject.Id)
                    {
                        throw new ProjectServiceException(400, "Cannot set a project to be its own parent");
                    }

                    // Only  register change only if parentId is not sent together with removefromparent field (we ignore the change)
                    if (newParentProject.Id != operatedProject.ParentId && (model.MakeParentless == null || !model.MakeParentless.Value))
                    {
                        operatedProject.ParentId = newParentProject.Id;
                        isUpdated = true;
                    }
                }

                // If remove parent is true and the item has a parent, then we register the change
                if (operatedProject.ParentId != null && model.MakeParentless != null && model.MakeParentless.Value)
                {
                    operatedProject.ParentId = null;
                    isUpdated = true;
                }

                //Update fields
                if (model.Name != null && model.Name.Length > 0 && model.Name != operatedProject.Name)
                {
                    operatedProject.Name = model.Name;
                    isUpdated = true;
                }
                if (model.Description != null && model.Description.Length > 0 && model.Description != operatedProject.Description)
                {
                    operatedProject.Description = model.Description;
                    isUpdated = true;
                }

                // If there is any update, we update the object
                if (isUpdated) {
                    operatedProject.UpdatedBy = validUser.UserId;
                    operatedProject.UpdatedDate = DateTime.UtcNow;
                    _unitOfWork.Repository<Project>().Update(operatedProject);
                    await _unitOfWork.SaveChangesAsync();
                }       

                await t.CommitAsync();

                return new ProjectResponseModel(operatedProject, getUserProject.Select(e => e.ProjectRole).ToList());
            }
            catch (Exception ex)
            {
                await t.RollbackAsync();
                _logger.LogError(ex, "An error occurred when using ProjectService");
                throw ex;
            }
        }

        public async Task<ProjectResponseModel> SoftDeleteExistingProject(long projectId, long deletedByUserId)
        {
            // Start the update transaction
            await using var t = await _unitOfWork.CreateTransaction();
            try
            {
                // Check if uid is valid or not
                ApplicationUser validUser = _userManager.Users.FirstOrDefault(e => e.UserId == deletedByUserId);
                if (validUser == null)
                {
                    throw new ProjectServiceException(404, "Cannot locate a valid user from the claim provided");
                }

                // Check if project is in db first
                var result = from project in _unitOfWork.Repository<Project>().GetDbset()
                             where project.Id == projectId
                             select project;
                if (result == null || result.Count() < 1)
                {
                    throw new ProjectServiceException(404, "Cannot find a single instance of a project from the infos you provided");
                }
                if (result.Count() > 1)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Inconsistency in database. Executing query returns more than one result: ");
                    sb.AppendLine(result.ToList().ToString());
                    throw new Exception(sb.ToString());
                }

                Project operatedProject = result.ToList()[0];

                // Get if user have the authorization to change project info
                var getUserProject = from userProject in _unitOfWork.Repository<UserProjects>().GetDbset()
                                     where userProject.UserId == validUser.UserId && userProject.ProjectId == operatedProject.Id
                                     select userProject;
                if (getUserProject == null || getUserProject.Count() < 1)
                {
                    throw new ProjectServiceException(404, "Cannot find the project you are looking for");
                }

                // flag to know if any field is going to be changed or not
                bool isUpdated = false;

                if (!operatedProject.Deleted)
                {
                    operatedProject.Deleted = true;
                    isUpdated = true;
                }

                // If there is any update, we update the object
                if (isUpdated)
                {
                    operatedProject.UpdatedBy = validUser.UserId;
                    operatedProject.UpdatedDate = DateTime.UtcNow;
                    _unitOfWork.Repository<Project>().Update(operatedProject);
                    await _unitOfWork.SaveChangesAsync();
                }

                await t.CommitAsync();

                return new ProjectResponseModel(operatedProject, getUserProject.Select(e => e.ProjectRole).ToList());
            }
            catch (Exception ex)
            {
                await t.RollbackAsync();
                _logger.LogError(ex, "An error occurred when using ProjectService");
                throw ex;
            }
        }
    }
}
