﻿namespace Fixie.Tests
{
    using static System.Environment;

    class TestProject : ITestProject
    {
        public void Configure(TestConfiguration configuration, TestEnvironment environment)
        {
            if (GetEnvironmentVariable("GITHUB_ACTIONS") == null)
                configuration.Reports.Add<DiffToolReport>();
        }
    }
}
