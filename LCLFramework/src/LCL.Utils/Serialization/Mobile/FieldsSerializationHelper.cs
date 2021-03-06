﻿

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LCL.Reflection;


namespace LCL.Serialization.Mobile
{
    /// <summary>
    /// 为了兼容系统的序列化机制，特写此类进行字段的序列化：
    /// 只要不标记 NonSerialized 的字段都进行序列化。
    /// </summary>
    public static class FieldsSerializationHelper
    {
        public static void SerialzeFields(object obj, ISerializationContext info)
        {
            bool isState = info.IsProcessingState;

            var fields = EnumerateSerializableFields(obj.GetType());
            foreach (var f in fields)
            {
                var v = f.GetValue(obj);
                var vType = v != null ? v.GetType() : f.FieldType;

                if (isState)
                {
                    if (info.IsState(vType)) { info.AddState(f.Name, v); }
                }
                else
                {
                    if (!info.IsState(vType)) { info.AddRef(f.Name, v); }
                }
            }
        }

        public static void DeserialzeFields(object obj, ISerializationContext info)
        {
            var formatter = info.RefFormatter;
            bool isState = info.IsProcessingState;

            var fields = EnumerateSerializableFields(obj.GetType());

            if (isState)
            {
                var allStates = info.States;
                foreach (var kv in allStates)
                {
                    var name = kv.Key;

                    var f = FindSingleField(fields, name);
                    if (f != null)
                    {
                        var v = TypeHelper.CoerceValue(f.FieldType, kv.Value);
                        f.SetValue(obj, v);
                    }
                }
            }
            else
            {
                var allReferences = info.References;
                foreach (var kv in allReferences)
                {
                    var name = kv.Key;

                    var f = FindSingleField(fields, name);
                    if (f != null)
                    {
                        var v = formatter.GetObject(kv.Value);
                        if (v != null) { f.SetValue(obj, v); }
                    }
                }
            }
        }

        private static FieldInfo FindSingleField(IEnumerable<FieldInfo> fields, string name)
        {
            var result = fields.Where(p => p.Name == name).ToArray();
            if (result.Length > 1)
            {
                throw new InvalidOperationException(string.Format("存在两个同名的字段：{0}.{1}, {2}.{3}，无法支持序列化。",
                    result[0].DeclaringType.Name, result[0].Name,
                    result[1].DeclaringType.Name, result[1].Name
                    ));
            }

            if (result.Length == 1) return result[0];

            return null;
        }

        private static IEnumerable<FieldInfo> EnumerateSerializableFields(Type objType)
        {
            var hierarchy = TypeHelper.GetHierarchy(objType,
                typeof(MobileObject), typeof(MobileBindingList<>), typeof(MobileList<>), typeof(MobileDictionary<,>)
                );

            foreach (var type in hierarchy)
            {
                if (type.IsDefined(typeof(MobileNonSerializedAttribute), false)) break;

                //由于本函数只为兼容，所以没有标记 [Serializable] 的类型就直接忽略它里面的所有字段。
                if (type.IsSerializable)
                {
                    var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    foreach (var field in fields)
                    {
                        if (!field.IsDefined(typeof(NonSerializedAttribute), false))
                        {
                            if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                            {
                                throw new InvalidOperationException(string.Format(
                                    "{0} 类中的字段 {1} 是代理类型，不能直接被序列化，请标记 NonSerializd 并重写 OnSerializeState 以自定义序列化。",
                                    field.DeclaringType.Name, field.Name
                                    ));
                            }

                            yield return field;
                        }
                    }
                }
            }
        }
    }
}
