using System;
using MessagePack;
using Sirenix.OdinInspector;

namespace VoxelsEngine.Data {
    [Serializable, MessagePackObject(true)]
    public class MainTextureJson {
        
        [ValueDropdown("@AssetsHelper.GetMainTextures()")]
        public string MainAlbedoTexture = null!;

        [ValueDropdown("@AssetsHelper.GetMainTextures()")]
        public string MainNormalsTexture = null!;

        [ValueDropdown("@AssetsHelper.GetMainTextures()")]
        public string MainHeightsTexture = null!;
    }
}