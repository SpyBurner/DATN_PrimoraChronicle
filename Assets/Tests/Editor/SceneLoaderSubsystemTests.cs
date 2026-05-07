using NUnit.Framework;
using Zenject;
using Core;
using System.Threading.Tasks;

namespace Core.Tests
{
    [TestFixture]
    public class SceneLoaderSubsystemTests : SubsystemTestBase
    {
        [SetUp]
        public override void CommonInstall()
        {
            base.CommonInstall();
            
            Container.BindInterfacesAndSelfTo<SceneLoaderModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<SceneLoaderController>().AsSingle();
            Container.BindInterfacesAndSelfTo<SceneLoaderSubsystem>().AsSingle();
            
            // SceneLoader depends on UIManager to show loading screen
            Container.Bind<IUIManagerSubsystem>().To<MockUIManagerSubsystem>().AsSingle();
        }

        [Test]
        public void TestLoadingState_DefaultsToFalse()
        {
            var model = Container.Resolve<ISceneLoaderModel>();
            Assert.IsFalse(model.IsLoading.Value);
        }

        [Test]
        public void TestCurrentLoad_DefaultsToNull()
        {
            var model = Container.Resolve<ISceneLoaderModel>();
            Assert.IsNull(model.CurrentLoad.Value);
        }
    }
}
