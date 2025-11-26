using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MainMenuScope : LifetimeScope
{
    [SerializeField] private MainMenuView mainMenuView;
    [SerializeField] private JoinPanelView joinPanelView;
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(mainMenuView);
        builder.RegisterComponent(joinPanelView);
        builder.Register<LobbyService>(Lifetime.Singleton);
        
        // Register RelayService as a singleton
        builder.Register<RelayService>(Lifetime.Singleton);
        
        // Register ServicesInitializer as a singleton
        builder.Register<ServicesInitializer>(Lifetime.Singleton);

        builder.RegisterEntryPoint<ServicesInitializer>();
    }
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }
}
