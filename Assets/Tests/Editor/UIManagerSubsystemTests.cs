using NUnit.Framework;
using Zenject;
using UnityEngine;
using Core;
using System.Threading.Tasks;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;

namespace Core.Tests
{
    [TestFixture]
    public class UIManagerSubsystemTests : SubsystemTestBase
    {
        private UIMappingSO _uiMapping;

        [SetUp]
        public override void CommonInstall()
        {
            base.CommonInstall();
            
            // Create and bind UIMappingSO
            _uiMapping = ScriptableObject.CreateInstance<UIMappingSO>();
            Container.BindInstance(_uiMapping).AsSingle();
            
            // Registry for scene-specific containers
            Container.Bind<SceneContextRegistry>().AsSingle();
            
            // Bind UIManager stack
            Container.BindInterfacesAndSelfTo<UIManagerModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<UIManagerController>().AsSingle();
            Container.BindInterfacesAndSelfTo<UIManagerSubsystem>().AsSingle();
        }

        [Test]
        public void TestRegisterPanel_IncrementsCount()
        {
            var subsystem = Container.Resolve<IUIManagerSubsystem>();
            var go = new GameObject("MockPanel");
            var panel = go.AddComponent<MockPanel>();

            subsystem.RegisterPanel(panel);

            Assert.AreEqual(1, subsystem.TotalPanelCount);
            
            // Cleanup
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TestUnregisterPanel_DecrementsCount()
        {
            var subsystem = Container.Resolve<IUIManagerSubsystem>();
            var go = new GameObject("MockPanel");
            var panel = go.AddComponent<MockPanel>();

            subsystem.RegisterPanel(panel);
            subsystem.UnregisterPanel(panel);

            Assert.AreEqual(0, subsystem.TotalPanelCount);
            
            // Cleanup
            Object.DestroyImmediate(go);
        }
        
        [Test]
        public async Task TestCloseLastPanel_TriggersDefaultScreen()
        {
            // Expect the "Destroy may not be called from edit mode" error because UIManagerController calls Destroy
            LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
            // Expect the "No default prefab found" because our dummy SO has no mappings for the test scene
            LogAssert.Expect(LogType.Error, new Regex("UIMappingSO: No default prefab found for scene name"));

            var subsystem = Container.Resolve<IUIManagerSubsystem>();
            var go = new GameObject("MockPanel");
            var panel = go.AddComponent<MockPanel>();

            // We need a UIRoot for the UIManager to function
            var uiRootGo = new GameObject("UIRoot");
            var uiRoot = uiRootGo.AddComponent<UIRoot>();
            subsystem.RegisterUIRoot(uiRoot);

            subsystem.RegisterPanel(panel);
            
            // Closing the panel should trigger ShowDefaultScreenForScene
            await subsystem.Close(panel);
            
            Assert.AreEqual(0, subsystem.TotalPanelCount);
            
            // Cleanup
            Object.DestroyImmediate(uiRootGo);
            // go is already destroyed by subsystem.Close(panel) but might still be around in some state
            if (go != null) Object.DestroyImmediate(go);
        }
    }
}
