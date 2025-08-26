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
        
        private static readonly HashSet<string> TypesToRemove = new HashSet<string>
        {
            "System.Runtime.CompilerServices.ExtensionAttribute"
        };

        public AssemblyResolver Resolver { get; } = new();

        public AssemblyDefinition CreateReferenceAssembly(string source, string target)
        {
            var assembly = AssemblyDefinition.ReadAssembly(source,
                new ReaderParameters { AssemblyResolver = Resolver });

            foreach (var module in assembly.Modules)
            {
                var typeDefsToRemove = new List<TypeDefinition>();
                
                foreach (var type in module.GetTypes())
                {
                    if (TypesToRemove.Contains(type.FullName))
                    {
                        typeDefsToRemove.Add(type);
                        Console.WriteLine($"Removing type {type.FullName} from {Path.GetFileName(source)}");
                        continue;
                    }
                    
                    foreach (var method in type.Methods)
                    {
                        if (method.HasCustomAttributes)
                        {
                            var attrsToRemove = method.CustomAttributes
                                .Where(ca => TypesToRemove.Contains(ca.AttributeType.FullName))
                                .ToList();
                            foreach (var attr in attrsToRemove)
                            {
                                method.CustomAttributes.Remove(attr);
                            }
                        }
                        
                        if (/*UseEmptyMethodBodies && */method.HasBody)
                        {
                            var emptyBody = new Mono.Cecil.Cil.MethodBody(method);
                            emptyBody.Instructions.Add(Mono.Cecil.Cil.Instruction.Create(Mono.Cecil.Cil.OpCodes.Ldnull));
                            emptyBody.Instructions.Add(Mono.Cecil.Cil.Instruction.Create(Mono.Cecil.Cil.OpCodes.Throw));
                            method.Body = emptyBody;
                        }
                    }
                    
                    if (type.HasCustomAttributes)
                    {
                        var attrsToRemove = type.CustomAttributes
                            .Where(ca => TypesToRemove.Contains(ca.AttributeType.FullName))
                            .ToList();
                        foreach (var attr in attrsToRemove)
                        {
                            type.CustomAttributes.Remove(attr);
                        }
                    }
                }
                
                if (module.HasCustomAttributes)
                {
                    var attrsToRemove = module.CustomAttributes
                        .Where(ca => TypesToRemove.Contains(ca.AttributeType.FullName))
                        .ToList();
                    foreach (var attr in attrsToRemove)
                    {
                        module.CustomAttributes.Remove(attr);
                    }
                }
                
                if (assembly.HasCustomAttributes)
                {
                    var attrsToRemove = assembly.CustomAttributes
                        .Where(ca => TypesToRemove.Contains(ca.AttributeType.FullName))
                        .ToList();
                    foreach (var attr in attrsToRemove)
                    {
                        assembly.CustomAttributes.Remove(attr);
                    }
                }
                
                foreach (var typeToRemove in typeDefsToRemove)
                {
                    module.Types.Remove(typeToRemove);
                }
            }
            
            MethodReference? referenceAttrCtor = assembly.MainModule.ImportReference(typeof(ReferenceAssemblyAttribute).GetConstructor(Type.EmptyTypes));
            assembly.CustomAttributes.Add(new CustomAttribute(referenceAttrCtor));

            assembly.Write(target);
            return assembly;
        }
    }
}