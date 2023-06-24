using System;
using LoneStoneStudio.Tools;
using Shared;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VoxelsEngine.UI {
    public class ConnectionModal : MonoBehaviour {
        [Required]
        public ClientMain Main = null!;

        [Required]
        public Button PlayButton = null!;

        [Required]
        public TMP_InputField NameInputField = null!;

        private void Awake() {
            PlayButton.onClick.RemoveAllListeners();
            // ReSharper disable once AsyncVoidLambda - Try catch OK
            PlayButton.onClick.AddListener(async () => {
                try {
                    if (String.IsNullOrEmpty(NameInputField.text)) return;
                    LocalState.Instance.CurrentPlayerName = NameInputField.text;
                    await Main.StartRemotePlay();
                    this.SmartActive(false);
                } catch (Exception e) {
                    Logr.LogException(e);
                }
            });
        }
    }
}