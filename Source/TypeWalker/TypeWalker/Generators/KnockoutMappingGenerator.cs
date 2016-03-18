﻿using System;

namespace TypeWalker.Generators
{
    public class KnockoutMappingGenerator : LanguageGenerator
    {
        public const string Id = "KnockoutMapping";

        private class KnockoutLanguage: Language
        {
            TypeScriptLanguage typescript;
            string namespacePrefix;

            public KnockoutLanguage(string namespacePrefix)
            {
                this.typescript = new TypeScriptLanguage();
                this.namespacePrefix = namespacePrefix;
            }

            public override TypeInfo GetTypeInfo(Type type)
            {
                var original = this.typescript.GetTypeInfo(type);

                var correctNamespace = string.IsNullOrWhiteSpace(original.NameSpaceName) 
                    ? original.NameSpaceName 
                    : this.namespacePrefix + "." + original.NameSpaceName;

                var info = new TypeInfo(original.Name, correctNamespace);

                return info;
            }
        }

        public KnockoutMappingGenerator(string namespacePrefix)
            : base(new KnockoutLanguage(namespacePrefix), Id)
        {
        }

        public override string NamespaceStartFormat
        {
            get
            { 
                return "/* {Comment} */" + Environment.NewLine + "declare module {NameSpaceName} {{" + Environment.NewLine; 
            }
        }

        public override string NamespaceEndFormat
        { 
            get
            {
                return "}}" + Environment.NewLine + Environment.NewLine; 
            }
        }

        public override string DerivedTypeStartFormat
        {
            get
            {
                return "    export interface {TypeName} extends {BaseTypeInfo.NameSpaceName}.{BaseTypeInfo.TypeName} {{" + Environment.NewLine;
            }
        }

        public override string TerminalTypeStartFormat 
        {
            get 
            { 
                return "    export interface {TypeName} {{" + Environment.NewLine; 
            }
        }

        public override string TypeEndFormat { get { return "    }}" + Environment.NewLine; } }

        public override string MemberStartFormat
        {
            get
            {
                return
                    "        {MemberName}: KnockoutObservable<{MemberTypeFullName}>;" + Environment.NewLine;
            }
        }

        public override string MemberEndFormat 
        {
            get
            {
                return string.Empty; 
            }
        }

        public override string MethodStartFormat => "        {MethodName}(): void;";

        public override string MethodEndFormat => string.Empty;

        public override bool ExportsNonPublicMembers
        {
            get { return false ; }
        }
    }
}