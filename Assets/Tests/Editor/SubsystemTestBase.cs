using Zenject;
using NUnit.Framework;

namespace Core.Tests
{
    public abstract class SubsystemTestBase : ZenjectUnitTestFixture
    {
        [SetUp]
        public virtual void CommonInstall()
        {
            // Bind core dependencies that many subsystems need
            Container.BindInterfacesAndSelfTo<DebugLogger>().AsSingle();
        }
    }
}
