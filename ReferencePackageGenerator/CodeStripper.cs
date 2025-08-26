using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ReferencePackageGenerator
{
    public class CodeStripper
    {
        private static readonly Type _compilerGeneratedType = typeof(CompilerGeneratedAttribute);

        public AssemblyResolver Resolver { get; } = new();

        public AssemblyDefinition CreateReferenceAssembly(string source, string target)
        {
            var assembly = AssemblyDefinition.ReadAssembly(source,
                new ReaderParameters { AssemblyResolver = Resolver });

            foreach (var module in assembly.Modules)
            {
                foreach (var type in module.GetTypes())
                {
                    foreach (var method in type.Methods)
                    {
                        if (/*UseEmptyMethodBodies && */method.HasBody)
                        {
                            var emptyBody = new Mono.Cecil.Cil.MethodBody(method);
                            emptyBody.Instructions.Add(Mono.Cecil.Cil.Instruction.Create(Mono.Cecil.Cil.OpCodes.Ldnull));
                            emptyBody.Instructions.Add(Mono.Cecil.Cil.Instruction.Create(Mono.Cecil.Cil.OpCodes.Throw));
                            method.Body = emptyBody;
                        }
                    }
                }
            }
            
            MethodReference? referenceAttrCtor = assembly.MainModule.ImportReference(typeof(ReferenceAssemblyAttribute).GetConstructor(Type.EmptyTypes));
            assembly.CustomAttributes.Add(new CustomAttribute(referenceAttrCtor));

            assembly.Write(target);
            return assembly;
        }
    }
}