using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TypeWalker.Extensions;

namespace TypeWalker.Generators
{
    public class KnockoutLanguage : TypeScriptLanguage
    {
        protected readonly string namespacePrefix;

        public KnockoutLanguage(string namespacePrefix)
        {
            this.namespacePrefix = namespacePrefix;
        }

        public override TypeInfo GetTypeInfo(Type type)
        {
            var original = base.GetTypeInfo(type);
            if (string.IsNullOrWhiteSpace(original.NameSpaceName) || string.IsNullOrWhiteSpace(this.namespacePrefix))
            {
                return original;
            }

            return new TypeInfo(original.Name, $"{this.namespacePrefix}.{original.NameSpaceName}");
        }
    }

    public class Exports
    {
        public ILookup<string, string> Dtos { get; set; }
        
        public ILookup<string, string> Controllers { get; set; }  
    }

    public static class KnockoutGeneratorFactory
    {
        private static readonly KnockoutLanguage TSLang = new KnockoutLanguage(null);
        public static Exports Create(IEnumerable<Type> typesToExport)
        {
            return Create(typesToExport.ToArray());
        }

        public static Exports Create(ICollection<Type> typesToExport)
        {
            var dtos = typesToExport.Where(IsDto).ToArray();
            var controllers = typesToExport.Where(IsController).ToArray();
            var other = typesToExport.Except(dtos)
                                     .Except(controllers)
                                     .ToArray();

            return new Exports()
            {
                Controllers = ControllersByNamespace(controllers),
                Dtos = DtosByNamespace(dtos)
            };
        }

        private static ILookup<string, string> DtosByNamespace(ICollection<Type> dtoTypes)
        {
            return dtoTypes.ToLookup(x => $"{TSLang.GetTypeInfo(x).NameSpaceName}.Dtos",
                                     x => DtoString(x));
        }

        private static string DtoString(Type t)
        {
            return $"{DtoType(t)}{Environment.NewLine}{DtoInterface(t)}{Environment.NewLine}{DtoImplementation(t)}";
        }

        private static string DtoType(Type t)
        {
            var typeInfo = TSLang.GetTypeInfo(t);
            
            StringBuilder sb = new StringBuilder($"    export type {typeInfo.Name} = {{");
            sb.AppendLine();

            // types do not support inheritance so much list all properites from base types onto this type
            foreach (var property in t.GetProperties())
            {
                sb.AppendLine($"        {property.Name}: {TSLang.GetTypeInfo(property.PropertyType).Name};");
            }

            sb.Append("    };");
            return sb.ToString();
        }

        private static string DtoInterface(Type t)
        {
            var typeInfo = TSLang.GetTypeInfo(t);

            StringBuilder sb = new StringBuilder($"    export interface I{typeInfo.Name} ");
            if (t.BaseType != null)
            {
                sb.Append($"extends I{TSLang.GetTypeInfo(t.BaseType).Name} ");
            }
            sb.AppendLine("{");

            foreach (var property in t.GetProperties().Where(x => x.DeclaringType == t))
            {
                sb.AppendLine($"        {property.Name}: KnockoutObservable<{TSLang.GetTypeInfo(property.PropertyType).Name}>;");
            }

            sb.Append("    };");
            return sb.ToString();
        }

