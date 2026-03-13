using System.Threading;
using UnityEngine;
using UnityObservables;

public interface ISceneLoaderModel : IModel
{
    Observable<bool> IsLoading { get; }
    Observable<AsyncOperation> CurrentLoad { get; }
    Observable<CancellationTokenSource> SceneToken { get; }
}