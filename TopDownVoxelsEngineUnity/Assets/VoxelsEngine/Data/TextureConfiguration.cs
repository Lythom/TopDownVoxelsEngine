using System;
using MessagePack;
using Sirenix.OdinInspector;

namespace VoxelsEngine.Data {
    [Serializable, MessagePackObject(true)]
    public class MainTextureConfiguration {
        
        [ValueDropdown("@AssetsHelper.GetMainTextures()")]
        public string MainAlbedoTexture = null!;

        [ValueDropdown("@AssetsHelper.GetMainTextures()")]
        public string MainNormalsTexture = null!;

        [ValueDropdown("@AssetsHelper.GetMainTextures()")]
        public string MainHeightsTexture = null!;
    }

    [Serializable, MessagePackObject(true)]
    public class FrameTextureConfiguration {
        [ValueDropdown("@AssetsHelper.GetFrameTextures()")]
        public string FrameAlbedoTexture = null!;

        [ValueDropdown("@AssetsHelper.GetFrameTextures()")]
        public string FrameNormalsTexture = null!;

        [ValueDropdown("@AssetsHelper.GetFrameTextures()")]
        public string FrameHeightsTexture = null!;
    }
}