        private static string DtoImplementation(Type t)
        {
            var typeInfo = TSLang.GetTypeInfo(t);

            StringBuilder sb = new StringBuilder($"    export class {typeInfo.Name}Edit implements I{typeInfo.Name} {{");
            sb.AppendLine();

            foreach (var property in t.GetProperties())
            {
                if (property.PropertyType.IsGenericCollectionType())
                {
                    var collectionName = TSLang.GetTypeInfo(property.PropertyType).Name;
                    collectionName = collectionName.Substring(0, collectionName.Length - 2);

                    Type collectionType;
                    if (property.PropertyType.TryGetGenericCollectionType(out collectionType) &&
                        collectionType.IsExportableType())
                    {
                        sb.AppendLine($"        {property.Name}: KnockoutObservableArray<{collectionName}Edit>;");
                    }
                    else
                    {
                        sb.AppendLine($"        {property.Name}: KnockoutObservableArray<{collectionName}>;");
                    }
                }
                else
                {
                    if (property.PropertyType.IsExportableType())
                    {
                        sb.AppendLine($"        {property.Name}: {TSLang.GetTypeInfo(property.PropertyType).Name}Edit;");
                    }
                    else
                    {
                        sb.AppendLine($"        {property.Name}: KnockoutObservable<{TSLang.GetTypeInfo(property.PropertyType).Name}>;");
                    }
                }
            }

            sb.AppendLine($"        constructor(initial?:{typeInfo.Name}) {{");
            foreach (var property in t.GetProperties())
            {
                if (property.PropertyType.IsGenericCollectionType())
                {
                    Type collectionType;
                    if (property.PropertyType.TryGetGenericCollectionType(out collectionType) &&
                        collectionType.IsExportableType())
                    {
                        var collectionName = TSLang.GetTypeInfo(property.PropertyType).Name;
                        collectionName = collectionName.Substring(0, collectionName.Length - 2);
                        sb.AppendLine($"            this.{property.Name} = ko.observableArray(initial && initial.{property.Name} && $.map(initial.{property.Name}, (item) => new {collectionName}Edit(item)));");
                    }
                    else
                    {
                        sb.AppendLine($"            this.{property.Name} = ko.observableArray(initial && initial.{property.Name});");
                    }
                }
                else
                {
                    if (property.PropertyType.IsExportableType())
                    {
                        sb.AppendLine($"            this.{property.Name} = new {TSLang.GetTypeInfo(property.PropertyType).Name}Edit(initial && initial.{property.Name});");
                    }
                    else
                    { 
                        sb.AppendLine($"            this.{property.Name} = ko.observable(initial && initial.{property.Name});");
                    }
                }
            }
            sb.AppendLine("        }");

            sb.Append("    };");
            return sb.ToString();
        }

        private static ILookup<string, string> ControllersByNamespace(ICollection<Type> controllerTypes)
        {
            return controllerTypes.ToLookup(x => $"{TSLang.GetTypeInfo(x).NameSpaceName}.Controllers",
                                            x => ControllerString(x));
        }

        private static string ControllerString(Type t)
        {
            return $"{ControllerInterface(t)}{Environment.NewLine}{ControllerImplementation(t)}";
        }

        private static string ControllerInterface(Type t)
        {
            var typeInfo = TSLang.GetTypeInfo(t);

            StringBuilder sb = new StringBuilder($"    export interface I{typeInfo.Name} ");
            if (t.BaseType != null)
            {
                sb.Append($"extends I{TSLang.GetTypeInfo(t.BaseType).Name} ");
            }
            sb.AppendLine("{");

            foreach (var method in t.GetMethods().Where(x => x.DeclaringType == t && !x.IsSpecialName /*Property getter/setter*/))
            {
                // TODO: when there are multiple parameters, need to create a version that has a parameter object as the only parameter
                sb.AppendLine($"        {method.Name}({string.Join(", ", method.GetParameters().Select(x => $"{x.Name}: {TSLang.GetTypeInfo(x.ParameterType).Name}"))}) : JQueryPromise<{TSLang.GetTypeInfo(method.ReturnType).Name}>;");
                sb.AppendLine();
            }

            sb.Append("    };");
            return sb.ToString();
        }

