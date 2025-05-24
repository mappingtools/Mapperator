using osu.Framework.Testing;

namespace Mapperator.DemoApp.Game.Tests.Visual
{
    public partial class DemoAppTestScene : TestScene
    {
        protected override ITestSceneTestRunner CreateRunner() => new DemoAppTestSceneTestRunner();

        private partial class DemoAppTestSceneTestRunner : DemoAppGameBase, ITestSceneTestRunner
        {
            private TestSceneTestRunner.TestRunner runner;

            protected override void LoadAsyncComplete()
            {
                base.LoadAsyncComplete();
                Add(runner = new TestSceneTestRunner.TestRunner());
            }

            public void RunTestBlocking(TestScene test) => runner.RunTestBlocking(test);
        }
    }
}
