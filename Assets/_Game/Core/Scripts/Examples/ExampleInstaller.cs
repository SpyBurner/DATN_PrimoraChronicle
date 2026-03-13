using Zenject;

public class ExampleInstaller: MonoInstaller
{
    public override void InstallBindings()
    {
        // ... existing bindings ...

        // Example subsystem bindings
        Container.BindInterfacesAndSelfTo<ExampleModel>().AsSingle();
        Container.BindInterfacesAndSelfTo<ExampleController>().AsSingle();
        Container.BindInterfacesAndSelfTo<ExampleSubsystem>().AsSingle();
    }
}