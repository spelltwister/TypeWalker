﻿using System;
using System.Collections.Generic;
using TypeWalker.Extensions;

namespace TypeWalker.Generators
{
    public class TypeScriptLanguage : Language
    {
        private static System.CodeDom.Compiler.CodeDomProvider codeDomProvider;

        static TypeScriptLanguage()
        {
            TypeScriptLanguage.codeDomProvider = System.CodeDom
                .Compiler
                .CodeDomProvider
                .CreateProvider("CSharp");
        }

        private readonly Dictionary<Type, TypeInfo> cache = new Dictionary<Type, TypeInfo>();

        public override TypeInfo GetTypeInfo(Type type)
        {
            TypeInfo result;
            if (!cache.TryGetValue(type, out result))
            {
                result = GetTypeInfoInner(type);
                cache[type] = result;
            }
            return result;
        }

        private static readonly TypeInfo any = new TypeInfo("any", string.Empty);

        public TypeInfo GetTypeInfoInner(Type type)
        {
            if (type.IsNullableType())
            {
                var baseType = type.GenericTypeArguments[0];
                return GetTypeInfo(baseType);
            }

            Type genericElementType = null;
            string typescriptName;

            if (TryGetTypeName(type.FullName, out typescriptName))
            {
                return new TypeInfo(typescriptName, string.Empty);
            }

            if (type.TryGetGenericCollectionType(out genericElementType))
            {
                var typeInfo = GetTypeInfo(genericElementType);
                var arrayResult = new TypeInfo(typeInfo.Name + "[]", typeInfo.NameSpaceName);
                return arrayResult;
            }

            if (type.IsDictionaryType())
            {
                return TypeScriptLanguage.any;
            }

            if (type.IsEnum)
            {
                return new TypeInfo("number", string.Empty);
            }

            //*
            if (!type.IsExportableType())
            {
                return TypeScriptLanguage.any;
            }
            //*/
            
            // for now, uses a C# style for anything else
            string typeName = type.FullName.Replace(type.Namespace + ".", "");
            var typeReference = new System.CodeDom.CodeTypeReference(typeName);
            var nameSpace = type.Namespace != "System" ? type.Namespace : "";
            return new TypeInfo(typeName, nameSpace);
        }

        private bool TryGetTypeName(string typeFullName, out string typescriptName)
        {
            switch (typeFullName)
            {
                case "System.String": typescriptName = "string"; return true;
                case "System.Byte": typescriptName = "string"; return true;
                case "System.Int32": typescriptName = "number"; return true;
                case "System.Int64": typescriptName = "number"; return true;
                case "System.Int16": typescriptName = "number"; return true;
                case "System.UInt32": typescriptName = "number"; return true;
                case "System.UInt64": typescriptName = "number"; return true;
                case "System.UInt16": typescriptName = "number"; return true;
                case "System.Decimal": typescriptName = "number"; return true;
                case "System.Single": typescriptName = "number"; return true;
                case "System.Double": typescriptName = "number"; return true;
                case "System.Boolean": typescriptName = "boolean"; return true;
                case "System.DateTime": typescriptName = "Date"; return true;
                case "System.Guid": typescriptName = "string"; return true;
                default: typescriptName = null; return false;
            }
        }
    }
}