using System;
using MessagePack;
using Sirenix.OdinInspector;

namespace VoxelsEngine.Data {
    [Serializable, MessagePackObject(true)]
    public class FrameTextureJson {
        [ValueDropdown("@AssetsHelper.GetFrameTextures()")]
        public string FrameAlbedoTexture = null!;

        [ValueDropdown("@AssetsHelper.GetFrameTextures()")]
        public string FrameNormalsTexture = null!;

        [ValueDropdown("@AssetsHelper.GetFrameTextures()")]
        public string FrameHeightsTexture = null!;
    }
}