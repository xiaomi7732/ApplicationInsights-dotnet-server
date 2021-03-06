﻿namespace FuncTest
{
    using System;    
    using System.Linq;
    using AI;
    using FuncTest.Helpers;
    using FuncTest.Serialization;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Diagnostics;

    /// <summary>
    /// Tests Dependency Collector Functionality for a WebApplication written in Dotnet Core.
    /// </summary>
    [TestClass]
    public class AspNetCoreHttpTests
    {
        internal const string AspxCoreAppFolder = ".\\AspxCore";
        internal static DotNetCoreTestWebApplication AspxCoreTestWebApplication { get; private set; }

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            AspxCoreTestWebApplication = new DotNetCoreTestWebApplication
            {
                AppName = "AspxCore",
                ExternalCallPath = "external/calls",
                Port = DeploymentAndValidationTools.AspxCorePort,
            };

            AspxCoreTestWebApplication.Deploy();
            DeploymentAndValidationTools.Initialize();

            Trace.TraceInformation("Aspnet core HttpTests class initialized");
        }

        [ClassCleanup]
        public static void MyClassCleanup()
        {            
            DeploymentAndValidationTools.CleanUp();
            Trace.TraceInformation("Aspnet core HttpTests class cleaned up");
            AspxCoreTestWebApplication.Remove();

        }

        [TestInitialize]
        public void MyTestInitialize()
        {
            DeploymentAndValidationTools.SdkEventListener.Start();
        }

        [TestCleanup]
        public void MyTestCleanup()
        {
            Assert.IsFalse(DeploymentAndValidationTools.SdkEventListener.FailureDetected, "Failure is detected. Please read test output logs.");
            DeploymentAndValidationTools.SdkEventListener.Stop();
        }               

        private static void EnsureDotNetCoreInstalled()
        {
            string output = "";
            string error = "";

            if (!DotNetCoreProcess.HasDotNetExe())
            {
                Assert.Inconclusive(".Net Core is not installed");
            }
            else
            {
                DotNetCoreProcess process = new DotNetCoreProcess("--version")
                    .RedirectStandardOutputTo((string outputMessage) => output += outputMessage)
                    .RedirectStandardErrorTo((string errorMessage) => error += errorMessage)
                    .Run();

                if (process.ExitCode.Value != 0 || !string.IsNullOrEmpty(error))
                {
                    Assert.Inconclusive(".Net Core is not installed");
                }
                else
                {
                    // Look for first dash to get semantic version. (for example: 1.0.0-preview2-003156)
                    int dashIndex = output.IndexOf('-');
                    Version version = new Version(dashIndex == -1 ? output : output.Substring(0, dashIndex));

                    Version minVersion = new Version("1.0.0");
                    if (version < minVersion)
                    {
                        Assert.Inconclusive($".Net Core version ({output}) must be greater than or equal to {minVersion}.");
                    }
                }
            }
        }

        private static IDisposable DotNetCoreTestSetup()
        {
            EnsureDotNetCoreInstalled();

            return new ExpectedSDKPrefixChanger("rddd");
        }

        private const string AspxCoreTestAppFolder = "..\\TestApps\\AspxCore\\";

        #region Tests

        [TestMethod]
        [TestCategory(TestCategory.NetCore)]
        [DeploymentItem(AspxCoreTestAppFolder, AspxCoreAppFolder)]
        public void TestRddForSyncHttpAspxCore()
        {
            using (DotNetCoreTestSetup())
            {
                // Execute and verify calls which succeeds            
                HttpTestHelper.ExecuteSyncHttpTests(AspxCoreTestWebApplication, true, 1, HttpTestConstants.AccessTimeMaxHttpNormal, "200", HttpTestConstants.QueryStringOutboundHttp);
            }
        }

        [TestMethod]
        [TestCategory(TestCategory.NetCore)]
        [DeploymentItem(AspxCoreTestAppFolder, AspxCoreAppFolder)]
        public void TestRddForSyncHttpPostCallAspxCore()
        {
            using (DotNetCoreTestSetup())
            {
                // Execute and verify calls which succeeds            
                HttpTestHelper.ExecuteSyncHttpPostTests(AspxCoreTestWebApplication, true, 1, HttpTestConstants.AccessTimeMaxHttpNormal, "200", HttpTestConstants.QueryStringOutboundHttpPost);
            }
        }

        [TestMethod]
        [Ignore] // Don't run this test until .NET Core writes diagnostic events for failed HTTP requests
        [TestCategory(TestCategory.NetCore)]
        [DeploymentItem(AspxCoreTestAppFolder, AspxCoreAppFolder)]
        public void TestRddForSyncHttpFailedAspxCore()
        {
            using (DotNetCoreTestSetup())
            {
                // Execute and verify calls which fails.            
                HttpTestHelper.ExecuteSyncHttpTests(AspxCoreTestWebApplication, false, 1, HttpTestConstants.AccessTimeMaxHttpInitial, "200", HttpTestConstants.QueryStringOutboundHttpFailed);
            }
        }

        #endregion        

        private class ExpectedSDKPrefixChanger : IDisposable
        {
            private readonly string previousExpectedSDKPrefix;

            public ExpectedSDKPrefixChanger(string expectedSDKPrefix)
            {
                previousExpectedSDKPrefix = DeploymentAndValidationTools.ExpectedSDKPrefix;
                DeploymentAndValidationTools.ExpectedSDKPrefix = expectedSDKPrefix;
            }

            public void Dispose()
            {
                DeploymentAndValidationTools.ExpectedSDKPrefix = previousExpectedSDKPrefix;
            }
        }
    }
}