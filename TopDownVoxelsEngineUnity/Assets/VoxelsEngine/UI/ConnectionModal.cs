using System;
using LoneStoneStudio.Tools;
using Shared;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VoxelsEngine.UI {
    public class ConnectionModal : MonoBehaviour {
        public static ConnectionModal Instance = null!;

        [Required]
        public ClientMain Main = null!;

        [Required]
        public Button PlayButton = null!;

        [Required]
        public Button PlayOfflineButton = null!;

        [Required]
        public TMP_InputField NameInputField = null!;

        private void Awake() {
            Instance = this;
            PlayButton.onClick.RemoveAllListeners();
            // ReSharper disable once AsyncVoidLambda - Try catch OK
            PlayButton.onClick.AddListener(async () => {
                try {
                    if (String.IsNullOrEmpty(NameInputField.text)) return;
                    this.SmartActive(false);
                    LocalState.Instance.CurrentPlayerName = NameInputField.text;
                    await Main.StartRemotePlay();
                } catch (Exception e) {
                    Logr.LogException(e);
                    this.SmartActive(true);
                }
            });

            PlayOfflineButton.onClick.RemoveAllListeners();
            // ReSharper disable once AsyncVoidLambda - Try catch OK
            PlayOfflineButton.onClick.AddListener(async () => {
                try {
                    this.SmartActive(false);
                    await Main.StartLocalPlay();
                } catch (Exception e) {
                    Logr.LogException(e);
                    this.SmartActive(true);
                }
            });

            // try {
            //
            //     Character c = new Character("Test", Vector3.one, "LevelId");
            //     var p = new CharacterJoinGameEvent(0, 0, c, Vector3.one);
            //     var s = MessagePackSerializer.Serialize((INetworkMessage) p);
            //     Logr.Log(MessagePackSerializer.ConvertToJson(s));
            //     var c2 = MessagePackSerializer.Deserialize<INetworkMessage>(s);
            //     Logr.Log(c2.GetType().Name);
            // } catch (Exception e) {
            //     Logr.LogException(e, "Failed to serialize/deserialize character");
            // }
        }
    }
}