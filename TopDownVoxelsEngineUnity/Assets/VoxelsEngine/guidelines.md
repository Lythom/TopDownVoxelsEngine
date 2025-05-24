# Coding guidelines

- Unity scripts extends the default MonoBehaviour.
- References should be formatted as public fields, use the odin [Required] attribute, and have a default null! value to dismiss error since Odin ensure the reference is provided.

``` 
[Required]
public TextMeshProUGUI StageText = null!;
```

- public properties are CamelCased.
- private properties are _camelCased.
- When appropriate, fields can use TinkStateSharp observable containers to have the value be reactive.
- Example of TinkStateSharp observable state :

```cs
// with a private state and public readyonly observable
private State<LoadingStage> _currentLoadingStage = Observable.State(LoadingStage.NotStarted);
public Observable<LoadingStage> CurrentLoadingStage => _currentLoadingStage;

// with a public state
public State<float> LoadingProgress = Observable.State(0f);

// Use example in some other script or same script
private void Start() {
    gameObject.DisposeOnDestroy(
        Observable.AutoRun(() => {
            StageText.text = GetStageDescription(CurrentLoadingStage.Value);
            progressBar.fillAmount = LoadingProgress.Value;
            progressText.text = $"{(int) (progressBar.fillAmount * 100)}%";
        })
    );
}
```