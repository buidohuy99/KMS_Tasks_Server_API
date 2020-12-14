﻿using Core.Application.Helper;
using Core.Application.Helper.Exceptions.Task;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Task;
using Core.Domain.DbEntities;
using Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApi.Controllers.v1
{
    public class TaskController : BaseController
    {
        private ITaskService _taskService;
        private ILogger<TaskController> _logger;

        public TaskController(ITaskService taskService, ILogger<TaskController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        [HttpPost("task")]
        public async Task<IActionResult> AddNewTask([FromBody] NewTaskModel newTask)
        {
            try
            {
                // Check validity of the request
                var claimsManager = HttpContext.User;
                if (!claimsManager.HasClaim(c => c.Type == "uid"))
                {
                    return Unauthorized(new Response<object>(false, null, "Token provided is invalid because there is no valid confidential claim"));
                }
                // Extract uid from token
                long uid;
                try
                {
                    uid = long.Parse(claimsManager.Claims.FirstOrDefault(c => c.Type == "uid").Value);
                }
                catch (Exception)
                {
                    return Unauthorized(new Response<object>(false, null, "Token provided is invalid because the value for the claim is invalid"));
                }

                // Carry on with the business logic
                TaskResponseModel addedTask = await _taskService.AddNewTask(uid, newTask);
                return Ok(new Response<TaskResponseModel>(true, addedTask, message: "Successfully added task"));
            }
            catch (Exception ex)
            {
                if (ex is TaskServiceException exception)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("A problem occurred when processing the content of your request, please recheck your request params: ");
                    sb.AppendLine(exception.Message);
                    return StatusCode(exception.StatusCode, new Response<object>(false, null, sb.ToString()));
                }
                return StatusCode(500, new Response<Exception>(false, ex, "Server encountered an exception"));
            }
        }

        [HttpGet("tasks")]
        public async Task<IActionResult> GetAllTasks([FromQuery] GetAllTasksModel model)
        {
            try
            {
                //Check validity of the token
                if (model.UserId != null)
                {
                    return BadRequest(new Response<object>(false, null, "Found illegal parameter UserId in query, we refuse to carry on with your request"));
                }
                var claimsManager = HttpContext.User;
                if (!claimsManager.HasClaim(c => c.Type == "uid"))
                {
                    return Unauthorized(new Response<object>(false, null, "Token provided is invalid because there is no valid confidential claim"));
                }
                // Extract uid from token
                long uid;
                try
                {
                    uid = long.Parse(claimsManager.Claims.FirstOrDefault(c => c.Type == "uid").Value);
                }
                catch (Exception)
                {
                    return Unauthorized(new Response<object>(false, null, "Token provided is invalid because the value for the claim is invalid"));
                }

                // If passes all tests, then we submit it to the service layer
                model.UserId = uid;
                // Carry on with the business logic
                IEnumerable<TaskResponseModel> tasks = await _taskService.GetAllTasks(model);
                return Ok(new Response<IEnumerable<TaskResponseModel>>(true, tasks, message: "Successfully fetched tasks of user"));
            }
            catch (Exception ex)
            {
                if (ex is TaskServiceException exception)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("A problem occurred when processing the content of your request, please recheck your request params: ");
                    sb.AppendLine(exception.Message);
                    return StatusCode(exception.StatusCode, new Response<object>(false, null, sb.ToString()));
                }
                return StatusCode(500, new Response<Exception>(false, ex, "Server encountered an exception"));
            }
        }

        [HttpGet("task/{taskId}")]
        public async Task<IActionResult> GetAParticularTask(int taskId)
        {
            throw new NotImplementedException();
        }

        [HttpPatch("task/{taskId}")]
        public async Task<IActionResult> UpdateAnExistingTask(int taskId, [FromBody] UpdateTaskInfoModel model)
        {
            throw new NotImplementedException();
        }

        [HttpDelete("task/{taskId}")]
        public async Task<IActionResult> DeleteExistingTask(int taskId)
        {
            throw new NotImplementedException();
        }
    }
}
