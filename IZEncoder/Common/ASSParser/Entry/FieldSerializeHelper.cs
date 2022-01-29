namespace IZEncoder.Common.ASSParser
{
    using System;
    using System.Reflection;
    using Serializer;

    internal sealed class FieldSerializeHelper
    {
        private readonly object defaultValue;

        public FieldSerializeHelper(FieldInfo info, EntryFieldAttribute fieldInfo, SerializeAttribute serializer)
        {
            defaultValue = fieldInfo.DefaultValue;
            GetValue = info.GetValue;
            SetValue = info.SetValue;
            if (serializer != null)
            {
                Serialize = serializeCustom(this, serializer.Serialize);
                Deserialize = deserializeCustom(this, serializer.Deserialize);
                DeserializeExact = deserializeCustomExact(this, serializer.Deserialize);
                return;
            }

            Serialize = serializeDefault(this, fieldInfo.Format);
            if (info.FieldType.GetTypeInfo().IsEnum)
            {
                Deserialize = deserializeEnum(this, info.FieldType);
                DeserializeExact = deserializeEnumExact(this, info.FieldType);
            }
            else
            {
                Deserialize = deserializeDefault(this, info.FieldType);
                DeserializeExact = deserializeDefaultExact(this, info.FieldType);
            }
        }

        public GetValueDelegate GetValue { get; }

        public SetValueDelegate SetValue { get; }

        public DeserializeDelegate Deserialize { get; }

        public DeserializeDelegate DeserializeExact { get; }

        public SerializeDelegate Serialize { get; }

        private static DeserializeDelegate deserializeCustom(FieldSerializeHelper field,
            DeserializerDelegate deserializer)
        {
            return (obj, value) =>
            {
                try
                {
                    field.SetValue(obj, deserializer(value));
                }
                catch (FormatException)
                {
                    field.SetValue(obj, field.defaultValue);
                }
            };
        }

        private static DeserializeDelegate deserializeDefault(FieldSerializeHelper field, Type fieldType)
        {
            return (obj, value) =>
            {
                try
                {
                    field.SetValue(obj, Convert.ChangeType(value, fieldType, FormatHelper.DefaultFormat));
                }
                catch (FormatException)
                {
                    field.SetValue(obj, field.defaultValue);
                }
            };
        }

        private static DeserializeDelegate deserializeEnum(FieldSerializeHelper field, Type fieldType)
        {
            return (obj, value) =>
            {
                try
                {
                    field.SetValue(obj, Enum.Parse(fieldType, value, true));
                }
                catch (FormatException)
                {
                    field.SetValue(obj, field.defaultValue);
                }
            };
        }

        private static DeserializeDelegate deserializeCustomExact(FieldSerializeHelper field,
            DeserializerDelegate deserializer)
        {
            return (obj, value) => field.SetValue(obj, deserializer(value));
        }

        private static DeserializeDelegate deserializeDefaultExact(FieldSerializeHelper field, Type fieldType)
        {
            return (obj, value) =>
                field.SetValue(obj, Convert.ChangeType(value, fieldType, FormatHelper.DefaultFormat));
        }

        private static DeserializeDelegate deserializeEnumExact(FieldSerializeHelper field, Type fieldType)
        {
            return (obj, value) => field.SetValue(obj, Enum.Parse(fieldType, value, true));
        }

        private static SerializeDelegate serializeCustom(FieldSerializeHelper field, SerializeDelegate serializer)
        {
            return obj => serializer(field.GetValue(obj) ?? field.defaultValue);
        }

        private static SerializeDelegate serializeDefault(FieldSerializeHelper field, string format)
        {
            var formatStr = $"{{0:{format}}}";
            return obj =>
                string.Format(FormatHelper.DefaultFormat, formatStr, field.GetValue(obj) ?? field.defaultValue);
        }
    }
}