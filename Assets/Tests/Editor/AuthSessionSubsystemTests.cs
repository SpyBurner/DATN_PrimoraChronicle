using NUnit.Framework;
using Zenject;
using System.Threading.Tasks;

namespace Core.Tests
{
    [TestFixture]
    public class AuthSessionSubsystemTests : SubsystemTestBase
    {
        [SetUp]
        public override void CommonInstall()
        {
            base.CommonInstall();
            
            // Bind dependencies for AuthSession
            Container.BindInterfacesAndSelfTo<AuthSessionModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<AuthSessionController>().AsSingle();
            Container.BindInterfacesAndSelfTo<AuthSessionSubsystem>().AsSingle();
            
            // Mock HttpService dependency
            Container.Bind<IHttpServiceSubsystem>().To<MockHttpServiceSubsystem>().AsSingle();
        }

        [Test]
        public async Task TestStoreSession_UpdatesModel()
        {
            var subsystem = Container.Resolve<IAuthSessionSubsystem>();
            var model = Container.Resolve<IAuthSessionModel>();

            await subsystem.StoreSession("test_user", "test_token");

            Assert.AreEqual("test_user", model.CurrentUserId.Value);
            Assert.AreEqual("test_token", model.AuthToken.Value);
            Assert.IsTrue(model.IsLoggedIn.Value);
        }

        [Test]
        public async Task TestClearSession_ResetsModel()
        {
            var subsystem = Container.Resolve<IAuthSessionSubsystem>();
            var model = Container.Resolve<IAuthSessionModel>();

            await subsystem.StoreSession("user", "token");
            await subsystem.ClearSession();

            Assert.AreEqual("", model.CurrentUserId.Value);
            Assert.AreEqual("", model.AuthToken.Value);
            Assert.IsFalse(model.IsLoggedIn.Value);
        }
    }
}
