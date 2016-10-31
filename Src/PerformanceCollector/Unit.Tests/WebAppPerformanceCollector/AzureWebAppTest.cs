﻿namespace Unit.Tests
{
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class UnitTestAzureWeb
    {
        [TestMethod]
        public void TestPerformanceCounterValuesAreCorrectlyRetrievedUsingPerformanceCounterFromJsonGauge()
        {
            PerformanceCounterFromJsonGauge gauge = new PerformanceCounterFromJsonGauge(@"\Process(??APP_WIN32_PROC??)\Private Bytes", "privateBytes", AzureWebApEnvironmentVariables.App, new CacheHelperTests());
            float value = gauge.GetValueAndReset();

            Assert.IsTrue(value > 0);
        }
    }
}