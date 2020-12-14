﻿using Core.Application.Helper.Exceptions;
using Core.Application.Helper.Exceptions.Task;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Task;
using Core.Domain.Constants;
using Core.Domain.DbEntities;
using Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Core.Domain.Constants.Enums;

namespace Infrastructure.Persistence.Services
{
    public class TaskService : ITaskService
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected ILogger<TaskService> _logger;
        protected readonly UserManager<ApplicationUser> _userManager;

        public TaskService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, ILogger<TaskService> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<TaskResponseModel> AddNewTask(long createdByUserId, NewTaskModel task)
        {
            if (task.Name == null || task.Name.Length <= 0) throw new TaskServiceException(400, "Cannot create new task without a name");

            if (task.ProjectId == null) throw new TaskServiceException(400, "Cannot create new task without an associated project");

            if (task.PriorityId != null && ((int)task.PriorityId < 0 || (int)task.PriorityId >= Enum.GetValues(typeof(TaskPriorityLevel)).Length))
            {
                throw new TaskServiceException(400, "Provided priority Id for task is invalid");
            }

            await using var t = await _unitOfWork.CreateTransaction();

            try
            {
                // Check if uid is valid or not
                ApplicationUser validUser = _userManager.Users.FirstOrDefault(e => e.UserId == createdByUserId);
                if (validUser == null)
                {
                    throw new TaskServiceException(404, "Cannot locate a valid user from the claim provided");
                }

                // Check if its associated project is valid
                var parent = from project in _unitOfWork.Repository<Project>().GetDbset()
                                 where project.Id == task.ProjectId
                                 select project;
                if (parent == null || parent.Count() < 1)
                {
                    throw new TaskServiceException(404, "Cannot find a single instance of the parent project from the infos you provided");
                }
                if (parent.Count() > 1)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Inconsistency in database. Executing query returns more than one result: ");
                    sb.AppendLine(parent.ToList().ToString());
                    throw new Exception(sb.ToString());
                }

                // Check if its parent task is valid
                if (task.ParentId != null)
                {
                    var parentTask = from Task in _unitOfWork.Repository<Task>().GetDbset()
                                 where Task.Id == task.ParentId
                                 select Task;
                    if (parentTask == null || parentTask.Count() < 1)
                    {
                        throw new TaskServiceException(404, "Cannot find a single instance of the parent task from the infos you provided");
                    }
                    if (parentTask.Count() > 1)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("Inconsistency in database. Executing query returns more than one result: ");
                        sb.AppendLine(parent.ToList().ToString());
                        throw new Exception(sb.ToString());
                    }
                }

                // Check if its assignedBy person is in the Db
                ApplicationUser assignedByUser = _userManager.Users.FirstOrDefault(e => e.UserId == task.AssignedBy);
                if (validUser == null)
                {
                    throw new TaskServiceException(404, "Cannot locate a valid user for assignedBy field from the data provided");
                }

                // Check if its assignedFor person is in the Db
                ApplicationUser assignedForUser = _userManager.Users.FirstOrDefault(e => e.UserId == task.AssignedFor);
                if (validUser == null)
                {
                    throw new TaskServiceException(404, "Cannot locate a valid user for assignedFor field from the data provided");
                }

                // Add task in first
                Tasks newTask = new Tasks()
                {
                    Name = task.Name,
                    Schedule = task.Schedule,
                    ScheduleString = task.ScheduleString,
                    PriorityId = task.PriorityId == 0 ? null : task.PriorityId,
                    ProjectId = task.ProjectId,
                    ParentId = task.ParentId,
                    Reminder = task.Reminder,
                    ReminderSchedule = task.ReminderSchedule,
                    AssignedBy = task.AssignedBy,
                    AssignedFor = task.AssignedFor,
                    CreatedBy = validUser.UserId,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    Deleted = false,
                };  
                await _unitOfWork.Repository<Tasks>().InsertAsync(newTask);
                await _unitOfWork.SaveChangesAsync();

                // Eager load instance
                var entry = _unitOfWork.Entry(newTask);
                entry.Reference(e => e.Project).Load();
                entry.Reference(e => e.Priority).Load();
                entry.Reference(e => e.Parent).Load();

                await t.CommitAsync();

                return new TaskResponseModel(newTask);
            }
            catch (Exception ex)
            {
                await t.RollbackAsync();
                _logger.LogError(ex, "An error occurred while using TaskService");
                throw ex;
            }
        }

        public async Task<IEnumerable<TaskResponseModel>> GetAllTasks(GetAllTasksModel model)
        {
            if (model.UserId == null) throw new TaskServiceException(400, "Cannot find tasks of this project if you don't provide a UserID for us to check if you have access rights");

            await using var t = await _unitOfWork.CreateTransaction();

            try
            {
                // Check if uid is valid or not
                ApplicationUser validUser = _userManager.Users.FirstOrDefault(e => e.UserId == model.UserId);
                if (validUser == null)
                {
                    throw new TaskServiceException(404, "Cannot locate a valid user from the claim provided");
                }

                // Query for all the tasks in projects that the user participated in that matches the queries
                var result = from userProjects in _unitOfWork.Repository<UserProjects>().GetDbset()
                             join tasks in _unitOfWork.Repository<Tasks>().GetDbset() on userProjects.ProjectId equals tasks.ProjectId
                             where userProjects.UserId == model.UserId
                             select tasks;

                // Run original statement through additional queries
                if(model.ProjectId != null)
                {
                    result.Where(e => e.ProjectId == model.ProjectId);
                }

                if(model.CategoryType != null)
                {
                    switch (model.CategoryType)
                    {
                        case (byte)MenuSidebarOptions.Today:
                            result.Where(e => e.Schedule.HasValue && e.Schedule.Value == DateTime.Today); break;
                        case (byte)MenuSidebarOptions.Upcoming:
                            result.Where(e => e.Schedule.HasValue && e.Schedule.Value > DateTime.Today); break;
                        default:
                            result.Where(e => e != null); break;
                    }
                }

                // Finally, convert the result by selecting in the form of response (eager load everything)
                var finalResult = result.Include(e => e.Project)
                    .Include(e => e.Priority)
                    .Include(e => e.Parent)
                    .Select(e => new TaskResponseModel(e));

                await _unitOfWork.SaveChangesAsync();

                return finalResult.ToList();
            }
            catch (Exception ex)
            {
                await t.RollbackAsync();
                _logger.LogError(ex, "An error occurred when using ProjectService");
                throw ex;
            }
        }

        public async Task<TaskResponseModel> GetOneTask(long taskId)
        {
            throw new NotImplementedException();
        }

        public async Task<TaskResponseModel> UpdateTaskInfo(long taskId, long updatedByUserId, UpdateTaskInfoModel model)
        {
            throw new NotImplementedException();
        }

        public async Task<TaskResponseModel> SoftDeleteExistingTask(long taskId, long deletedByUserId)
        {
            throw new NotImplementedException();
        }
    }
}
