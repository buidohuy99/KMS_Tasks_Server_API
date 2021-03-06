﻿using MB.Core.Application.Helper;
using MB.Core.Application.Helper.Exceptions.User;
using MB.Core.Application.Interfaces;
using MB.Core.Application.Models.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;
using System.Threading.Tasks;
using MB.WebApi.Controllers.v1.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using MB.Core.Domain.DbEntities;
using Microsoft.AspNetCore.SignalR;
using MB.WebApi.Hubs.v1;
using MB.Core.Application.Interfaces.Misc;
using System.Collections.Generic;

namespace MB.WebApi.Controllers.v1
{
    [Area("user-management")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UserController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IHubContext<GlobalHub> _hubContext;

        public UserController(IUserService userService, UserManager<ApplicationUser> userManager, IHubContext<GlobalHub> hubContext) : base(userManager)
        {
            _userService = userService;
            _hubContext = hubContext;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                //Check validity of the token
                var claimsManager = HttpContext.User;
                long? uid = null;
                try
                {
                    uid = GetUserId(claimsManager);
                }
                catch (Exception e)
                {
                    return Unauthorized(e.Message);
                }

                if (!uid.HasValue)
                {
                    return Unauthorized("Unauthorized individuals cannot access this route");
                }

                // If passes all tests, then we submit it to the service layer
                // Carry on with the business logic
                UserResponseModel profile = await _userService.GetUserInfoById(uid.Value);
                return Ok(new HttpResponse<UserResponseModel>(true, profile, message: "Successfully fetched infos of user"));
            }
            catch (Exception ex)
            {
                if (ex is UserServiceException exception)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("A problem occurred when processing the content of your request, please recheck your request params: ");
                    sb.AppendLine(exception.Message);
                    uint? statusCode = ServiceExceptionsProcessor.GetStatusCode(exception.Message);
                    if (statusCode != null && statusCode.HasValue)
                    {
                        return StatusCode((int)statusCode.Value, new HttpResponse<object>(false, null, sb.ToString()));
                    }
                }
                return StatusCode(500, new HttpResponse<Exception>(false, ex, "Server encountered an exception"));
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsersByFields([FromQuery] FindUserByFieldsModel model)
        {
            try
            {
                //Check validity of the token
                var claimsManager = HttpContext.User;
                long? uid = null;
                try
                {
                    uid = GetUserId(claimsManager);
                }
                catch (Exception e)
                {
                    return Unauthorized(e.Message);
                }

                if (!uid.HasValue)
                {
                    return Unauthorized("Unauthorized individuals cannot access this route");
                }

                // If passes all tests, then we submit it to the service layer
                // Carry on with the business logic
                var result = await _userService.GetUserInfoByFields(model);
                return Ok(new HttpResponse<IEnumerable<UserResponseModel>>(true, result, message: "Successfully found users from input"));
            }
            catch (Exception ex)
            {
                if (ex is UserServiceException exception)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("A problem occurred when processing the content of your request, please recheck your request params: ");
                    sb.AppendLine(exception.Message);
                    uint? statusCode = ServiceExceptionsProcessor.GetStatusCode(exception.Message);
                    if (statusCode != null && statusCode.HasValue)
                    {
                        return StatusCode((int)statusCode.Value, new HttpResponse<object>(false, null, sb.ToString()));
                    }
                }
                return StatusCode(500, new HttpResponse<Exception>(false, ex, "Server encountered an exception"));
            }
        }

        [HttpPatch("profile")]
        public async Task<IActionResult> UpdateExistingUser(UpdateUserInfoModel model)
        {
            try
            {
                //Check validity of the token
                var claimsManager = HttpContext.User;
                long? uid = null;
                try
                {
                    uid = GetUserId(claimsManager);
                }
                catch (Exception e)
                {
                    return Unauthorized(e.Message);
                }

                if (!uid.HasValue)
                {
                    return Unauthorized("Unauthorized individuals cannot access this route");
                }

                // If passes all tests, then we submit it to the service layer
                // Carry on with the business logic
                UserResponseModel updatedUser = await _userService.UpdateUserInfo(uid.Value, model);

                await _hubContext.Clients.Group($"User{updatedUser.Id}Group").SendAsync("profile-info-changed", updatedUser);

                return Ok(new HttpResponse<UserResponseModel>(true, updatedUser, message: "Successfully patched infos of user"));
            }
            catch (Exception ex)
            {
                if (ex is UserServiceException exception)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("A problem occurred when processing the content of your request, please recheck your request params: ");
                    sb.AppendLine(exception.Message);
                    uint? statusCode = ServiceExceptionsProcessor.GetStatusCode(exception.Message);
                    if (statusCode != null && statusCode.HasValue)
                    {
                        return StatusCode((int)statusCode.Value, new HttpResponse<object>(false, null, sb.ToString(), exception.IdentityErrors));
                    }
                }
                return StatusCode(500, new HttpResponse<Exception>(false, ex, "Server encountered an exception"));
            }
        }
    }
}
