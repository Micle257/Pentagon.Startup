﻿// -----------------------------------------------------------------------
//  <copyright file="ExecutionOptions.cs">
//   Copyright (c) Michal Pokorný. All Rights Reserved.
//  </copyright>
// -----------------------------------------------------------------------

namespace Pentagon.Extensions.Startup
{
    public class ExecutionOptions
    {
        public AppExecutionType ExecutionType { get; set; }

        public int LoopWaitMilliseconds { get; set; }

        public int? TerminateValue { get; set; }
    }
}