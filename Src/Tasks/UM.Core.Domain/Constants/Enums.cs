﻿using System;
using System.Collections.Generic;
using System.Text;

namespace UM.Core.Domain.Constants
{
    public class Enums
    {
        public enum UserRoles
        {
            Administrator = 0,
            Moderator = 1,
            User = 2
        }

        public enum ProjectRoles : int
        {
            None = 0,
            Owner = 1,
            PM = 2,
            Leader = 3,
            QA = 4,
            Dev = 5,
            BA = 6,
            MeUMer = 7
        }

        public enum MenuSidebarOptions : byte
        {
            Dashboard = 0,
            Today = 1,
            Upcoming = 2
        }

        public enum LoginProvider : byte
        {
            System = 0,
            Facebook = 1,
            Google = 2,
        }

        public enum TaskPriorityLevel : int
        {
            None = 0,
            Emergency = 1,
            High = 2,
            Medium = 3,
            Low = 4,
            Anytime = 5
        }
    }
}
