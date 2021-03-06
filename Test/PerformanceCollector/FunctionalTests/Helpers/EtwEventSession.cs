﻿namespace Functional.Helpers
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.Diagnostics.Tracing;
    using Microsoft.Diagnostics.Tracing.Session;

    public class EtwEventSession : IDisposable
    {
        private readonly string[] providers =
        {
            "Microsoft-ApplicationInsights-Extensibility-Web", 
            "Microsoft-ApplicationInsights-Extensibility-DependencyCollector", 
            "Microsoft-ApplicationInsights-Core",
            "Microsoft-ApplicationInsights-Extensibility-AppMapCorrelation",
        };

        private const string SessionName = "RequestTelemetryFunctionalTest";

        private TraceEventSession session;

        public void Start()
        {
            if (!(TraceEventSession.IsElevated() ?? false))
            {
                Trace.WriteLine(
                    "WARNING! To turn on ETW events you need to be Administrator, please run from an Admin process.");
                return;
            }

            // Same session name is reused to prevent multiple orphaned sessions in case if dispose is not done when test stopped in debug
            // Important! Note that session can leave longer that the process and it is important to dispose it
            session = new TraceEventSession(SessionName, null);
            foreach (var provider in this.providers)
            {
                this.session.EnableProvider(provider);
            }
            this.session.StopOnDispose = true;

            this.session.Source.Dynamic.All += Process;
            this.session.Source.UnhandledEvents += Process;

            Task.Run(() =>
            {
                // Blocking call. Will end when session is disposed
                this.session.Source.Process();
            });

            this.FailureDetected = false;
            Trace.WriteLine("Etw session started");
        }

        public bool FailureDetected { get; set; }

        public void Stop()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            if (this.session != null)
            {
                this.session.Dispose();
                this.session = null;
            }

            Trace.WriteLine("Etw session stopped");
        }

        private void Process(TraceEvent data)
        {
            Trace.WriteLine(string.Format(
                "Application Trace. Level: {0}; Id: {1}; Message: {2}; ",
                data.Level,
                data.ID,
                data.FormattedMessage));

            this.TestAppDomainNameIsTheLastPayloadParameter(data);
            
            this.TestTraceLevelIsNotErrorOrCritical(data);            
        }

        private void TestAppDomainNameIsTheLastPayloadParameter(TraceEvent data)
        {
            if (data.PayloadNames.Length > 0)
            {
                int id = (int) data.ID;

                // Not system event
                if ((id > 0) && (id < 65534))
                {
                    string domainName = data.PayloadString(data.PayloadNames.Length - 1);
                    bool correctName = TraceAssert.IsTrue(
                        domainName.StartsWith("/LM/W3SVC"),
                        "Every message must have application name as the last parameter to enable StatusMonitor integration: " +
                        domainName);

                    this.FailureDetected = !correctName || this.FailureDetected;
                }
            }
            else
            {
                TraceAssert.IsTrue(false, "Trace must have at least 1 parameter - appDomain name");
                this.FailureDetected = true;
            }
        }

        private void TestTraceLevelIsNotErrorOrCritical(TraceEvent data)
        {
            this.FailureDetected =
                !TraceAssert.AreNotEqual(data.Level, TraceEventLevel.Error, data.FormattedMessage)
                || this.FailureDetected;
            this.FailureDetected =
                !TraceAssert.AreNotEqual(data.Level, TraceEventLevel.Critical, data.FormattedMessage)
                || this.FailureDetected;
        }

        private static class TraceAssert
        {
            public static bool AreEqual<T>(T expected, T actual, string message) where T : IComparable
            {
                if (actual.CompareTo(expected) != 0)
                {
                    Trace(expected, actual, message);
                    return false;
                }

                return true;
            }

            public static bool AreNotEqual<T>(T expected, T actual, string message) where T : IComparable
            {
                if (actual.CompareTo(expected) == 0)
                {
                    Trace(expected, actual, message);
                    return false;
                }

                return true;
            }

            public static bool IsTrue(bool condition, string message)
            {
                if (!condition)
                {
                    System.Diagnostics.Trace.WriteLine("ERROR! " + message);
                    return false;
                }

                return true;
            }

            private static void Trace<T>(T expected, T actual, string message) where T : IComparable
            {
                string assert = string.Format("Expected: {0}; Actual: {1}; Message: {2}", expected, actual, message);
                System.Diagnostics.Trace.WriteLine("ERROR! " + assert);
            }
        }
    }
}