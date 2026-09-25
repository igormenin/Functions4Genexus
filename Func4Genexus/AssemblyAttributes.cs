// Atributos obrigatÃ³rios para GeneXus 18
// PackageCompatibility com Version=143920 Ã© o "nÃºmero mÃ¡gico" do Gx18
// sem ele o GeneXus rejeita o package com "version 0, expecting 143920"
using System.Reflection;
using Artech.Architecture.Common.Packages;

[assembly: PackageCompatibility(Version = 143920)]

#if GX18_U14
[assembly: AssemblyMetadata("GxLine", "Gx18u14")]
#else
[assembly: AssemblyMetadata("GxLine", "Gx18u13")]
#endif

// VersÃ£o da extensÃ£o Func4Genexus
[assembly: AssemblyVersion("0.2.1.0")]
[assembly: AssemblyFileVersion("0.2.1.0")]
[assembly: AssemblyInformationalVersion("0.2.1")]


