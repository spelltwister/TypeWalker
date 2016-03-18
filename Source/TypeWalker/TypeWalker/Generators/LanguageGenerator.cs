using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeWalker.Extensions;

namespace TypeWalker.Generators
{
    public abstract class LanguageGenerator
    {
        private readonly Language language;
        private readonly string id;

        protected LanguageGenerator(Language language, string id)
        {
            this.language = language;
            this.id = id;
        }

        public abstract string NamespaceStartFormat { get; }

        public abstract string NamespaceEndFormat { get; }

        public abstract string DerivedTypeStartFormat { get; }

        public abstract string TerminalTypeStartFormat { get; }

        public abstract string TypeEndFormat { get; }

        public abstract string MemberStartFormat { get; }

        public abstract string MemberEndFormat { get; }

        public abstract string MethodStartFormat { get; }

        public abstract string MethodEndFormat { get; }

        public abstract bool ExportsNonPublicMembers { get; }

        protected IDictionary<string, IList<Type>> GetTypesByNamespace(IEnumerable<Type> startingTypes)
        {
            IDictionary<string, IList<Type>> typesByNamespace = new Dictionary<string, IList<Type>>();

            var typeCollector = new Visitor();
            string currentNamespace = null;
            typeCollector.NameSpaceVisiting += (sender, args) =>
            {
                currentNamespace = args.NameSpaceName;
                if (!typesByNamespace.ContainsKey(args.NameSpaceName))
                {
                    typesByNamespace[args.NameSpaceName] = new List<Type>();
                }
            };
            typeCollector.TypeVisited += (sender, args) => { typesByNamespace[currentNamespace].Add(args.Type); };
            typeCollector.Visit(startingTypes, this.language);

            return typesByNamespace;
        }

        protected string GenerateNamespaceTypes(string nameSpace, IList<Type> allTypes)
        {
            var trace = new StringBuilder();
            var visitor = new Visitor();

            trace.AppendFormatObject(NamespaceStartFormat, new NameSpaceEventArgs()
            {
                Comment = nameSpace,
                NameSpaceName = nameSpace
            });

            visitor.TypeVisiting += (sender, args) => {
                if (args.BaseTypeInfo != null)
                {
                    trace.AppendFormatObject(DerivedTypeStartFormat, args);
                }
                else
                {
                    trace.AppendFormatObject(TerminalTypeStartFormat, args);
                }
            };
            visitor.TypeVisited += (sender, args) => { trace.AppendFormatObject(TypeEndFormat, args); };

            Func<MemberEventArgs, bool> include = args =>
                (this.ExportsNonPublicMembers || args.IsPublic) && args.IsOwnProperty && !args.IgnoredByGenerators.Contains(this.id);

            visitor.MemberVisiting += (sender, args) => {
                if (include(args))
                {
                    trace.AppendFormatObject(MemberStartFormat, args);
                }
            };

            visitor.MemberVisited += (sender, args) =>
            {
                if (include(args))
                {
                    trace.AppendFormatObject(MemberEndFormat, args);
                }
            };

            visitor.MethodVisiting += (sender, args) =>
            {
                if ((this.ExportsNonPublicMembers || args.MethodInfo.IsPublic) && args.IsOwnMethod)
                {
                    trace.AppendFormatObject(MethodStartFormat, args);
                }
            };

            visitor.MethodVisited += (sender, args) =>
            {
                if ((this.ExportsNonPublicMembers || args.MethodInfo.IsPublic) && args.IsOwnMethod)
                {
                    trace.AppendFormatObject(MethodEndFormat, args);
                }
            };

            visitor.Visit(allTypes, this.language);

            trace.AppendFormatObject(NamespaceEndFormat, new NameSpaceEventArgs() { Comment = nameSpace, NameSpaceName = nameSpace });

            return trace.ToString().Trim();
        }

        public string Generate(IEnumerable<Type> startingTypes)
        {
            StringBuilder sb = new StringBuilder();

            var typesByNamespace = GetTypesByNamespace(startingTypes);
            foreach (var typeName in typesByNamespace)
            {
                sb.AppendLine(GenerateNamespaceTypes(typeName.Key, typeName.Value));
            }

            return sb.ToString();
        }
    }
}