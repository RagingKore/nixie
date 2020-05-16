﻿namespace Fixie.Tests.TestAdapter
{
    using System;
    using System.Collections.Generic;
    using Fixie.TestAdapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Assertions;
    using Fixie.Internal;
    using Fixie.Internal.Listeners;
    using static System.Environment;

    public class ExecutionRecorderTests
    {
        public void ShouldMapMessagesToVsTestExecutionRecorder()
        {
            const string assemblyPath = "assembly.path.dll";
            var recorder = new StubExecutionRecorder();

            var executionRecorder = new ExecutionRecorder(recorder, assemblyPath);

            var @case = Case("Pass", 1);
            executionRecorder.Record(new PipeMessage.CaseStarted
            (
                new CaseStarted(@case)
            ));
            @case.Duration = TimeSpan.FromSeconds(1);
            @case.Output = "Output";
            executionRecorder.Record(new PipeMessage.CasePassed
            (
                new CasePassed(@case)
            ));

            @case = Case("Fail");
            executionRecorder.Record(new PipeMessage.CaseStarted
            (
                new CaseStarted(@case)
            ));
            @case.Duration = TimeSpan.FromSeconds(2);
            @case.Output = "Output";
            @case.Fail(new StubException("Exception Message"));
            executionRecorder.Record(new PipeMessage.CaseFailed
            (
                new CaseFailed(@case)
            ));

            @case = Case("Skip");
            executionRecorder.Record(new PipeMessage.CaseStarted
            (
                new CaseStarted(@case)
            ));
            @case.Skip("Skip Reason");
            executionRecorder.Record(new PipeMessage.CaseSkipped
            (
                new CaseSkipped(@case)
            ));

            var className = typeof(SampleTestClass).FullName;

            var starts = recorder.TestStarts;
            starts.Count.ShouldBe(3);
            starts[0].ShouldBeExecutionTimeTest(className+".Pass", assemblyPath);
            starts[1].ShouldBeExecutionTimeTest(className+".Fail", assemblyPath);
            starts[2].ShouldBeExecutionTimeTest(className+".Skip", assemblyPath);

            var results = recorder.TestResults;
            results.Count.ShouldBe(3);

            foreach (var result in results)
            {
                result.Traits.ShouldBeEmpty();
                result.Attachments.ShouldBeEmpty();
                result.ComputerName.ShouldBe(MachineName);
            }

            var pass = results[0];
            var fail = results[1];
            var skip = results[2];

            pass.TestCase.ShouldBeExecutionTimeTest(className+".Pass", assemblyPath);
            pass.TestCase.DisplayName.ShouldBe(className+".Pass");
            pass.Outcome.ShouldBe(TestOutcome.Passed);
            pass.ErrorMessage.ShouldBe(null);
            pass.ErrorStackTrace.ShouldBe(null);
            pass.DisplayName.ShouldBe(className+".Pass(1)");
            pass.Messages.Count.ShouldBe(1);
            pass.Messages[0].Category.ShouldBe(TestResultMessage.StandardOutCategory);
            pass.Messages[0].Text.ShouldBe("Output");
            pass.Duration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);

            fail.TestCase.ShouldBeExecutionTimeTest(className+".Fail", assemblyPath);
            fail.TestCase.DisplayName.ShouldBe(className+".Fail");
            fail.Outcome.ShouldBe(TestOutcome.Failed);
            fail.ErrorMessage.ShouldBe("Exception Message");
            fail.ErrorStackTrace.ShouldBe(typeof(StubException).FullName + NewLine + "Exception Stack Trace");
            fail.DisplayName.ShouldBe(className+".Fail");
            fail.Messages.Count.ShouldBe(1);
            fail.Messages[0].Category.ShouldBe(TestResultMessage.StandardOutCategory);
            fail.Messages[0].Text.ShouldBe("Output");
            fail.Duration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);

            skip.TestCase.ShouldBeExecutionTimeTest(className+".Skip", assemblyPath);
            skip.TestCase.DisplayName.ShouldBe(className+".Skip");
            skip.Outcome.ShouldBe(TestOutcome.Skipped);
            skip.ErrorMessage.ShouldBe("Skip Reason");
            skip.ErrorStackTrace.ShouldBe(null);
            skip.DisplayName.ShouldBe(className+".Skip");
            skip.Messages.ShouldBeEmpty();
            skip.Duration.ShouldBe(TimeSpan.Zero);
        }

        static Case Case(string methodName, params object?[] parameters)
            => new Case(typeof(SampleTestClass).GetInstanceMethod(methodName), parameters);

        class SampleTestClass
        {
            public void Pass(int x) { }
            public void Fail() { }
            public void Skip() { }
        }

        class StubException : Exception
        {
            public StubException(string message)
                : base(message) { }

            public override string StackTrace
                => "Exception Stack Trace";
        }

        class StubExecutionRecorder : ITestExecutionRecorder
        {
            public List<TestCase> TestStarts { get; } = new List<TestCase>();
            public List<TestResult> TestResults { get; } = new List<TestResult>();

            public void RecordResult(TestResult testResult)
                => TestResults.Add(testResult);

            public void SendMessage(TestMessageLevel testMessageLevel, string message)
                => throw new NotImplementedException();

            public void RecordStart(TestCase testCase)
                => TestStarts.Add(testCase);

            public void RecordEnd(TestCase testCase, TestOutcome outcome)
                => throw new NotImplementedException();

            public void RecordAttachments(IList<AttachmentSet> attachmentSets)
                => throw new NotImplementedException();
        }
    }
}