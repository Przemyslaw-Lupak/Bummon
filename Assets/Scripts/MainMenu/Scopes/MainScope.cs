using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MainScope : LifetimeScope
{
    [SerializeField] private MainMenuView mainMenuView;
    [SerializeField] private JoinPanelView joinPanelView;
    
    
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(mainMenuView);
        builder.RegisterComponent(joinPanelView);

        builder.Register<PlayerIdentityService>(Lifetime.Singleton);
        builder.Register<LobbyService>(Lifetime.Singleton);
        builder.Register<RelayService>(Lifetime.Singleton);
        builder.Register<NetworkPlayerService>(Lifetime.Singleton);

        builder.Register<ServicesInitializer>(Lifetime.Singleton);
        builder.RegisterEntryPoint<ServicesInitializer>(Lifetime.Singleton);        
    }
    
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }
}