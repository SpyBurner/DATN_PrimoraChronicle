using System;
using System.Threading.Tasks;
using System.Threading;
using NUnit.Framework;
using Zenject;
using UnityEngine;
using UnityEngine.Events;

[TestFixture]
public class AccountLoginSubsystemTests : ZenjectUnitTestFixture
{
    private AccountLoginModel _model;
    private AccountLoginController _controller;
    private AccountLoginSubsystem _subsystem;

    private MockLogger _logger;
    private MockHttpService _http;
    private MockAuthSession _auth;
    private MockSceneLoader _scene;

    [SetUp]
    public void CommonInstall()
    {
        _logger = new MockLogger();
        _http = new MockHttpService();
        _auth = new MockAuthSession();
        _scene = new MockSceneLoader();

        Container.Bind<IDebugLogger>().FromInstance(_logger);
        Container.Bind<IHttpServiceSubsystem>().FromInstance(_http);
        Container.Bind<IAuthSessionSubsystem>().FromInstance(_auth);
        Container.Bind<ISceneLoaderSubsystem>().FromInstance(_scene);

        Container.Bind<IAccountLoginModel>().To<AccountLoginModel>().AsSingle();
        Container.Bind<IAccountLoginController>().To<AccountLoginController>().AsSingle();
        Container.Bind<IAccountLoginSubsystem>().To<AccountLoginSubsystem>().AsSingle();

        _model = Container.Resolve<AccountLoginModel>();
        _controller = Container.Resolve<AccountLoginController>();
        _subsystem = Container.Resolve<AccountLoginSubsystem>();
    }

    [Test]
    public async Task Login_WithValidCredentials_CallsHttpAndLoadsLobby()
    {
        // Arrange
        _subsystem.SetEmail("test@example.com");
        _subsystem.SetPassword("password123");
        
        _http.SuccessResponse = new LoginResponse { 
            token = "fake-token", 
            user = new UserDataResponse { ID = "user-1", username = "testuser" } 
        };

        // Act
        await _subsystem.Login();

        // Assert
        Assert.AreEqual("fake-token", _auth.LastToken);
        Assert.AreEqual("Lobby", _scene.LastLoadedScene);
        Assert.IsTrue(string.IsNullOrEmpty(_model.ErrorMessage.Value));
    }

    [Test]
    public async Task Login_WithInvalidCredentials_SetsErrorMessage()
    {
        // Arrange
        _subsystem.SetEmail("wrong@example.com");
        _subsystem.SetPassword("wrong");
        
        _http.SuccessResponse = null; // Simulate failure

        // Act
        await _subsystem.Login();

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(_model.ErrorMessage.Value));
        Assert.AreEqual("Invalid email or password.", _model.ErrorMessage.Value);
    }

    // --- Mocks ---

    private class MockLogger : IDebugLogger
    {
        public void Log(string m) => Debug.Log(m);
        public void LogError(string m) => Debug.LogError(m);
        public void LogWarning(string m) => Debug.LogWarning(m);
        public void SetLoggingEnabled(bool isEnabled) { }
    }

    private class MockHttpService : IHttpServiceSubsystem
    {
        public object SuccessResponse;
        public event UnityAction<int> RequestQueueCountChanged;
        public event UnityAction<bool> IsRequestingChanged;

        public Task<T> Get<T>(string url) => Task.FromResult((T)SuccessResponse);
        public Task<T> Post<T, TRequest>(string url, TRequest payload) where TRequest : class => Task.FromResult((T)SuccessResponse);
        public Task<string> Get(string url) => Task.FromResult(SuccessResponse?.ToString() ?? string.Empty);
        public Task<string> Post<TRequest>(string url, TRequest payload) where TRequest : class => Task.FromResult(SuccessResponse?.ToString() ?? string.Empty);
        public void SetAuthToken(string token) { }
        
        public void Initialize() { }
        public void Dispose() { }
    }

    private class MockAuthSession : IAuthSessionSubsystem
    {
        public string LastToken;
        public event UnityAction<string> CurrentUserIdChanged;
        public event UnityAction<string> AuthTokenChanged;
        public event UnityAction<bool> IsLoggedInChanged;

        public Task StoreSession(string userId, string authToken) { LastToken = authToken; return Task.CompletedTask; }
        public Task ClearSession() => Task.CompletedTask;
        public Task LoadPersistedSession() => Task.CompletedTask;
        public void Initialize() { }
        public void Dispose() { }
    }

    private class MockSceneLoader : ISceneLoaderSubsystem
    {
        public string LastLoadedScene;
        public event UnityAction<bool> IsLoadingChanged;
        public event UnityAction<AsyncOperation> CurrentLoadChanged;
        public event UnityAction<CancellationTokenSource> SceneTokenChanged;

        public Task LoadScene(string name) { LastLoadedScene = name; return Task.CompletedTask; }
        public Task ReloadScene() => Task.CompletedTask;
        public void Initialize() { }
        public void Dispose() { }
    }
}
