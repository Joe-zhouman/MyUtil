using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Util.DeepCopy.ArrayExtensions;

namespace Util.DeepCopy {
    public static class ObjectDeepCopyExtensions {
        private static readonly MethodInfo CloneMethod =
            typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool IsPrimitive(this Type type) {
            if (type == typeof(string)) return true;
            return type.IsValueType & type.IsPrimitive;
        }

        public static object MemberwiseCopy(this object sourceObject) {
            return InternalCopy(sourceObject, new Dictionary<object, object>(new ReferenceEqualityComparer()));
        }

        private static object InternalCopy(object sourceObject, IDictionary<object, object> visited) {
            if (sourceObject == null) return null;
            Type typeToReflect = sourceObject.GetType();
            if (IsPrimitive(typeToReflect)) return sourceObject;
            if (visited.ContainsKey(sourceObject)) return visited[sourceObject];
            if (typeof(Delegate).IsAssignableFrom(typeToReflect)) return null;
            object? targetObject = CloneMethod.Invoke(sourceObject, null);
            if (typeToReflect.IsArray) {
                Type? arrayType = typeToReflect.GetElementType();
                if (IsPrimitive(arrayType) == false) {
                    Array clonedArray = (Array) targetObject;
                    clonedArray.ForEach((array, indices) =>
                        array.SetValue(InternalCopy(clonedArray.GetValue(indices), visited), indices));
                }
            }

            visited.Add(sourceObject, targetObject);
            CopyFields(sourceObject, visited, targetObject, typeToReflect);
            RecursiveCopyBaseTypePrivateFields(sourceObject, visited, targetObject, typeToReflect);
            return targetObject;
        }

        private static void RecursiveCopyBaseTypePrivateFields(object sourceObject,
            IDictionary<object, object> visited, object targetObject, Type typeToReflect) {
            if (typeToReflect.BaseType != null) {
                RecursiveCopyBaseTypePrivateFields(sourceObject, visited, targetObject, typeToReflect.BaseType);
                CopyFields(sourceObject, visited, targetObject, typeToReflect.BaseType,
                    BindingFlags.Instance | BindingFlags.NonPublic, info => info.IsPrivate);
            }
        }

        private static void CopyFields(object sourceObject, IDictionary<object, object> visited, object targetObject,
            Type typeToReflect,
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public |
                                        BindingFlags.FlattenHierarchy, Func<FieldInfo, bool> filter = null) {
            foreach (FieldInfo fieldInfo in typeToReflect.GetFields(bindingFlags)) {
                if (filter != null && filter(fieldInfo) == false) continue;
                if (IsPrimitive(fieldInfo.FieldType)) continue;
                object? originalFieldValue = fieldInfo.GetValue(sourceObject);
                object clonedFieldValue = InternalCopy(originalFieldValue, visited);
                fieldInfo.SetValue(targetObject, clonedFieldValue);
            }
        }
        /// <summary>
        /// Memberwise Copy which [Serializable] is unnecessary
        /// </summary>
        /// <typeparam name="T">any type</typeparam>
        /// <param name="source">source object</param>
        /// <returns></returns>
        public static T MemberwiseCopy<T>(this T source) {
            return (T)MemberwiseCopy((object) source);
        }

        /// <summary>
        /// Deep copy for [Serializable] class
        /// </summary>
        /// <typeparam name="T">[Serializable] class</typeparam>
        /// <param name="source">source object</param>
        /// <returns></returns>
        public static T DeepCopy<T>(this T source) {
            using MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, source);
            stream.Position = 0;
            return (T)formatter.Deserialize(stream);
        }
    }

    public class ReferenceEqualityComparer : EqualityComparer<object> {
        public override bool Equals(object x, object y) {
            return ReferenceEquals(x, y);
        }

        public override int GetHashCode(object obj) {
            if (obj == null) return 0;
            return obj.GetHashCode();
        }
    }

    namespace ArrayExtensions {
        public static class ArrayExtensions {
            public static void ForEach(this Array array, Action<Array, int[]> action) {
                if (array.LongLength == 0) return;
                ArrayTraverse walker = new ArrayTraverse(array);
                do {
                    action(array, walker.Position);
                } while (walker.Step());
            }
        }

        internal class ArrayTraverse {
            private readonly int[] _maxLengths;
            public readonly int[] Position;

            public ArrayTraverse(Array array) {
                _maxLengths = new int[array.Rank];
                for (int i = 0; i < array.Rank; ++i) _maxLengths[i] = array.GetLength(i) - 1;
                Position = new int[array.Rank];
            }

            public bool Step() {
                for (int i = 0; i < Position.Length; ++i)
                    if (Position[i] < _maxLengths[i]) {
                        Position[i]++;
                        for (int j = 0; j < i; j++) Position[j] = 0;
                        return true;
                    }

                return false;
            }
        }
    }
}