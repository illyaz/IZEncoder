namespace IZEncoder.Common.ASSParser
{
    using System;
    using System.Reflection;
    using Serializer;

    internal sealed class ScriptInfoSerializeHelper
    {
        private readonly object defaultValue;
        private readonly string format;

        public ScriptInfoSerializeHelper(FieldInfo info, ScriptInfoAttribute fieldInfo, SerializeAttribute serializer)
        {
            GetValue = info.GetValue;
            SetValue = info.SetValue;
            defaultValue = fieldInfo.DefaultValue;
            if (serializer != null)
            {
                //custom
                Deserialize = deserializeCustom(this, serializer.Deserialize);
                DeserializeExact = deserializeCustomExact(this, serializer.Deserialize);
                if (fieldInfo.IsOptional)
                    Serialize = serializeOptional(this, serializer.Serialize);
                else
                    Serialize = serialize(this, serializer.Serialize);
                format = fieldInfo.FieldName + ": {0}";
                return;
            }

            //enum, nullable and others
            if (fieldInfo.IsOptional)
                Serialize = serializeOptional(this);
            else
                Serialize = serialize(this);
            var fieldType = info.FieldType;
            format = fieldInfo.FieldName + ": {0:" + fieldInfo.Format + "}";
            Type nullableInner;
            if ((nullableInner = Nullable.GetUnderlyingType(fieldType)) != null)
            {
                //nullable
                if (defaultValue?.GetType() == nullableInner)
                    defaultValue = Activator.CreateInstance(fieldType, defaultValue);
                Deserialize = deserializeNullable(this, fieldType, nullableInner);
                DeserializeExact = deserializeNullableExact(this, fieldType, nullableInner);
                return;
            }

            if (fieldType.GetTypeInfo().IsEnum)
            {
                //enum
                Deserialize = deserializeEnum(this, fieldType);
                DeserializeExact = deserializeEnumExact(this, fieldType);
                return;
            }

            //default
            Deserialize = deserializeDefault(this, fieldType);
            DeserializeExact = deserializeDefaultExact(this, fieldType);
        }

        public GetValueDelegate GetValue { get; }

        public SetValueDelegate SetValue { get; }

        public DeserializeDelegate Deserialize { get; }

        public DeserializeDelegate DeserializeExact { get; }

        public SerializeDelegate Serialize { get; }

        private static DeserializeDelegate deserializeDefault(ScriptInfoSerializeHelper target, Type fieldType)
        {
            return (obj, value) =>
            {
                try
                {
                    target.SetValue(obj, Convert.ChangeType(value, fieldType, FormatHelper.DefaultFormat));
                }
                catch (FormatException)
                {
                    target.SetValue(obj, target.defaultValue);
                }
            };
        }

        private static DeserializeDelegate deserializeCustom(ScriptInfoSerializeHelper target,
            DeserializerDelegate deserializer)
        {
            return (obj, value) =>
            {
                try
                {
                    target.SetValue(obj, deserializer(value));
                }
                catch (FormatException)
                {
                    target.SetValue(obj, target.defaultValue);
                }
            };
        }

        private static DeserializeDelegate deserializeEnum(ScriptInfoSerializeHelper target, Type fieldType)
        {
            return (obj, value) =>
            {
                try
                {
                    target.SetValue(obj, Enum.Parse(fieldType, value, true));
                }
                catch (FormatException)
                {
                    target.SetValue(obj, target.defaultValue);
                }
            };
        }

        private static DeserializeDelegate deserializeNullable(ScriptInfoSerializeHelper target, Type fieldType,
            Type innerType)
        {
            return (obj, value) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        target.SetValue(obj, target.defaultValue);
                    }
                    else
                    {
                        var innerValue = Convert.ChangeType(value, innerType, FormatHelper.DefaultFormat);
                        var nullable = Activator.CreateInstance(fieldType, innerValue);
                        target.SetValue(obj, nullable);
                    }
                }
                catch (FormatException)
                {
                    target.SetValue(obj, target.defaultValue);
                }
            };
        }

        private static DeserializeDelegate deserializeDefaultExact(ScriptInfoSerializeHelper target, Type fieldType)
        {
            return (obj, value) =>
                target.SetValue(obj, Convert.ChangeType(value, fieldType, FormatHelper.DefaultFormat));
        }

        private static DeserializeDelegate deserializeCustomExact(ScriptInfoSerializeHelper target,
            DeserializerDelegate deserializer)
        {
            return (obj, value) => target.SetValue(obj, deserializer(value));
        }

        private static DeserializeDelegate deserializeEnumExact(ScriptInfoSerializeHelper target, Type fieldType)
        {
            return (obj, value) => target.SetValue(obj, Enum.Parse(fieldType, value, true));
        }

        private static DeserializeDelegate deserializeNullableExact(ScriptInfoSerializeHelper target, Type fieldType,
            Type innerType)
        {
            return (obj, value) =>
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    target.SetValue(obj, target.defaultValue);
                }
                else
                {
                    var innerValue = Convert.ChangeType(value, innerType, FormatHelper.DefaultFormat);
                    var nullable = Activator.CreateInstance(fieldType, innerValue);
                    target.SetValue(obj, nullable);
                }
            };
        }

        private static SerializeDelegate serialize(ScriptInfoSerializeHelper target)
        {
            return obj =>
            {
                var value = target.GetValue(obj) ?? target.defaultValue;
                return string.Format(FormatHelper.DefaultFormat, target.format, value);
            };
        }

        private static SerializeDelegate serialize(ScriptInfoSerializeHelper target, SerializerDelegate serializer)
        {
            return obj =>
            {
                var value = target.GetValue(obj) ?? target.defaultValue;
                return string.Format(FormatHelper.DefaultFormat, target.format, serializer(value));
            };
        }

        private static SerializeDelegate serializeOptional(ScriptInfoSerializeHelper target)
        {
            return obj =>
            {
                var value = target.GetValue(obj);
                if (value == null || value == target.defaultValue)
                    return null;
                return string.Format(FormatHelper.DefaultFormat, target.format, value);
            };
        }

        private static SerializeDelegate serializeOptional(ScriptInfoSerializeHelper target,
            SerializerDelegate serializer)
        {
            return obj =>
            {
                var value = target.GetValue(obj);
                if (value == null || value == target.defaultValue)
                    return null;
                return string.Format(FormatHelper.DefaultFormat, target.format, serializer(value));
            };
        }
    }
}