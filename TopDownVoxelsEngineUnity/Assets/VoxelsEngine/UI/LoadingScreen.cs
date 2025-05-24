using DG.Tweening;
using LoneStoneStudio.Tools;
using Sirenix.OdinInspector;
using TinkState;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VoxelsEngine;

public class LoadingScreen : MonoBehaviour {
    [Required]
    public RectTransform Container = null!;

    [Required]
    public CanvasGroup CanvasGroup = null!;

    [Required]
    public TextMeshProUGUI StageText = null!;

    [Required]
    public TextMeshProUGUI progressText = null!;

    [Required]
    public Image progressBar = null!;

    [Required]
    public ClientMain ClientMain = null!;

    private void Start() {
        gameObject.DisposeOnDestroy(
            Observable.AutoRun(OnProgressChanged)
        );
    }

    private void OnProgressChanged() {
        var loadingStage = ClientMain.CurrentLoadingStage.Value;
        var progressValue = ClientMain.LoadingProgress.Value;

        StageText.text = GetStageDescription(loadingStage);
        progressBar.fillAmount = progressValue;
        progressText.text = $"{(int) (progressBar.fillAmount * 100)}%";

        var isActive = loadingStage is not (LoadingStage.NotStarted or LoadingStage.Complete);
        if (isActive) {
            DOTween.Kill(CanvasGroup);
            CanvasGroup.alpha = 1;
            Container.SmartActive(true);
        } else {
            DOTween.Kill(CanvasGroup);
            CanvasGroup
                .DOFade(0, 1f)
                .SetTarget(CanvasGroup)
                .OnComplete(() => Container.SmartActive(false));
        }
    }

    private string GetStageDescription(LoadingStage stage) {
        return stage switch {
            LoadingStage.Initializing => "Initializing game…",
            LoadingStage.NotStarted => "Welcome!",
            LoadingStage.LocalCheckingSaveFile => "Checking local save…",
            LoadingStage.LocalLoadingGameState => "Loading local save…",
            LoadingStage.LocalCreatingGameState => "Creating local save…",
            LoadingStage.ClientConnectingToServer => "Connecting to server…",
            LoadingStage.ClientAuthenticatingPlayer => "Authenticating on server…",
            LoadingStage.LocalGeneratingChunks => "Generate initial chunks on local save…",
            LoadingStage.LocalCreatingCharacter => "Creating local character…",
            LoadingStage.UploadingToGPU => "Updading data to GPU…",
            LoadingStage.EnteringGame => "Entering game…",
            LoadingStage.Complete => "Enjoy!",
            _ => stage.ToString()
        };
    }
}