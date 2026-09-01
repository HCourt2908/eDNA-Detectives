using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace EDNA.Investigation.Editor
{
    public static class InvestigationTestRunner
    {
        private static TestRunnerApi activeApi;
        private static TestCallbacks activeCallbacks;

        [MenuItem("eDNA Detectives/Validation/Run EditMode Tests")]
        public static void RunEditModeTests()
        {
            Run(TestMode.EditMode, "EDNA.Investigation.EditModeTests", true);
        }

        [MenuItem("eDNA Detectives/Validation/Run PlayMode Tests")]
        public static void RunPlayModeTests()
        {
            Run(TestMode.PlayMode, "EDNA.Investigation.PlayModeTests", false);
        }

        private static void Run(TestMode mode, string assemblyName, bool runSynchronously)
        {
            activeApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            activeCallbacks = new TestCallbacks(mode, activeApi);
            activeApi.RegisterCallbacks(activeCallbacks);
            activeApi.Execute(
                new ExecutionSettings(
                    new Filter
                    {
                        testMode = mode,
                        assemblyNames = new[] { assemblyName }
                    })
                {
                    runSynchronously = runSynchronously
                });
        }

        private sealed class TestCallbacks : ICallbacks
        {
            private readonly TestMode mode;
            private readonly TestRunnerApi api;

            public TestCallbacks(TestMode mode, TestRunnerApi api)
            {
                this.mode = mode;
                this.api = api;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log($"INVESTIGATION_TESTS_STARTED mode={mode} count={testsToRun.TestCaseCount}");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                Debug.Log(
                    $"INVESTIGATION_TESTS_FINISHED mode={mode} passed={result.PassCount} failed={result.FailCount} skipped={result.SkipCount} inconclusive={result.InconclusiveCount}");
                api.UnregisterCallbacks(this);
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus == TestStatus.Failed)
                {
                    Debug.LogError($"INVESTIGATION_TEST_FAILED mode={mode} name={result.FullName}\n{result.Message}\n{result.StackTrace}");
                }
            }
        }
    }
}
