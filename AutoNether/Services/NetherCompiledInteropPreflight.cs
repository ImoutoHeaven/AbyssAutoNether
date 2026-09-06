#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace AutoNether.Services;

/// <summary>Resolve compiled interop references before a later JIT can discover a missing game member.</summary>
internal static class NetherCompiledInteropPreflight
{
    public static int Validate(Assembly plugin, List<string> failures)
    {
        int checkedCount = 0;
        try
        {
            using var pe = new PEReader(File.OpenRead(plugin.Location));
            MetadataReader metadata = pe.GetMetadataReader();
            var nativeTypes = new NativeTypeProvider();
            foreach (TypeReferenceHandle handle in metadata.TypeReferences)
            {
                if (!nativeTypes.GetTypeFromReference(metadata, handle, 0))
                    continue;
                TypeReference type = metadata.GetTypeReference(handle);
                Check(MetadataTokens.GetToken(handle), metadata.GetString(type.Name));
            }
            foreach (MemberReferenceHandle handle in metadata.MemberReferences)
            {
                MemberReference member = metadata.GetMemberReference(handle);
                bool native = member.Parent.Kind switch
                {
                    HandleKind.TypeReference => nativeTypes.GetTypeFromReference(metadata, (TypeReferenceHandle)member.Parent, 0),
                    HandleKind.TypeSpecification => metadata.GetTypeSpecification((TypeSpecificationHandle)member.Parent)
                        .DecodeSignature(nativeTypes, (object?)null),
                    _ => false,
                };
                if (native)
                    Check(MetadataTokens.GetToken(handle), metadata.GetString(member.Name));
            }

            void Check(int token, string name)
            {
                checkedCount++;
                try
                {
                    if (plugin.ManifestModule.ResolveMember(token) == null)
                        failures.Add("compiled-interop:" + name + " => unresolved-reference");
                }
                catch (Exception ex)
                {
                    failures.Add("compiled-interop:" + name + " => " + ex.GetType().Name + ":" + ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            failures.Add("compiled-interop-precheck => " + ex.GetType().Name + ":" + ex.Message);
        }
        return checkedCount;
    }

    // The framework's signature decoder handles arrays, nested generics and modifiers.
    // The value only identifies references involving generated game/Unity assemblies.
    private sealed class NativeTypeProvider : ISignatureTypeProvider<bool, object?>
    {
        public bool GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            EntityHandle scope = reader.GetTypeReference(handle).ResolutionScope;
            if (scope.Kind == HandleKind.TypeReference)
                return GetTypeFromReference(reader, (TypeReferenceHandle)scope, rawTypeKind);
            if (scope.Kind != HandleKind.AssemblyReference)
                return false;
            string assembly = reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)scope).Name);
            return assembly is "Project" or "Absf" or "UniTask" or "UniRx" or "Il2Cppmscorlib"
                || assembly.StartsWith("UnityEngine.", StringComparison.Ordinal);
        }

        public bool GetTypeFromSpecification(MetadataReader reader, object? context, TypeSpecificationHandle handle, byte rawTypeKind) =>
            reader.GetTypeSpecification(handle).DecodeSignature(this, context);
        public bool GetGenericInstantiation(bool type, ImmutableArray<bool> arguments) => type || arguments.Any(value => value);
        public bool GetArrayType(bool element, ArrayShape shape) => element;
        public bool GetSZArrayType(bool element) => element;
        public bool GetByReferenceType(bool element) => element;
        public bool GetPointerType(bool element) => element;
        public bool GetPinnedType(bool element) => element;
        public bool GetModifiedType(bool modifier, bool type, bool required) => modifier || type;
        public bool GetFunctionPointerType(MethodSignature<bool> signature) => signature.ReturnType || signature.ParameterTypes.Any(value => value);
        public bool GetGenericMethodParameter(object? context, int index) => false;
        public bool GetGenericTypeParameter(object? context, int index) => false;
        public bool GetPrimitiveType(PrimitiveTypeCode type) => false;
        public bool GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => false;
    }
}
