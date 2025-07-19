using Zenject;

public class GameInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        InputActions inputActions = new InputActions();
        inputActions.Enable();
        Container.Bind<InputActions>().FromInstance(inputActions).AsSingle();
    }
}