        private static string ControllerImplementation(Type t)
        {
            var typeInfo = TSLang.GetTypeInfo(t);
            
            // TODO: inheritance
            StringBuilder sb = new StringBuilder($"    export class {typeInfo.Name} implements I{typeInfo.Name} {{");
            sb.AppendLine();

            sb.AppendLine("        private urlBase: string;");
            sb.AppendLine("        constructor(urlBase: string) {");
            sb.AppendLine("            this.urlBase = urlBase;");
            sb.AppendLine("        }");
            sb.AppendLine();

            foreach (var method in t.GetMethods().Where(x => x.DeclaringType == t && !x.IsSpecialName))
            {
                // TODO: when there are multiple parameters, need to create a version that has a parameter object as the only parameter
                sb.AppendLine($"        {method.Name}({string.Join(", ", method.GetParameters().Select(x => $"{x.Name}: {TSLang.GetTypeInfo(x.ParameterType).Name}"))}) : JQueryPromise<{TSLang.GetTypeInfo(method.ReturnType).Name}> {{");
                sb.AppendLine();

                sb.AppendLine($"            return $.getJSON(this.urlBase + '/api/');");
                sb.AppendLine();

                sb.AppendLine($"            return $.postJSON(this.urlBase + '/api/');");

                sb.AppendLine("        };");
            }

            sb.Append("    };");
            return sb.ToString();
        }

        private static bool IsController(Type t)
        {
            return HasOwnMethods(t) && !HasProperties(t);
        }

        private static bool IsDto(Type t)
        {
            return !HasOwnMethods(t) && HasProperties(t);
        }

        private static bool HasOwnMethods(Type t)
        {
            return t.GetMethods().Any(x => x.DeclaringType == t && !t.IsSpecialName);
        }

        private static bool HasProperties(Type t)
        {
            return t.GetProperties().Any();
        }
    }

    public class KnockoutMappingGenerator : LanguageGenerator
    {
        public const string Id = "KnockoutMapping";
        
        public KnockoutMappingGenerator(string namespacePrefix)
            : base(new KnockoutLanguage(namespacePrefix), Id)
        {
        }

        public override string NamespaceStartFormat => "/* {Comment} */" + Environment.NewLine + "declare module {NameSpaceName}.Editable {{" + Environment.NewLine;

        public override string NamespaceEndFormat => "}}" + Environment.NewLine + Environment.NewLine;

        public override string DerivedTypeStartFormat => "    export interface I{TypeName} extends I{BaseTypeInfo.NameSpaceName}.{BaseTypeInfo.TypeName} {{" + Environment.NewLine;

        public override string TerminalTypeStartFormat => "    export interface I{TypeName} {{" + Environment.NewLine;

        public override string TypeEndFormat => "    }}" + Environment.NewLine;

        public override string MemberStartFormat => "        {MemberName}: KnockoutObservable<{MemberTypeFullName}>;" + Environment.NewLine;

        public override string MemberEndFormat => string.Empty;

        public override string MethodStartFormat => "        {MethodName}(): JQueryPromise<any>;";

        public override string MethodEndFormat => string.Empty;

        public override bool ExportsNonPublicMembers => false;
    }

    public class TypeScriptTypeGenerator : LanguageGenerator
    {
        public const string Id = "TypeScriptRadOnlyTypes";

        public TypeScriptTypeGenerator(string namespacePrefix)
            : base(new KnockoutLanguage(namespacePrefix), Id)
        {
        }

        public override string NamespaceStartFormat => "/* {Comment} */" + Environment.NewLine + "declare module {NameSpaceName}.Types {{" + Environment.NewLine;

        public override string NamespaceEndFormat => "}}" + Environment.NewLine + Environment.NewLine;

        public override string DerivedTypeStartFormat => "    export interface {TypeName} extends {BaseTypeInfo.NameSpaceName}.{BaseTypeInfo.TypeName} {{" + Environment.NewLine;

        public override string TerminalTypeStartFormat => "    export interface {TypeName} {{" + Environment.NewLine;

        public override string TypeEndFormat => "    }}" + Environment.NewLine;

        public override string MemberStartFormat => "        {MemberName}: KnockoutObservable<{MemberTypeFullName}>;" + Environment.NewLine;

        public override string MemberEndFormat => string.Empty;

        public override string MethodStartFormat => "        {MethodName}(): void;";

        public override string MethodEndFormat => string.Empty;

        public override bool ExportsNonPublicMembers => false;
    }
}