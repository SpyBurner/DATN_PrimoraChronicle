using Zenject;

public class AccountInstaller : MonoInstaller
{
        public override void InstallBindings()
        {
                // Login
                Container.BindInterfacesAndSelfTo<AccountLoginModel>()
                        .AsSingle().NonLazy();
                Container.BindInterfacesAndSelfTo<AccountLoginController>()
                        .AsSingle().NonLazy();
                Container.BindInterfacesAndSelfTo<AccountLoginSubsystem>()
                        .AsSingle().NonLazy();

                // Register
                Container.BindInterfacesAndSelfTo<AccountRegisterModel>()
                        .AsSingle().NonLazy();
                Container.BindInterfacesAndSelfTo<AccountRegisterController>()
                        .AsSingle().NonLazy();
                Container.BindInterfacesAndSelfTo<AccountRegisterSubsystem>()
                        .AsSingle().NonLazy();
        }
}
