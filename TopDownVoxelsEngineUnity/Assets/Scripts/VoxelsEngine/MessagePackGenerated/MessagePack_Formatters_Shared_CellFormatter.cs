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
    public sealed class CellFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Shared.Cell>
    {
        // Block
        private static global::System.ReadOnlySpan<byte> GetSpan_Block() => new byte[1 + 5] { 165, 66, 108, 111, 99, 107 };
        // DamageLevel
        private static global::System.ReadOnlySpan<byte> GetSpan_DamageLevel() => new byte[1 + 11] { 171, 68, 97, 109, 97, 103, 101, 76, 101, 118, 101, 108 };

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::Shared.Cell value, global::MessagePack.MessagePackSerializerOptions options)
        {
            var formatterResolver = options.Resolver;
            writer.WriteMapHeader(2);
            writer.WriteRaw(GetSpan_Block());
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::Shared.BlockId>(formatterResolver).Serialize(ref writer, value.Block, options);
            writer.WriteRaw(GetSpan_DamageLevel());
            writer.Write(value.DamageLevel);
        }

        public global::Shared.Cell Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                throw new global::System.InvalidOperationException("typecode is null, struct not supported");
            }

            options.Security.DepthStep(ref reader);
            var formatterResolver = options.Resolver;
            var length = reader.ReadMapHeader();
            var __Block__ = default(global::Shared.BlockId);
            var __DamageLevel__ = default(byte);

            for (int i = 0; i < length; i++)
            {
                var stringKey = global::MessagePack.Internal.CodeGenHelpers.ReadStringSpan(ref reader);
                switch (stringKey.Length)
                {
                    default:
                    FAIL:
                      reader.Skip();
                      continue;
                    case 5:
                        if (global::MessagePack.Internal.AutomataKeyGen.GetKey(ref stringKey) != 461229747266UL) { goto FAIL; }

                        __Block__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::Shared.BlockId>(formatterResolver).Deserialize(ref reader, options);
                        continue;
                    case 11:
                        if (!global::System.MemoryExtensions.SequenceEqual(stringKey, GetSpan_DamageLevel().Slice(1))) { goto FAIL; }

                        __DamageLevel__ = reader.ReadByte();
                        continue;

                }
            }

            var ____result = new global::Shared.Cell(__Block__, __DamageLevel__);
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
