using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MainMenuScope : LifetimeScope
{
    [SerializeField] private MainMenuView mainMenuView;
    [SerializeField] private JoinPanelView joinPanelView;
    
    protected override void Configure(IContainerBuilder builder)
    {
        // Register identity service FIRST (Singleton across scenes)
        builder.RegisterComponent(mainMenuView);
        builder.RegisterComponent(joinPanelView);

        builder.Register<PlayerIdentityService>(Lifetime.Singleton);
        builder.Register<LobbyService>(Lifetime.Singleton);
        builder.Register<RelayService>(Lifetime.Singleton);

        builder.Register<ServicesInitializer>(Lifetime.Singleton);
        builder.RegisterEntryPoint<ServicesInitializer>(Lifetime.Singleton);        
    }
    
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }
}