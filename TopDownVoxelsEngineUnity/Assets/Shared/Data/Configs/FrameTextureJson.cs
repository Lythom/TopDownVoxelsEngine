using System;
using MessagePack;
using Sirenix.OdinInspector;

namespace VoxelsEngine.Data {
    [Serializable, MessagePackObject(true)]
    public class FrameTextureJson {
        public string FrameAlbedoTexture = null!;
        public string FrameNormalsTexture = null!;
        public string FrameHeightsTexture = null!;
    }
}