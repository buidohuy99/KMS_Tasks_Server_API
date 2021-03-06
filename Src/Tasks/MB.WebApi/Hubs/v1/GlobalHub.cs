﻿using MB.Core.Application.Interfaces.Misc;
using MB.Core.Domain.DbEntities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MB.WebApi.Hubs.v1
{
    public class GlobalHub : Hub
    {
        private ILogger<GlobalHub> _logger;
        private IConnectionManager _connectionManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public GlobalHub(ILogger<GlobalHub> logger, IConnectionManager connectionManager, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _userManager = userManager;
        }

        public override Task OnConnectedAsync()
        {
            _connectionManager.RegisterConnection(Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            _connectionManager.RemoveConnection(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        public async void Login(long uid)
        {
            // Check validity of the request
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User{uid}Group");
           _connectionManager.AddConnectionToRoom(Context.ConnectionId, $"User{uid}Group");
        }

        public void Logout(long uid)
        {
            _connectionManager.ClearRoomsOfConnection(Context.ConnectionId);
        }

        public async void RegisterViewProject(long projectId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Project{projectId}Group");
            _connectionManager.AddConnectionToRoom(Context.ConnectionId, $"Project{projectId}Group");
        }

        public async void RemoveFromViewingProject(long projectId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Project{projectId}Group");
            _connectionManager.RemoveConnectionFromRoom(Context.ConnectionId, $"Project{projectId}Group");
        }
    }
}
