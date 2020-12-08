﻿using Core.Domain.Constants;
using System;
using System.Collections.Generic;

namespace Core.Domain.DbEntities
{
    public partial class PriorityLevel
    {
        public Enums.TaskPriorityLevel Id { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
    }
}
