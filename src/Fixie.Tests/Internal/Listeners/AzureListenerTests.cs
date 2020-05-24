﻿namespace Fixie.Tests.Internal.Listeners
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using Assertions;
    using Fixie.Internal.Listeners;
    using static Fixie.Internal.Serialization;

    public class AzureListenerTests : MessagingTests
    {
        class Request<TContent>
        {
            public Request(HttpMethod method, string uri, TContent content)
            {
                Method = method;
                Uri = uri;
                Content = content;
            }

            public HttpMethod Method { get; }
            public string Uri { get; }
            public TContent Content { get; }
        }

        public void ShouldReportResultsToAzureDevOpsApi()
        {
            var project = Guid.NewGuid().ToString();
            var accessToken = Guid.NewGuid().ToString();
            var buildId = Guid.NewGuid().ToString();
            var runUrl = "http://localhost:4567/run/" + Guid.NewGuid();
            var requests = new List<object>();
            var batchSize = 2;

            Action<HttpClient> assertCommonHttpConcerns = client =>
            {
                var actualHeader = client.DefaultRequestHeaders.Accept.Single();
                actualHeader.MediaType.ShouldBe("application/json");

                var actualAuthorization = client.DefaultRequestHeaders.Authorization;
                actualAuthorization.Scheme.ShouldBe("Bearer");
                actualAuthorization.Parameter.ShouldBe(accessToken);
            };

            var listener = new AzureListener("http://localhost:4567", project, accessToken, buildId,
                (client, method, uri, content) =>
                {
                    assertCommonHttpConcerns(client);
                    requests.Add(new Request<AzureListener.CreateRun>(method, uri, content));
                    return Serialize(new AzureListener.TestRun {url = runUrl});
                },
                (client, method, uri, content) =>
                {
                    assertCommonHttpConcerns(client);
                    requests.Add(new Request<IReadOnlyList<AzureListener.Result>>(method, uri, content));
                    return "";
                },
                (client, method, uri, content) =>
                {
                    assertCommonHttpConcerns(client);
                    requests.Add(new Request<AzureListener.CompleteRun>(method, uri, content));
                    return "";
                }, batchSize);

            Run(listener, out var console);

            console
                .ShouldBe(
                    "Console.Out: Fail",
                    "Console.Error: Fail",
                    "Console.Out: FailByAssertion",
                    "Console.Error: FailByAssertion",
                    "Console.Out: Pass",
                    "Console.Error: Pass");

            var firstRequest = (Request<AzureListener.CreateRun>)requests.First();
            firstRequest.Method.ShouldBe(HttpMethod.Post);
            firstRequest.Uri.ShouldBe($"http://localhost:4567/{project}/_apis/test/runs?api-version=5.0");

            var createRun = firstRequest.Content;
            createRun.name.ShouldBe("Fixie.Tests (.NETCoreApp,Version=v3.1)");
            createRun.build.id.ShouldBe(buildId);
            createRun.isAutomated.ShouldBe(true);

            var resultBatches = requests
                .Skip(1)
                .Take(requests.Count - 2)
                .Cast<Request<IReadOnlyList<AzureListener.Result>>>()
                .Select(request =>
                {
                    request.Method.ShouldBe(HttpMethod.Post);
                    request.Uri.ShouldBe($"{runUrl}/results?api-version=5.0");

                    return request.Content;
                }).ToList();

            resultBatches.Count.ShouldBe(3);
            resultBatches[0].Count.ShouldBe(2);
            resultBatches[1].Count.ShouldBe(2);
            resultBatches[2].Count.ShouldBe(1);

            var results = resultBatches.SelectMany(x => x).ToList();
            results.Count.ShouldBe(5);

            var fail = results[0];
            var failByAssertion = results[1];
            var pass = results[2];
            var skipWithReason = results[3];
            var skipWithoutReason = results[4];

            skipWithReason.automatedTestName.ShouldBe(TestClass + ".SkipWithReason");
            skipWithReason.testCaseTitle.ShouldBe(TestClass + ".SkipWithReason");
            skipWithReason.outcome.ShouldBe("Warning");
            skipWithReason.durationInMs.ShouldBeGreaterThanOrEqualTo(0);
            skipWithReason.errorMessage.ShouldBe("⚠ Skipped with reason.");
            skipWithReason.stackTrace.ShouldBe(null);

            skipWithoutReason.automatedTestName.ShouldBe(TestClass + ".SkipWithoutReason");
            skipWithoutReason.testCaseTitle.ShouldBe(TestClass + ".SkipWithoutReason");
            skipWithoutReason.outcome.ShouldBe("Warning");
            skipWithoutReason.durationInMs.ShouldBeGreaterThanOrEqualTo(0);
            skipWithoutReason.errorMessage.ShouldBe(null);
            skipWithoutReason.stackTrace.ShouldBe(null);

            fail.automatedTestName.ShouldBe(TestClass + ".Fail");
            fail.testCaseTitle.ShouldBe(TestClass + ".Fail");
            fail.outcome.ShouldBe("Failed");
            fail.durationInMs.ShouldBeGreaterThanOrEqualTo(0);
            fail.errorMessage.ShouldBe("'Fail' failed!");
            fail.stackTrace!
                .Lines()
                .CleanStackTraceLineNumbers()
                .ShouldBe("Fixie.Tests.FailureException", At("Fail()"));

            failByAssertion.automatedTestName.ShouldBe(TestClass + ".FailByAssertion");
            failByAssertion.testCaseTitle.ShouldBe(TestClass + ".FailByAssertion");
            failByAssertion.outcome.ShouldBe("Failed");
            failByAssertion.durationInMs.ShouldBeGreaterThanOrEqualTo(0);
            failByAssertion.errorMessage!.Lines().ShouldBe(
                "Expected: 2",
                "Actual:   1");
            failByAssertion.stackTrace!
                .Lines()
                .CleanStackTraceLineNumbers()
                .ShouldBe("Fixie.Tests.Assertions.AssertException", At("FailByAssertion()"));

            pass.automatedTestName.ShouldBe(TestClass + ".Pass");
            pass.testCaseTitle.ShouldBe(TestClass + ".Pass");
            pass.outcome.ShouldBe("Passed");
            pass.durationInMs.ShouldBeGreaterThanOrEqualTo(0);
            pass.errorMessage.ShouldBe(null);
            pass.stackTrace.ShouldBe(null);

            var lastRequest = (Request<AzureListener.CompleteRun>)requests.Last();
            lastRequest.Method.ShouldBe(new HttpMethod("PATCH"));
            lastRequest.Uri.ShouldBe($"{runUrl}?api-version=5.0");

            var updateRun = lastRequest.Content;
            updateRun.state.ShouldBe("Completed");
        }
    }
}