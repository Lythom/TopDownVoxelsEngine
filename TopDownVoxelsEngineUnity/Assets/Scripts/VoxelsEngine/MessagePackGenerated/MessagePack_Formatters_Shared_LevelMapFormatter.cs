// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY MPC(MessagePack-CSharp). DO NOT CHANGE IT.
// </auto-generated>

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168
#pragma warning disable CS1591 // document public APIs

#pragma warning disable SA1129 // Do not use default value type constructor
#pragma warning disable SA1309 // Field names should not begin with underscore
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.Shared
{
    public sealed class LevelMapFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Shared.LevelMap>
    {
        // Chunks
        private static global::System.ReadOnlySpan<byte> GetSpan_Chunks() => new byte[1 + 6] { 166, 67, 104, 117, 110, 107, 115 };
        // GenerationQueue
        private static global::System.ReadOnlySpan<byte> GetSpan_GenerationQueue() => new byte[1 + 15] { 175, 71, 101, 110, 101, 114, 97, 116, 105, 111, 110, 81, 117, 101, 117, 101 };
        // SaveId
        private static global::System.ReadOnlySpan<byte> GetSpan_SaveId() => new byte[1 + 6] { 166, 83, 97, 118, 101, 73, 100 };
        // LevelId
        private static global::System.ReadOnlySpan<byte> GetSpan_LevelId() => new byte[1 + 7] { 167, 76, 101, 118, 101, 108, 73, 100 };

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::Shared.LevelMap value, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            var formatterResolver = options.Resolver;
            writer.WriteMapHeader(4);
            writer.WriteRaw(GetSpan_Chunks());
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::Shared.Chunk[,]>(formatterResolver).Serialize(ref writer, value.Chunks, options);
            writer.WriteRaw(GetSpan_GenerationQueue());
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::System.Collections.Concurrent.ConcurrentQueue<int>>(formatterResolver).Serialize(ref writer, value.GenerationQueue, options);
            writer.WriteRaw(GetSpan_SaveId());
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Serialize(ref writer, value.SaveId, options);
            writer.WriteRaw(GetSpan_LevelId());
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Serialize(ref writer, value.LevelId, options);
        }

        public global::Shared.LevelMap Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            var formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var __GenerationQueue__IsInitialized = false;
            var __GenerationQueue__ = default(global::System.Collections.Concurrent.ConcurrentQueue<int>);
            var __SaveId__ = default(string);
            var __LevelId__ = default(string);

            for (int i = 0; i < length; i++)
            {
                var stringKey = global::MessagePack.Internal.CodeGenHelpers.ReadStringSpan(ref reader);
                switch (stringKey.Length)
                {
                    default:
                    FAIL:
                      reader.Skip();
                      continue;
                    case 6:
                        switch (global::MessagePack.Internal.AutomataKeyGen.GetKey(ref stringKey))
                        {
                            default: goto FAIL;
                            case 126905251883075UL:
                                reader.Skip();
                                continue;
                            case 110266397647187UL:
                                __SaveId__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Deserialize(ref reader, options);
                                continue;
                        }
                    case 15:
                        if (!global::System.MemoryExtensions.SequenceEqual(stringKey, GetSpan_GenerationQueue().Slice(1))) { goto FAIL; }

                        __GenerationQueue__IsInitialized = true;
                        __GenerationQueue__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::System.Collections.Concurrent.ConcurrentQueue<int>>(formatterResolver).Deserialize(ref reader, options);
                        continue;
                    case 7:
                        if (global::MessagePack.Internal.AutomataKeyGen.GetKey(ref stringKey) != 28228227578619212UL) { goto FAIL; }

                        __LevelId__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Deserialize(ref reader, options);
                        continue;

                }
            }

            var ____result = new global::Shared.LevelMap(__SaveId__, __LevelId__);
            if (__GenerationQueue__IsInitialized)
            {
                ____result.GenerationQueue = __GenerationQueue__;
            }

            reader.Depth--;
            return ____result;
        }
    }

}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1129 // Do not use default value type constructor
#pragma warning restore SA1309 // Field names should not begin with underscore
#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name