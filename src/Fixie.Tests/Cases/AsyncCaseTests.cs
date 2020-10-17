﻿namespace Fixie.Tests.Cases
{
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Assertions;
    using static System.Environment;
    using static Utility;

    public class AsyncCaseTests
    {
        public async Task ShouldAwaitThenPassUponSuccessfulAsyncExecution()
        {
            (await Run<AwaitThenPassTestClass>())
                .ShouldBe(
                    For<AwaitThenPassTestClass>(".Test passed"));
        }

        public async Task ShouldAwaitResultThenPassUponSuccessfulAsyncExecution()
        {
            (await Run<AwaitResultThenPassTestClass>())
                .ShouldBe(
                    For<AwaitResultThenPassTestClass>(".Test passed"));
        }

        public async Task ShouldCompleteTaskThenPassUponSuccessfulTaskExecution()
        {
            (await Run<CompleteTaskThenPassTestClass>())
                .ShouldBe(
                    For<CompleteTaskThenPassTestClass>(".Test passed"));
        }

        public async Task ShouldGetTaskResultThenPassUponSuccessfulTaskExecution()
        {
            (await Run<CompleteTaskWithResultThenPassTestClass>())
                .ShouldBe(
                    For<CompleteTaskWithResultThenPassTestClass>(".Test passed"));
        }

        public async Task ShouldPassForNullTask()
        {
            (await Run<NullTaskTestClass>())
                .ShouldBe(
                    For<NullTaskTestClass>(".Test passed"));
        }

        public async Task ShouldFailWithOriginalExceptionWhenAsyncCaseMethodThrowsAfterAwaiting()
        {
            (await Run<AwaitThenFailTestClass>())
                .ShouldBe(
                    For<AwaitThenFailTestClass>(
                        ".Test failed: Expected: 0" + NewLine +
                        "Actual:   3"));
        }

        public async Task ShouldFailWithOriginalExceptionWhenAsyncCaseMethodThrowsWithinTheAwaitedTask()
        {
            (await Run<AwaitOnTaskThatThrowsTestClass>())
                .ShouldBe(
                    For<AwaitOnTaskThatThrowsTestClass>(".Test failed: Attempted to divide by zero."));
        }

        public async Task ShouldFailWithOriginalExceptionWhenAsyncCaseMethodThrowsBeforeAwaitingOnAnyTask()
        {
            (await Run<FailBeforeAwaitTestClass>())
                .ShouldBe(
                    For<FailBeforeAwaitTestClass>(".Test failed: 'Test' failed!"));
        }

        public async Task ShouldFailWithClearExplanationWhenAsyncCaseMethodReturnsNonStartedTask()
        {
            (await Run<FailDueToNonStartedTaskTestClass>())
                .ShouldBe(
                    For<FailDueToNonStartedTaskTestClass>(
                        ".Test failed: The test returned a non-started task, which cannot " +
                        "be awaited. Consider using Task.Run or Task.Factory.StartNew."));
        }

        public async Task ShouldExecuteReturnedTaskDeclaredAsObject()
        {
            (await Run<CompleteTaskDeclaredAsObjectTestClass>())
                .ShouldBe(
                    For<CompleteTaskDeclaredAsObjectTestClass>(
                        ".Test failed: Expected: 0" + NewLine +
                        "Actual:   3"));
        }

        public async Task ShouldFailUnsupportedAsyncVoidCases()
        {
            (await Run<UnsupportedAsyncVoidTestTestClass>())
                .ShouldBe(
                    For<UnsupportedAsyncVoidTestTestClass>(".Test failed: " +
                        "Async void methods are not supported. Declare async methods with a return type of " +
                        "Task to ensure the task actually runs to completion."));
        }

        abstract class SampleTestClassBase
        {
            protected static void ThrowException([CallerMemberName] string member = default!)
            {
                throw new FailureException(member);
            }

            protected static Task<int> Divide(int numerator, int denominator)
            {
                return Task.Run(() => numerator/denominator);
            }
        }

        class AwaitThenPassTestClass : SampleTestClassBase
        {
            public async Task Test()
            {
                var result = await Divide(15, 5);

                result.ShouldBe(3);
            }
        }

        class AwaitResultThenPassTestClass : SampleTestClassBase
        {
            public async Task<bool> Test()
            {
                var result = await Divide(15, 5);

                result.ShouldBe(3);

                return true;
            }
        }

        class CompleteTaskThenPassTestClass : SampleTestClassBase
        {
            public Task Test()
            {
                var divide = Divide(15, 5);

                return divide.ContinueWith(division =>
                {
                    division.Result.ShouldBe(3);
                });
            }
        }

        class CompleteTaskWithResultThenPassTestClass : SampleTestClassBase
        {
            public Task<bool> Test()
            {
                var divide = Divide(15, 5);

                return divide.ContinueWith(division =>
                {
                    division.Result.ShouldBe(3);

                    return true;
                });
            }
        }

        class NullTaskTestClass
        {
            public Task? Test()
            {
                // Although unlikely, we must ensure that
                // we don't attempt to wait on a Task that
                // is in fact null.
                return null;
            }
        }

        class AwaitThenFailTestClass : SampleTestClassBase
        {
            public async Task Test()
            {
                var result = await Divide(15, 5);

                result.ShouldBe(0);
            }
        }

        class AwaitOnTaskThatThrowsTestClass : SampleTestClassBase
        {
            public async Task Test()
            {
                await Divide(15, 0);

                throw new ShouldBeUnreachableException();
            }
        }

        class FailBeforeAwaitTestClass : SampleTestClassBase
        {
            public async Task Test()
            {
                ThrowException();

                await Divide(15, 5);
            }
        }

        class FailDueToNonStartedTaskTestClass
        {
            public Task Test()
            {
                return new Task(() => throw new ShouldBeUnreachableException());
            }
        }

        class CompleteTaskDeclaredAsObjectTestClass : SampleTestClassBase
        {
            public object Test()
            {
                var divide = Divide(15, 5);

                return divide.ContinueWith(division =>
                {
                    // Fail within the continuation, so that we can prove
                    // that the task object was fully executed.
                    division.Result.ShouldBe(0);
                });
            }
        }

        class UnsupportedAsyncVoidTestTestClass : SampleTestClassBase
        {
            public async void Test()
            {
                await Divide(15, 5);

                throw new ShouldBeUnreachableException();
            }
        }
    }
}